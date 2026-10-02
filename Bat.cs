using SDL2;

namespace AshenOath;

/// <summary>
/// Bat: enemy terbang. Tanpa gravity, tanpa platform collision.
/// Hover ringan di sekitar spawn saat player jauh, chase normalized
/// saat dalam radius. Bobbing hanya visual (render), physics deterministik.
/// </summary>
public sealed class Bat : Enemy
{
    public const int BatWidth = 32;
    public const int BatHeight = 24;
    public const int BatMaxHp = 25;
    public const int BatDamage = 12;
    public const float BatSpeed = 110f;
    public const float DetectionRadius = 350f;
    public const float DetectionHysteresis = 60f;
    public const float StopDistance = 16f;
    public const float HoverSpeed = 30f;
    public const float HoverRadius = 24f;
    public const float KnockbackForce = 260f;
    public const float KnockbackDuration = 0.18f;
    public const float HitFlashDuration = 0.12f;
    public const float BobPeriod = 1.6f;
    public const float BobAmplitude = 8f;

    public override int Width => BatWidth;
    public override int Height => BatHeight;
    public override string HurtSfx => "bat_hurt";
    public bool IsFlashing => _flashTime > 0f;

    protected override float DefaultKnockbackForce => KnockbackForce;

    private readonly float _homeX;
    private readonly float _homeY;
    private bool _chasing;
    private float _knockbackTime;
    private float _knockbackVx;
    private float _flashTime;
    private float _stunTime;
    private float _bobTime;

    public Bat(float spawnX, float spawnY)
        : base(spawnX, spawnY, BatMaxHp, BatDamage)
    {
        _homeX = spawnX;
        _homeY = spawnY;
    }

    protected override void ApplyKnockback(float direction, float force)
    {
        _knockbackTime = KnockbackDuration;
        _knockbackVx = direction * force;
        _flashTime = HitFlashDuration;
    }

    public override void ApplyStun(float duration)
    {
        if (Alive && duration > 0f)
            _stunTime = Math.Max(_stunTime, duration);
    }

    protected override void OnRevived()
    {
        _chasing = false;
        _knockbackTime = 0f;
        _knockbackVx = 0f;
        _flashTime = 0f;
        _stunTime = 0f;
        _bobTime = 0f;
    }

    public override void Update(float deltaTime, World world, Player player)
    {
        if (!Alive)
            return;

        if (_flashTime > 0f)
        {
            _flashTime -= deltaTime;
            if (_flashTime < 0f)
                _flashTime = 0f;
        }
        _bobTime += deltaTime;

        float vx;
        float vy;
        if (_stunTime > 0f)
        {
            // Stun: velocity AI dihentikan; tetap melayang (tanpa gravity).
            _stunTime -= deltaTime;
            if (_stunTime < 0f)
                _stunTime = 0f;
            _knockbackTime -= deltaTime;
            if (_knockbackTime < 0f)
                _knockbackTime = 0f;
            vx = _knockbackTime > 0f ? _knockbackVx : 0f;
            vy = 0f;
            State = EnemyState.Stunned;
        }
        else if (_knockbackTime > 0f)
        {
            _knockbackTime -= deltaTime;
            vx = _knockbackVx;
            vy = 0f;
            State = _chasing ? EnemyState.Chase : EnemyState.Patrol;
        }
        else
        {
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;
            float px = player.X + Player.Width / 2f;
            float py = player.Y + Player.Height / 2f;
            float dist = DistanceTo(cx, cy, px, py);

            if (player.Alive && (dist <= DetectionRadius || (_chasing && dist <= DetectionRadius + DetectionHysteresis)))
                _chasing = true;
            else
                _chasing = false;

            if (_chasing && dist > StopDistance)
            {
                State = EnemyState.Chase;
                vx = (px - cx) / dist * BatSpeed * SpeedMultiplier;
                vy = (py - cy) / dist * BatSpeed * SpeedMultiplier;
            }
            else if (_chasing)
            {
                State = EnemyState.Chase;
                vx = 0f;
                vy = 0f;
            }
            else
            {
                State = EnemyState.Patrol;
                float hx = _homeX + Width / 2f - cx;
                float hy = _homeY + Height / 2f - cy;
                float homeDist = MathF.Sqrt(hx * hx + hy * hy);
                if (homeDist > HoverRadius)
                {
                    vx = hx / homeDist * HoverSpeed;
                    vy = hy / homeDist * HoverSpeed;
                }
                else
                {
                    vx = 0f;
                    vy = 0f;
                }
            }
        }
        VelocityX = vx;
        VelocityY = vy;

        X += vx * deltaTime;
        Y += vy * deltaTime;

        if (X < 0f) X = 0f;
        if (Y < 0f) Y = 0f;
        float maxX = world.Width - Width;
        float maxY = world.Height - Height;
        if (X > maxX) X = maxX;
        if (Y > maxY) Y = maxY;
    }

    /// <summary>Triangle-wave 0..amplitude, deterministik (visual saja).</summary>
    public float BobOffset()
    {
        float phase = (_bobTime % BobPeriod) / BobPeriod;
        return (phase < 0.5f ? phase * 2f : (1f - phase) * 2f) * BobAmplitude;
    }

    public override void Render(IntPtr renderer, Camera camera)
    {
        if (!Alive)
            return;

        float bob = BobOffset();
        int sx = (int)(X - camera.RenderX);
        int sy = (int)(Y - camera.RenderY + bob);
        int wingLift = bob > BobAmplitude / 2f ? -4 : 0;

        if (IsFlashing)
        {
            SDL.SDL_Rect flash = new() { x = sx, y = sy, w = Width, h = Height };
            SDL.SDL_SetRenderDrawColor(renderer, 240, 240, 245, 255);
            SDL.SDL_RenderFillRect(renderer, ref flash);
            return;
        }

        SDL.SDL_Rect wingL = new() { x = sx - 8, y = sy + 4 + wingLift, w = 10, h = 12 };
        SDL.SDL_Rect wingR = new() { x = sx + Width - 2, y = sy + 4 + wingLift, w = 10, h = 12 };
        SDL.SDL_SetRenderDrawColor(renderer, 60, 45, 100, 255);
        SDL.SDL_RenderFillRect(renderer, ref wingL);
        SDL.SDL_RenderFillRect(renderer, ref wingR);

        SDL.SDL_Rect body = new() { x = sx, y = sy, w = Width, h = Height };
        SDL.SDL_SetRenderDrawColor(renderer, 95, 75, 145, 255);
        SDL.SDL_RenderFillRect(renderer, ref body);

        SDL.SDL_Rect eyes = new() { x = sx + 7, y = sy + 6, w = Width - 14, h = 4 };
        SDL.SDL_SetRenderDrawColor(renderer, 255, 90, 90, 255);
        SDL.SDL_RenderFillRect(renderer, ref eyes);
    }
}
