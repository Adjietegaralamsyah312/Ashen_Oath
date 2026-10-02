using SDL2;

namespace AshenOath;

/// <summary>
/// Tampilan satu frame: texture + source rectangle + ukuran render terkontrol.
/// </summary>
public sealed class Sprite
{
    public Texture Texture { get; }
    public SDL.SDL_Rect Source { get; set; }
    public int RenderWidth { get; set; }
    public int RenderHeight { get; set; }

    public Sprite(Texture texture, SDL.SDL_Rect source, int renderWidth, int renderHeight)
    {
        Texture = texture;
        Source = source;
        RenderWidth = renderWidth;
        RenderHeight = renderHeight;
    }
}
