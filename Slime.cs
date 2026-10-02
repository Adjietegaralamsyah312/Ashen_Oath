using SDL2;

namespace AshenOath;

/// <summary>
/// Slime prototype: rectangle, patrol kiri/kanan, gravity + collision
/// via Physics/World yang sama dengan player. Balik arah saat menabrak
/// wall atau mencapai tepi platform.
/// </summary>
public sealed class Slime : Enemy
{
    public const int SlimeWidth = 36;
    public const int SlimeHeight = 28;
    public const int SlimeMaxHp = 30;
    public const int SlimeDamage = 10;
    public const float SlimeSpeed = 60f;
    public const float KnockbackForce = 260f;
    public const float KnockbackDuration = 0.18f;
    public const float HitFlashDuration = 0.12f;

    public override int Width => SlimeWidth;
    public override int Height => SlimeHeight;
    public bool IsFlashing => _flashTime > 0f;
    public override bool IsStunned => _stunTime > 0f;

    private int _direction = -1;
    private float _knockbackTime;
    private float _knockbackVx;
    private float _flashTime;
    private float _stunTime;

    public Slime(float spawnX, float spawnY)
        : base(spawnX, spawnY, SlimeMaxHp, SlimeDamage)
    {
    }

    protected override float DefaultKnockbackForce => KnockbackForce;

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
        _direction = -1;
        _knockbackTime = 0f;
        _knockbackVx = 0f;
        _stunTime = 0f;
        _knockbackVx = 0f;
        _flashTime = 0f;
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

        float intendedVx;
        if (_stunTime > 0f)
        {
            // Stun: tidak patrol; knockback + gravity tetap berjalan.
            _stunTime -= deltaTime;
            if (_stunTime < 0f)
                _stunTime = 0f;
            _knockbackTime -= deltaTime;
            if (_knockbackTime < 0f)
                _knockbackTime = 0f;
            intendedVx = _knockbackTime > 0f ? _knockbackVx : 0f;
            State = EnemyState.Stunned;
        }
        else if (_knockbackTime > 0f)
        {
            _knockbackTime -= deltaTime;
            intendedVx = _knockbackVx;
            State = EnemyState.Patrol;
        }
        else
        {
            intendedVx = _direction * SlimeSpeed * SpeedMultiplier;
            State = EnemyState.Patrol;
        }
        VelocityX = intendedVx;

        bool wasGrounded = Grounded;
        VelocityY = Physics.ApplyGravity(VelocityY, deltaTime);
        Physics.MoveX(this, world.Solids, deltaTime);

        if (intendedVx != 0f && VelocityX == 0f)
        {
            // Menabrak wall: balik arah.
            _direction = -_direction;
        }
        else if (wasGrounded && _knockbackTime <= 0f && _stunTime <= 0f)
        {
            // Tepi platform: tidak ada pijakan di depan -> balik arah.
            float probeX = _direction > 0 ? X + Width + 2f : X - 2f;
            var probe = new Aabb(probeX - 1f, Y + Height, 2f, 8f);
            bool footing = false;
            foreach (var solid in world.Solids)
            {
                if (probe.Overlaps(solid))
                {
                    footing = true;
                    break;
                }
            }
            if (!footing)
                _direction = -_direction;
        }

        Physics.MoveY(this, world.Solids, deltaTime);

        if (X < 0f) X = 0f;
        if (Y < 0f) Y = 0f;
        float maxX = world.Width - Width;
        float maxY = world.Height - Height;
        if (X > maxX) X = maxX;
        if (Y > maxY) Y = maxY;
    }

    public override void Render(IntPtr renderer, Camera camera)
    {
        if (!Alive)
            return;

        if (IsFlashing)
        {
            SDL.SDL_Rect flash = new()
            {
                x = (int)(X - camera.RenderX),
                y = (int)(Y - camera.RenderY),
                w = Width,
                h = Height
            };
            SDL.SDL_SetRenderDrawColor(renderer, 240, 240, 245, 255);
            SDL.SDL_RenderFillRect(renderer, ref flash);
            return;
        }

        SDL.SDL_Rect body = new()
        {
            x = (int)(X - camera.RenderX),
            y = (int)(Y - camera.RenderY),
            w = Width,
            h = Height
        };
        SDL.SDL_SetRenderDrawColor(renderer, 80, 180, 100, 255);
        SDL.SDL_RenderFillRect(renderer, ref body);

        SDL.SDL_Rect base_ = new()
        {
            x = (int)(X - camera.RenderX),
            y = (int)(Y - camera.RenderY) + Height - 6,
            w = Width,
            h = 6
        };
        SDL.SDL_SetRenderDrawColor(renderer, 50, 130, 70, 255);
        SDL.SDL_RenderFillRect(renderer, ref base_);
    }
}
