using SDL2;

namespace AshenOath;

/// <summary>
/// Font bitmap internal 5x7 (tanpa SDL_ttf / asset / dependency).
/// Support: A-Z (lowercase dipetakan ke uppercase), 0-9, spasi,
/// dan . , : ! ? ' - + / ( ) %. Glif tak dikenal = spasi (aman).
/// Render via rectangle primitive, screen-space.
/// </summary>
public static class BitmapFont
{
    public const int CharWidth = 5;
    public const int CharHeight = 7;
    public const int CharSpacing = 1;

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
        ['C'] = new[] { ".####", "#....", "#....", "#....", "#....", "#....", ".####" },
        ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
        ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
        ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
        ['G'] = new[] { ".####", "#....", "#....", "#..##", "#...#", "#...#", ".###." },
        ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        ['I'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
        ['J'] = new[] { "..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.." },
        ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
        ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
        ['M'] = new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" },
        ['N'] = new[] { "#...#", "##..#", "##..#", "#.#.#", "#..##", "#..##", "#...#" },
        ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
        ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
        ['Q'] = new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" },
        ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
        ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
        ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
        ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
        ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", ".#.#.", ".#.#.", "..#.." },
        ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" },
        ['X'] = new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" },
        ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
        ['Z'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },
        ['0'] = new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
        ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
        ['2'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" },
        ['3'] = new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." },
        ['4'] = new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." },
        ['5'] = new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
        ['6'] = new[] { ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###." },
        ['7'] = new[] { "#####", "....#", "...#.", "..#..", "..#..", "..#..", "..#.." },
        ['8'] = new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
        ['9'] = new[] { ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###." },
        [' '] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." },
        ['.'] = new[] { ".....", ".....", ".....", ".....", ".....", ".##..", ".##.." },
        [','] = new[] { ".....", ".....", ".....", ".....", ".##..", ".##..", ".#..." },
        [':'] = new[] { ".....", ".....", ".##..", ".##..", ".....", ".##..", ".##.." },
        ['!'] = new[] { "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#.." },
        ['?'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.." },
        ['\''] = new[] { "..#..", "..#..", ".....", ".....", ".....", ".....", "....." },
        ['-'] = new[] { ".....", ".....", ".....", "#####", ".....", ".....", "....." },
        ['+'] = new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." },
        ['/'] = new[] { "....#", "....#", "...#.", "...#.", "..#..", ".#...", "#...." },
        ['('] = new[] { "...#.", "..#..", ".#...", ".#...", ".#...", "..#..", "...#." },
        [')'] = new[] { ".#...", "..#..", "...#.", "...#.", "...#.", "..#..", ".#..." },
        ['%'] = new[] { "##..#", "##.#.", "..#..", "..#..", ".#...", ".#.##", "#..##" },
    };

    /// <summary>True bila karakter dapat digambar (selain spasi aman).</summary>
    public static bool HasGlyph(char c)
    {
        if (c == ' ') return true;
        return Glyphs.ContainsKey(char.ToUpperInvariant(c));
    }

    /// <summary>Lebar teks dalam pixel (0 untuk string kosong).</summary>
    public static int MeasureText(string? text, int scale = 2)
    {
        if (string.IsNullOrEmpty(text) || scale <= 0)
            return 0;
        return text.Length * (CharWidth + CharSpacing) * scale - CharSpacing * scale;
    }

    public static int LineHeight(int scale = 2) => scale <= 0 ? 0 : CharHeight * scale;

    /// <summary>Gambar teks pada posisi screen-space. Glif tak dikenal = spasi.</summary>
    public static void DrawText(IntPtr renderer, string? text, int x, int y, int scale = 2,
        byte r = 235, byte g = 235, byte b = 240)
    {
        if (string.IsNullOrEmpty(text) || scale <= 0)
            return;
        SDL.SDL_SetRenderDrawColor(renderer, r, g, b, 255);
        int cx = x;
        foreach (char raw in text)
        {
            char c = char.ToUpperInvariant(raw);
            if (Glyphs.TryGetValue(c, out var rows))
            {
                for (int row = 0; row < CharHeight; row++)
                {
                    string line = rows[row];
                    for (int col = 0; col < CharWidth; col++)
                    {
                        if (line[col] == '#')
                        {
                            SDL.SDL_Rect pixel = new()
                            {
                                x = cx + col * scale,
                                y = y + row * scale,
                                w = scale,
                                h = scale
                            };
                            SDL.SDL_RenderFillRect(renderer, ref pixel);
                        }
                    }
                }
            }
            cx += (CharWidth + CharSpacing) * scale;
        }
    }

    /// <summary>Gambar teks rata tengah pada [centerX] (screen-space).</summary>
    public static void DrawTextCentered(IntPtr renderer, string? text, int centerX, int y, int scale = 2,
        byte r = 235, byte g = 235, byte b = 240)
    {
        int w = MeasureText(text, scale);
        DrawText(renderer, text, centerX - w / 2, y, scale, r, g, b);
    }
}
