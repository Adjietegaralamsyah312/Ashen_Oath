using SDL2;

namespace AshenOath;

/// <summary>
/// Zona goal/exit stage: rectangle + visual gerbang emas.
/// </summary>
public sealed class Goal
{
    public float X { get; }
    public float Y { get; }
    public float W { get; }
    public float H { get; }
    public Aabb Bounds => new(X, Y, W, H);

    public Goal(float x, float y, float w, float h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    public bool IsReached(Aabb playerBounds) => Bounds.Overlaps(playerBounds);

    public void Render(IntPtr renderer, Camera camera)
    {
        int sx = (int)(X - camera.RenderX);
        int sy = (int)(Y - camera.RenderY);
        int w = (int)W;
        int h = (int)H;

        SDL.SDL_Rect outer = new() { x = sx, y = sy, w = w, h = h };
        SDL.SDL_SetRenderDrawColor(renderer, 200, 160, 60, 255);
        SDL.SDL_RenderFillRect(renderer, ref outer);

        SDL.SDL_Rect inner = new() { x = sx + 8, y = sy + 8, w = w - 16, h = h - 8 };
        SDL.SDL_SetRenderDrawColor(renderer, 255, 225, 130, 255);
        SDL.SDL_RenderFillRect(renderer, ref inner);

        SDL.SDL_Rect top = new() { x = sx - 6, y = sy - 10, w = w + 12, h = 10 };
        SDL.SDL_SetRenderDrawColor(renderer, 240, 200, 80, 255);
        SDL.SDL_RenderFillRect(renderer, ref top);
    }
}
