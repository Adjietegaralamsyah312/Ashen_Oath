using SDL2;

namespace AshenOath;

/// <summary>
/// Satu solid rectangle di world (ground, platform, wall).
/// Data + render diri sendiri. Tidak tahu tentang Player.
/// </summary>
public sealed class Platform
{
    public float X { get; }
    public float Y { get; }
    public float W { get; }
    public float H { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    public Aabb Bounds => new(X, Y, W, H);

    public Platform(float x, float y, float w, float h, byte r, byte g, byte b)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
        R = r;
        G = g;
        B = b;
    }

    public void Render(IntPtr renderer, Camera camera)
    {
        SDL.SDL_Rect rect = new()
        {
            x = (int)(X - camera.RenderX),
            y = (int)(Y - camera.RenderY),
            w = (int)W,
            h = (int)H
        };
        SDL.SDL_SetRenderDrawColor(renderer, R, G, B, 255);
        SDL.SDL_RenderFillRect(renderer, ref rect);
    }
}
