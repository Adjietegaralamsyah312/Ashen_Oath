using SDL2;

namespace AshenOath;

/// <summary>
/// Spike hazard: static AABB, deals damage on player overlap.
/// Respects player invulnerability.
/// </summary>
public sealed class Spike
{
    public const int SpikeDamage = 35;
    public const int SpikeWidth = 32;
    public const int SpikeHeight = 16;

    public float X { get; }
    public float Y { get; }
    public int Width => SpikeWidth;
    public int Height => SpikeHeight;
    public Aabb Bounds => new(X, Y, Width, Height);

    private readonly float _spawnX;
    private readonly float _spawnY;

    /// <summary>Difficulty saat spawn (Normal = nilai existing).</summary>
    public Difficulty Difficulty { get; private set; } = Difficulty.Normal;

    public Spike(float x, float y)
    {
        _spawnX = x;
        _spawnY = y;
        X = x;
        Y = y;
    }

    /// <summary>Terapkan difficulty sekali saat spawn (Hard +15%).</summary>
    public void ApplyDifficulty(Difficulty difficulty)
    {
        Difficulty = DifficultyModifiers.IsDefined(difficulty) ? difficulty : Difficulty.Normal;
    }

    public void Update(float deltaTime, World world, Player player)
    {
    }

    public void CheckPlayer(Player player)
    {
        if (!player.Alive)
            return;

        if (player.Bounds.Overlaps(Bounds))
        {
            player.TakeDamage(DifficultyModifiers.ScaleHazardDamage(SpikeDamage, Difficulty));
        }
    }

    public void Render(IntPtr renderer, Camera camera)
    {
        int sx = (int)(X - camera.RenderX);
        int sy = (int)(Y - camera.RenderY);

        SDL.SDL_Rect baseRect = new() { x = sx, y = sy + Height - 4, w = Width, h = 4 };
        SDL.SDL_SetRenderDrawColor(renderer, 80, 60, 50, 255);
        SDL.SDL_RenderFillRect(renderer, ref baseRect);

        for (int i = 0; i < Width; i += 8)
        {
            SDL.SDL_Rect spike = new() { x = sx + i, y = sy, w = 8, h = Height };
            SDL.SDL_SetRenderDrawColor(renderer, 180, 140, 120, 255);
            SDL.SDL_RenderFillRect(renderer, ref spike);

            SDL.SDL_Rect tip = new() { x = sx + i + 2, y = sy, w = 4, h = Height };
            SDL.SDL_SetRenderDrawColor(renderer, 200, 160, 130, 255);
            SDL.SDL_RenderFillRect(renderer, ref tip);
        }
    }

    public void RenderDebug(IntPtr renderer, Camera camera)
    {
        int sx = (int)(X - camera.RenderX);
        int sy = (int)(Y - camera.RenderY);
        SDL.SDL_Rect debug = new() { x = sx, y = sy, w = Width, h = Height };
        SDL.SDL_SetRenderDrawColor(renderer, 255, 80, 80, 255);
        SDL.SDL_RenderDrawRect(renderer, ref debug);
    }
}