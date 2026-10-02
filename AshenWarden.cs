using SDL2;

namespace AshenOath;

/// <summary>
/// Boss pertama "Ashen Warden". Menggunakan Player/Combat/Skills/Physics/
/// Camera/HUD/AudioManager existing; tidak ada combat system kedua.
/// </summary>
public sealed class AshenWarden : Boss
{
    // ---- Prototype constants (plant.md §2, §6-8, §10) ----
    public const int BossMaxHp = 500;
    public const int ContactDamagePhase1 = 20;
    public const int ContactDamagePhase2 = 25;
    public const int BossWidth = 64;
    public const int BossHeight = 80;
    public const float MoveSpeedPhase1 = 75f;
    public const float MoveSpeedPhase2 = 100f;
    public const int Phase2Threshold = 250;

    public const float MeleeWindup = 0.35f;
    public const float MeleeActiveDuration = 0.20f;
    public const int MeleeWidth = 90;
    public const int MeleeHeight = 60;
    public const int MeleeDamagePhase1 = 20;
    public const int MeleeDamagePhase2 = 25;
    public const float MeleeCooldownPhase1 = 1.2f;
    public const float MeleeCooldownPhase2 = 0.85f;
    public const float MeleeRange = 85f;

    public const float ProjectileCooldownPhase1 = 2.5f;
    public const float ProjectileCooldownPhase2 = 1.6f;
    public const float RangedThreshold = 280f;

    public const float LeapCooldown = 4.0f;
    public const float LeapWindup = 0.30f;
    public const float LeapAirTime = 0.55f;
    public const float LeapVelocityY = -550f;
    public const float LeapMaxDistance = 400f;
    public const int LeapWidth = 70;
    public const int LeapHeight = 40;
    public const float LeapActiveDuration = 0.20f;
    public const int LeapDamage = 25;

    public const float KnockbackDuration = 0.12f;
    public const float HitFlashDuration = 0.12f;

    public override string HurtSfx => "boss_hit";
    public override int Damage => DifficultyModifiers.ScaleBossDamage(
        _phase >= 2 ? ContactDamagePhase2 : ContactDamagePhase1, Difficulty);

    public float MoveSpeed => _phase >= 2 ? MoveSpeedPhase2 : MoveSpeedPhase1;
    public int MeleeDamage => DifficultyModifiers.ScaleBossDamage(
        _phase >= 2 ? MeleeDamagePhase2 : MeleeDamagePhase1, Difficulty);
    private int ScaledLeapDamage => DifficultyModifiers.ScaleBossDamage(LeapDamage, Difficulty);

    /// <summary>True selama windup serangan (telegraph visual).</summary>
    public bool IsWindingUp => Alive && !BossDefeated
        && (_attackWindup > 0f || _leapWindupTime > 0f);
    public float MeleeCooldownValue => _phase >= 2 ? MeleeCooldownPhase2 : MeleeCooldownPhase1;
    public float ProjectileCooldownValue => _phase >= 2 ? ProjectileCooldownPhase2 : ProjectileCooldownPhase1;
    public float AttackCooldownRemaining => _attackCooldown;
    public float ProjectileCooldownRemaining => _projectileCooldown;
    public float LeapCooldownRemaining => _leapCooldown;
    public float PhaseTransitionTimeLeft => _phaseTransitionTime;
    public bool MeleeActive => _attackActive;
    public bool MeleeHitDone => _meleeHitDone;
    public Aabb MeleeBox => _meleeBox;
    public bool IsLeaping => _isLeaping;
    public bool LeapLandingActive => _leapLandingActive;
    public Aabb LeapBox => _leapBox;
    public bool IsFlashing => _flashTime > 0f;
    public bool IsPhaseFlashing => CurrentState == BossState.PhaseTransition;
    public Aabb? ArenaBounds { get; set; }

    public event Action? MeleeStarted;
    public event Action? ProjectileFired;
    public event Action? LeapStarted;

    private float _projectileCooldown;
    private float _meleeActiveTime;
    private bool _meleeHitDone;
    private Aabb _meleeBox;
    private readonly List<BossProjectile> _pending = new();

    private float _leapCooldown;
    private bool _isLeaping;
    private float _leapWindupTime;
    private bool _leapAirborne;
    private float _leapLandingTime;
    private bool _leapLandingActive;
    private bool _leapHitDone;
    private Aabb _leapBox;
    private float _leapTargetX;

    private float _knockbackTime;
    private float _knockbackVx;
    private float _flashTime;
    private bool _bossDifficultyApplied;

    public AshenWarden(float spawnX, float spawnY)
        : base(spawnX, spawnY, BossMaxHp, ContactDamagePhase1, BossWidth, BossHeight)
    {
    }

    /// <summary>
    /// Difficulty khusus boss (HP +25%, damage +15% di Hard).
    /// Tidak memanggil base agar mult enemy tidak ikut terpakai.
    /// </summary>
    public override void ApplyDifficulty(Difficulty difficulty)
    {
        if (_bossDifficultyApplied)
            return;
        _bossDifficultyApplied = true;
        Difficulty = DifficultyModifiers.IsDefined(difficulty) ? difficulty : Difficulty.Normal;
        MaxHp = DifficultyModifiers.ScaleBossHp(BossMaxHp, Difficulty);
        Hp = MaxHp;
    }

    protected override float MaxStunDuration => 0.15f;

    protected override void ApplyKnockback(float direction, float force)
    {
        // Knockback sangat kecil + durasi singkat (tidak stunlock).
        _knockbackTime = KnockbackDuration;
        _knockbackVx = direction * force * 0.25f;
        _flashTime = HitFlashDuration;
    }

    protected override void OnDamaged()
    {
        _flashTime = HitFlashDuration;
    }

    protected override void OnDied()
    {
        _pending.Clear();
        _isLeaping = false;
        _leapAirborne = false;
        _leapLandingActive = false;
        VelocityX = 0f;
    }

    protected override void OnClearAttacks()
    {
        _attackActive = false;
        _attackWindup = 0f;
        _meleeActiveTime = 0f;
        _meleeHitDone = false;
        _isLeaping = false;
        _leapWindupTime = 0f;
        _leapAirborne = false;
        _leapLandingActive = false;
        _leapLandingTime = 0f;
        _leapHitDone = false;
        _pending.Clear();
    }

    public List<BossProjectile> DrainSpawnedProjectiles()
    {
        if (_pending.Count == 0)
            return new List<BossProjectile>();
        var out_ = new List<BossProjectile>(_pending);
        _pending.Clear();
        return out_;
    }

    public override void Update(float deltaTime, World world, Player player)
    {
        if (!Alive)
        {
            UpdateDeath(deltaTime);
            VelocityX = 0f;
            VelocityY = Physics.ApplyGravity(VelocityY, deltaTime);
            Physics.MoveX(this, world.Solids, deltaTime);
            Physics.MoveY(this, world.Solids, deltaTime);
            ClampArena();
            return;
        }
        if (BossDefeated)
            return;

        if (!EncounterActive)
        {
            CurrentState = BossState.Idle;
            VelocityX = 0f;
            VelocityY = Physics.ApplyGravity(VelocityY, deltaTime);
            Physics.MoveX(this, world.Solids, deltaTime);
            Physics.MoveY(this, world.Solids, deltaTime);
            ClampArena();
            return;
        }

        if (_flashTime > 0f)
        {
            _flashTime -= deltaTime;
            if (_flashTime < 0f) _flashTime = 0f;
        }

        UpdatePhaseTransition(deltaTime);
        if (CurrentState == BossState.PhaseTransition)
        {
            VelocityX = 0f;
            VelocityY = Physics.ApplyGravity(VelocityY, deltaTime);
            Physics.MoveX(this, world.Solids, deltaTime);
            Physics.MoveY(this, world.Solids, deltaTime);
            ClampArena();
            return;
        }

        UpdateStun(deltaTime);
        base.UpdateCooldowns(deltaTime);
        if (_projectileCooldown > 0f)
        {
            _projectileCooldown -= deltaTime;
            if (_projectileCooldown < 0f) _projectileCooldown = 0f;
        }
        if (_leapCooldown > 0f)
        {
            _leapCooldown -= deltaTime;
            if (_leapCooldown < 0f) _leapCooldown = 0f;
        }

        // Phase 1 -> 2.
        if (_phase == 1 && Hp <= Phase2Threshold)
        {
            TransitionToPhase(2);
            _projectileCooldown = Math.Min(_projectileCooldown, 0.5f);
            return;
        }

        if (_stunTime > 0f)
        {
            CurrentState = BossState.Stunned;
            if (_knockbackTime > 0f)
            {
                _knockbackTime -= deltaTime;
                VelocityX = _knockbackVx;
            }
            else
            {
                VelocityX = 0f;
            }
            VelocityY = Physics.ApplyGravity(VelocityY, deltaTime);
            Physics.MoveX(this, world.Solids, deltaTime);
            Physics.MoveY(this, world.Solids, deltaTime);
            ClampArena();
            if (_stunTime <= 0f)
                CurrentState = BossState.Chase;
            return;
        }

        if (_knockbackTime > 0f)
        {
            _knockbackTime -= deltaTime;
            if (_knockbackTime < 0f) _knockbackTime = 0f;
        }

        // ---- Leap (Phase 2) ----
        if (_isLeaping)
        {
            UpdateLeap(deltaTime, world, player);
            return;
        }

        // ---- Melee attack ----
        if (CurrentState == BossState.Attack)
        {
            VelocityX = _knockbackTime > 0f ? _knockbackVx : 0f;
            VelocityY = Physics.ApplyGravity(VelocityY, deltaTime);
            Physics.MoveX(this, world.Solids, deltaTime);
            Physics.MoveY(this, world.Solids, deltaTime);
            ClampArena();

            if (_attackWindup > 0f)
            {
                UpdateMeleeBox();
                return;
            }
            if (_attackActive)
            {
                UpdateMeleeBox();
                _meleeActiveTime -= deltaTime;
                if (!_meleeHitDone && player.Alive && _meleeBox.Overlaps(player.Bounds))
                {
                    player.TakeDamage(MeleeDamage);
                    _meleeHitDone = true;
                }
                if (_meleeActiveTime <= 0f)
                {
                    _attackActive = false;
                    _attackCooldown = MeleeCooldownValue;
                    CurrentState = BossState.Chase;
                }
            }
            else
            {
                CurrentState = BossState.Chase;
            }
            return;
        }

        // ---- Chase ----
        CurrentState = BossState.Chase;
        FacePlayer(player);
        float hDist = HorizontalDistanceToPlayer(player);

        // Leap trigger (Phase 2 saja, jarak menengah, di ground).
        if (_phase >= 2 && _leapCooldown <= 0f && Grounded
            && hDist > 150f && hDist < 550f && player.Alive)
        {
            StartLeap(player);
            return;
        }

        // Melee trigger.
        if (player.Alive && hDist <= MeleeRange && _attackCooldown <= 0f)
        {
            StartMelee();
            return;
        }

        // Projectile trigger (cukup jauh).
        if (player.Alive && hDist >= RangedThreshold && _projectileCooldown <= 0f)
        {
            FireProjectile(player);
            // Tetap bergerak sambil menembak (lebih agresif Phase 2).
        }

        float dir = _facingDirection >= 0 ? 1f : -1f;
        float kb = _knockbackTime > 0f ? _knockbackVx : 0f;
        VelocityX = kb != 0f ? kb : dir * MoveSpeed;
        // Jangan chase bila player mati: berhenti.
        if (!player.Alive)
            VelocityX = 0f;

        VelocityY = Physics.ApplyGravity(VelocityY, deltaTime);
        Physics.MoveX(this, world.Solids, deltaTime);
        Physics.MoveY(this, world.Solids, deltaTime);
        ClampArena();
    }

    private void StartMelee()
    {
        CurrentState = BossState.Attack;
        _attackWindup = MeleeWindup;
        _attackActive = false;
        _meleeActiveTime = MeleeActiveDuration;
        _meleeHitDone = false;
        UpdateMeleeBox();
        MeleeStarted?.Invoke();
    }

    private void UpdateMeleeBox()
    {
        float x = _facingDirection >= 0 ? X + Width : X - MeleeWidth;
        float y = Y + (Height - MeleeHeight) / 2f;
        _meleeBox = new Aabb(x, y, MeleeWidth, MeleeHeight);
    }

    private void FireProjectile(Player player)
    {
        float cx = X + Width / 2f;
        float cy = Y + Height / 2f;
        float px = player.X + Player.Width / 2f;
        float py = player.Y + Player.Height / 2f;
        float dx = px - cx;
        float dy = py - cy;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1f) { dx = _facingDirection; dy = 0f; len = 1f; }
        dx /= len; dy /= len;

        float sx = cx + dx * (Width / 2f) - BossProjectile.ProjectileWidth / 2f;
        float sy = cy + dy * (Height / 2f) - BossProjectile.ProjectileHeight / 2f;
        int dmg = DifficultyModifiers.ScaleBossDamage(_phase >= 2 ? 18 : 15, Difficulty);
        _pending.Add(new BossProjectile(sx, sy, dx, dy, dmg));
        _projectileCooldown = ProjectileCooldownValue;
        // Tahap 20 telegraph: flash singkat saat firing (visual cue, tanpa ubah timing).
        // Melee/leap sudah punya windup 0.35s/0.30s + blade/landing indicator di Render.
        _flashTime = Math.Max(_flashTime, HitFlashDuration * 0.75f);
        ProjectileFired?.Invoke();
    }

    private void StartLeap(Player player)
    {
        _isLeaping = true;
        _leapWindupTime = LeapWindup;
        _leapAirborne = false;
        _leapLandingActive = false;
        _leapHitDone = false;
        float cx = X + Width / 2f;
        float px = player.X + Player.Width / 2f;
        float dx = px - cx;
        dx = Math.Clamp(dx, -LeapMaxDistance, LeapMaxDistance);
        _leapTargetX = cx + dx;
        if (ArenaBounds is Aabb arena)
            _leapTargetX = Math.Clamp(_leapTargetX, arena.Left + Width / 2f, arena.Right - Width / 2f);
        FacePlayer(player);
        CurrentState = BossState.Attack;
        LeapStarted?.Invoke();
    }

    private void UpdateLeap(float deltaTime, World world, Player player)
    {
        if (_leapWindupTime > 0f)
        {
            _leapWindupTime -= deltaTime;
            VelocityX = 0f;
            VelocityY = Physics.ApplyGravity(VelocityY, deltaTime);
            Physics.MoveX(this, world.Solids, deltaTime);
            Physics.MoveY(this, world.Solids, deltaTime);
            ClampArena();
            if (_leapWindupTime <= 0f)
            {
                // Launch: hitung Vx agar tiba di target dalam LeapAirTime.
                float cx = X + Width / 2f;
                float dx = _leapTargetX - cx;
                VelocityX = dx / LeapAirTime;
                VelocityY = LeapVelocityY;
                Grounded = false;
                _leapAirborne = true;
            }
            return;
        }

        if (_leapAirborne)
        {
            VelocityY = Physics.ApplyGravity(VelocityY, deltaTime);
            Physics.MoveX(this, world.Solids, deltaTime);
            bool wasAirborne = !Grounded;
            Physics.MoveY(this, world.Solids, deltaTime);
            ClampArena();
            if (wasAirborne && Grounded)
            {
                // Landing!
                _leapAirborne = false;
                _leapLandingActive = true;
                _leapLandingTime = LeapActiveDuration;
                _leapHitDone = false;
                VelocityX = 0f;
                UpdateLeapBox();
                if (player.Alive && _leapBox.Overlaps(player.Bounds))
                {
                    player.TakeDamage(ScaledLeapDamage);
                    _leapHitDone = true;
                }
            }
            else if (!Grounded)
            {
                // Masih di udara: update box untuk visual, tanpa damage.
                UpdateLeapBox();
            }
            return;
        }

        if (_leapLandingActive)
        {
            _leapLandingTime -= deltaTime;
            VelocityX = 0f;
            VelocityY = Physics.ApplyGravity(VelocityY, deltaTime);
            Physics.MoveX(this, world.Solids, deltaTime);
            Physics.MoveY(this, world.Solids, deltaTime);
            ClampArena();
            UpdateLeapBox();
            if (!_leapHitDone && player.Alive && _leapBox.Overlaps(player.Bounds))
            {
                player.TakeDamage(ScaledLeapDamage);
                _leapHitDone = true;
            }
            if (_leapLandingTime <= 0f)
            {
                _leapLandingActive = false;
                _isLeaping = false;
                _leapCooldown = LeapCooldown;
                CurrentState = BossState.Chase;
            }
            return;
        }

        _isLeaping = false;
        CurrentState = BossState.Chase;
    }

    private void UpdateLeapBox()
    {
        float x = X + (Width - LeapWidth) / 2f;
        float y = Y + Height - LeapHeight;
        _leapBox = new Aabb(x, y, LeapWidth, LeapHeight);
    }

    private void ClampArena()
    {
        if (ArenaBounds is Aabb arena)
        {
            if (X < arena.Left) { X = arena.Left; VelocityX = Math.Max(0f, VelocityX); }
            float maxX = arena.Right - Width;
            if (X > maxX) { X = maxX; VelocityX = Math.Min(0f, VelocityX); }
        }
        else
        {
            if (X < 0f) X = 0f;
        }
    }

    public override void Render(IntPtr renderer, Camera camera)
    {
        // Tetap terlihat sebentar saat mati (death delay), hilang setelah Defeated.
        if (BossDefeated)
            return;
        if (!Alive && DeathTimeLeft <= 0f)
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

        // PhaseTransition: kedip ungu sebagai feedback visual.
        if (CurrentState == BossState.PhaseTransition)
        {
            bool blink = (int)(_phaseTransitionTime / 0.12f) % 2 == 0;
            SDL.SDL_Rect tr = new() { x = sx, y = sy, w = Width, h = Height };
            if (blink)
                SDL.SDL_SetRenderDrawColor(renderer, 170, 80, 220, 255);
            else
                SDL.SDL_SetRenderDrawColor(renderer, 90, 40, 120, 255);
            SDL.SDL_RenderFillRect(renderer, ref tr);
        }
        else if (!Alive)
        {
            SDL.SDL_Rect corpse = new() { x = sx, y = sy, w = Width, h = Height };
            SDL.SDL_SetRenderDrawColor(renderer, 70, 60, 80, 255);
            SDL.SDL_RenderFillRect(renderer, ref corpse);
        }
        else if (_phase >= 2)
        {
            SDL.SDL_Rect body = new() { x = sx, y = sy, w = Width, h = Height };
            SDL.SDL_SetRenderDrawColor(renderer, 150, 40, 45, 255);
            SDL.SDL_RenderFillRect(renderer, ref body);
        }
        else
        {
            SDL.SDL_Rect body = new() { x = sx, y = sy, w = Width, h = Height };
            SDL.SDL_SetRenderDrawColor(renderer, 110, 110, 130, 255);
            SDL.SDL_RenderFillRect(renderer, ref body);
        }

        // Helm/tanduk agar jauh lebih besar & beda dari Skeleton.
        SDL.SDL_Rect helm = new() { x = sx + 6, y = sy - 8, w = Width - 12, h = 14 };
        SDL.SDL_SetRenderDrawColor(renderer, _phase >= 2 ? (byte)230 : (byte)200, 60, 60, 255);
        SDL.SDL_RenderFillRect(renderer, ref helm);

        // Mata menghadap arah facing.
        int eyeX = _facingDirection >= 0 ? sx + Width - 22 : sx + 8;
        SDL.SDL_Rect eyes = new() { x = eyeX, y = sy + 16, w = 14, h = 6 };
        SDL.SDL_SetRenderDrawColor(renderer, 255, 220, 80, 255);
        SDL.SDL_RenderFillRect(renderer, ref eyes);

        // Senjata/attack indicator saat windup/active.
        if (CurrentState == BossState.Attack && Alive)
        {
            SDL.SDL_Rect blade;
            if (_attackWindup > 0f)
                SDL.SDL_SetRenderDrawColor(renderer, 220, 200, 120, 255);
            else
                SDL.SDL_SetRenderDrawColor(renderer, 255, 90, 70, 255);
            if (_facingDirection >= 0)
                blade = new() { x = sx + Width, y = sy + 10, w = 26, h = 12 };
            else
                blade = new() { x = sx - 26, y = sy + 10, w = 26, h = 12 };
            SDL.SDL_RenderFillRect(renderer, ref blade);
        }

        // Leap indicator.
        if (_isLeaping && Alive)
        {
            SDL.SDL_Rect mark = new() { x = sx, y = sy - 12, w = Width, h = 6 };
            SDL.SDL_SetRenderDrawColor(renderer, 255, 140, 40, 255);
            SDL.SDL_RenderFillRect(renderer, ref mark);
        }
    }
}
