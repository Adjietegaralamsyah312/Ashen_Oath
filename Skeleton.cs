using SDL2;

namespace AshenOath;

/// <summary>
/// Skeleton: ground melee. Patrol saat player jauh, chase horizontal saat
/// dalam radius deteksi, berhenti saat sangat dekat. Gravity + collision
/// via Physics/World yang sama dengan player/slime.
/// </summary>
public sealed class Skeleton : Enemy
{
    public const int SkeletonWidth = 32;
    public const int SkeletonHeight = 48;
    public const int SkeletonMaxHp = 50;
    public const int SkeletonDamage = 15;
    public const float SkeletonSpeed = 90f;
    public const float DetectionRadius = 280f;
    public const float DetectionHysteresis = 60f;
    public const float StopDistance = 30f;
    public const float KnockbackForce = 220f;
    public const float KnockbackDuration = 0.18f;
    public const float HitFlashDuration = 0.12f;

    public override int Width => SkeletonWidth;
    public override int Height => SkeletonHeight;
    public override string HurtSfx => "skeleton_hurt";
    public bool IsFlashing => _flashTime > 0f;

    protected override float DefaultKnockbackForce => KnockbackForce;

    private int _direction = -1;
    private bool _chasing;
    private float _knockbackTime;
    private float _knockbackVx;
    private float _flashTime;
    private float _stunTime;

    public Skeleton(float spawnX, float spawnY)
        : base(spawnX, spawnY, SkeletonMaxHp, SkeletonDamage)
    {
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
        _direction = -1;
        _chasing = false;
        _knockbackTime = 0f;
        _knockbackVx = 0f;
        _flashTime = 0f;
        _stunTime = 0f;
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
            // Stun: AI berhenti; knockback + gravity tetap berjalan.
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

            if (_chasing)
            {
                State = EnemyState.Chase;
                float dx = px - cx;
                if (Math.Abs(dx) > StopDistance)
                {
                    _direction = dx > 0f ? 1 : -1;
                    intendedVx = _direction * SkeletonSpeed * SpeedMultiplier;
                }
                else
                {
                    intendedVx = 0f;
                }
            }
            else
            {
                State = EnemyState.Patrol;
                intendedVx = _direction * SkeletonSpeed * SpeedMultiplier;
            }
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

        int sx = (int)(X - camera.RenderX);
        int sy = (int)(Y - camera.RenderY);

        if (IsFlashing)
        {
            SDL.SDL_Rect flash = new() { x = sx, y = sy, w = Width, h = Height };
            SDL.SDL_SetRenderDrawColor(renderer, 240, 240, 245, 255);
            SDL.SDL_RenderFillRect(renderer, ref flash);
            return;
        }

        SDL.SDL_Rect body = new() { x = sx, y = sy + 14, w = Width, h = Height - 14 };
        SDL.SDL_SetRenderDrawColor(renderer, 180, 180, 190, 255);
        SDL.SDL_RenderFillRect(renderer, ref body);

        SDL.SDL_Rect skull = new() { x = sx + 4, y = sy, w = Width - 8, h = 16 };
        SDL.SDL_SetRenderDrawColor(renderer, 215, 215, 220, 255);
        SDL.SDL_RenderFillRect(renderer, ref skull);

        SDL.SDL_Rect eyes = new() { x = sx + 8, y = sy + 6, w = Width - 16, h = 5 };
        SDL.SDL_SetRenderDrawColor(renderer, 30, 30, 40, 255);
        SDL.SDL_RenderFillRect(renderer, ref eyes);
    }
}
