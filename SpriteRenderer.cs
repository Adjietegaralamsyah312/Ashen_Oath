using SDL2;

namespace AshenOath;

/// <summary>
/// Satu-satunya tempat yang memanggil SDL_RenderCopyEx untuk sprite.
/// Flip horizontal dipakai untuk arah Left/Right.
/// </summary>
public static class SpriteRenderer
{
    public static void Draw(
        IntPtr renderer,
        IntPtr texture,
        in SDL.SDL_Rect source,
        int x, int y, int width, int height,
        SDL.SDL_RendererFlip flip)
    {
        SDL.SDL_Rect src = source;
        SDL.SDL_Rect dest = new() { x = x, y = y, w = width, h = height };
        SDL.SDL_RenderCopyEx(renderer, texture, ref src, ref dest, 0.0, IntPtr.Zero, flip);
    }
}
