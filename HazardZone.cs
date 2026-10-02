using SDL2;

namespace AshenOath;

/// <summary>
/// Hazard Zone: rectangular area dealing periodic damage.
/// Uses per-zone timer to tick at fixed interval (0.5s).
/// Respects player invulnerability.
/// </summary>
public sealed class HazardZone
{
    public const int HazardDamage = 15;
    public const float TickInterval = 0.5f;

    public float X { get; }
    public float Y { get; }
    public int Width { get; }
    public int Height { get; }
    public Aabb Bounds => new(X, Y, Width, Height);

    private float _tickTimer;

    /// <summary>Difficulty saat spawn (Normal = nilai existing).</summary>
    public Difficulty Difficulty { get; private set; } = Difficulty.Normal;

    public HazardZone(float x, float y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        _tickTimer = TickInterval;
    }

    /// <summary>Terapkan difficulty sekali saat spawn (Hard +15%).</summary>
    public void ApplyDifficulty(Difficulty difficulty)
    {
        Difficulty = DifficultyModifiers.IsDefined(difficulty) ? difficulty : Difficulty.Normal;
    }

    public void Update(float deltaTime, World world, Player player)
    {
        _tickTimer -= deltaTime;
        if (_tickTimer <= 0f)
        {
            _tickTimer = TickInterval;
            CheckPlayer(player);
        }
    }

    public void CheckPlayer(Player player)
    {
        if (!player.Alive)
            return;

        if (player.Bounds.Overlaps(Bounds))
        {
            player.TakeDamage(DifficultyModifiers.ScaleHazardDamage(HazardDamage, Difficulty));
        }
    }

    public void Render(IntPtr renderer, Camera camera)
    {
        int sx = (int)(X - camera.RenderX);
        int sy = (int)(Y - camera.RenderY);

for (int x = sx; x < sx + Width; x += 16)
            {
                for (int y = sy; y < sy + Height; y += 16)
                {
                    bool dark = ((x - sx) / 16 + (y - sy) / 16) % 2 == 0;
                    SDL.SDL_Rect tile = new() { x = x, y = y, w = 16, h = 16 };
                    SDL.SDL_SetRenderDrawColor(renderer, dark ? (byte)140 : (byte)100, (byte)40, (byte)40, (byte)255);
                    SDL.SDL_RenderFillRect(renderer, ref tile);
                }
            }

        SDL.SDL_Rect border = new() { x = sx, y = sy, w = Width, h = Height };
        SDL.SDL_SetRenderDrawColor(renderer, 200, 60, 60, 255);
        SDL.SDL_RenderDrawRect(renderer, ref border);
    }

    public void RenderDebug(IntPtr renderer, Camera camera)
    {
        int sx = (int)(X - camera.RenderX);
        int sy = (int)(Y - camera.RenderY);
        SDL.SDL_Rect debug = new() { x = sx, y = sy, w = Width, h = Height };
        SDL.SDL_SetRenderDrawColor(renderer, 255, 120, 80, 255);
        SDL.SDL_RenderDrawRect(renderer, ref debug);
    }
}