using SDL2;

namespace AshenOath;

/// <summary>
/// Projectile milik boss: hanya damage ke Player, tidak ke enemy,
/// tidak pierce, hilang saat kena player/wall/expire.
/// System terpisah dari <see cref="Projectile"/> karena ownership
/// dan arah memang berbeda (2D sederhana vs horizontal player).
/// Collision conventions sama (AABB Overlaps, world Solids).
/// </summary>
public sealed class BossProjectile
{
    public const float Speed = 350f;
    public const float Lifetime = 2.0f;
    public const int ProjectileWidth = 16;
    public const int ProjectileHeight = 10;

    public float X { get; private set; }
    public float Y { get; private set; }
    public int Damage { get; }
    public bool Alive { get; private set; } = true;
    public float TimeLeft => _timeLeft;
    public Aabb Bounds => new(X, Y, ProjectileWidth, ProjectileHeight);

    private readonly float _vx;
    private readonly float _vy;
    private float _timeLeft = Lifetime;

    public BossProjectile(float x, float y, float dirX, float dirY, int damage)
    {
        X = x;
        Y = y;
        Damage = damage;
        float len = MathF.Sqrt(dirX * dirX + dirY * dirY);
        if (len < 0.001f) { dirX = 1f; dirY = 0f; len = 1f; }
        _vx = dirX / len * Speed;
        _vy = dirY / len * Speed;
    }

    public void Update(float deltaTime, World world, Player player)
    {
        if (!Alive)
            return;

        _timeLeft -= deltaTime;
        if (_timeLeft <= 0f)
        {
            Alive = false;
            return;
        }

        X += _vx * deltaTime;
        Y += _vy * deltaTime;

        if (X < -ProjectileWidth || X > world.Width || Y < -64f || Y > world.Height + 64f)
        {
            Alive = false;
            return;
        }

        var box = Bounds;
        foreach (var solid in world.Solids)
        {
            if (box.Overlaps(solid))
            {
                Alive = false;
                return;
            }
        }

        if (player.Alive && box.Overlaps(player.Bounds))
        {
            player.TakeDamage(Damage);
            Alive = false;
        }
    }

    public void Kill()
    {
        Alive = false;
    }

    public void Render(IntPtr renderer, Camera camera)
    {
        if (!Alive)
            return;
        int sx = (int)(X - camera.RenderX);
        int sy = (int)(Y - camera.RenderY);
        SDL.SDL_Rect body = new() { x = sx, y = sy, w = ProjectileWidth, h = ProjectileHeight };
        SDL.SDL_SetRenderDrawColor(renderer, 230, 110, 50, 255);
        SDL.SDL_RenderFillRect(renderer, ref body);
        SDL.SDL_Rect core = new() { x = sx + 3, y = sy + 2, w = ProjectileWidth - 6, h = ProjectileHeight - 4 };
        SDL.SDL_SetRenderDrawColor(renderer, 255, 210, 120, 255);
        SDL.SDL_RenderFillRect(renderer, ref core);
    }
}
