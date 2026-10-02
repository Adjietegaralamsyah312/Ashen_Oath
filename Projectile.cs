using SDL2;

namespace AshenOath;

/// <summary>
/// Projectile horizontal: arah dikunci saat dibuat, gerak delta time,
/// mati saat kena enemy/wall/platform, habis lifetime, atau keluar world.
/// Tanpa gravity, tanpa physics Player. Render rectangle saja.
/// </summary>
public sealed class Projectile
{
    public const float Speed = 650f;
    public const int Damage = 12;
    public const float Lifetime = 1.2f;
    public const int Width = 12;
    public const int Height = 8;

    public float X { get; private set; }
    public float Y { get; private set; }
    public float Direction { get; }
    public bool Alive { get; private set; } = true;
    public Aabb Bounds => new(X, Y, Width, Height);

    private float _velocityX;
    private float _timeLeft = Lifetime;

    public Projectile(float x, float y, float direction)
    {
        X = x;
        Y = y;
        Direction = direction >= 0f ? 1f : -1f;
        _velocityX = Direction * Speed;
    }

    public void Update(float deltaTime, World world)
    {
        if (!Alive)
            return;

        _timeLeft -= deltaTime;
        if (_timeLeft <= 0f)
        {
            Alive = false;
            return;
        }

        X += _velocityX * deltaTime;

        if (X < -Width || X > world.Width || Y < -64f || Y > world.Height + 64f)
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

        SDL.SDL_Rect body = new() { x = sx, y = sy, w = Width, h = Height };
        SDL.SDL_SetRenderDrawColor(renderer, 120, 220, 255, 255);
        SDL.SDL_RenderFillRect(renderer, ref body);

        // Ekor di sisi belakang agar arah jelas.
        int tailX = Direction > 0f ? sx - 5 : sx + Width;
        SDL.SDL_Rect tail = new() { x = tailX, y = sy + 2, w = 5, h = Height - 4 };
        SDL.SDL_SetRenderDrawColor(renderer, 60, 140, 190, 255);
        SDL.SDL_RenderFillRect(renderer, ref tail);
    }
}
