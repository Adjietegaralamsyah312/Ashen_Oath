using SDL2;

namespace AshenOath;

public enum Facing
{
    Right,
    Left
}

/// <summary>
/// Horizontal sama seperti Tahap 2 (250 px/s, delta time).
/// Vertikal dimiliki physics: gravity, jump (grounded saja), tabrakan AABB
/// axis-separated via <see cref="Physics"/> + <see cref="World"/>.
/// Render sprite/animasi sama seperti Tahap 3 (Idle diam, Walk gerak,
/// flip kiri/kanan), fallback rectangle bila sprite tidak ada.
/// </summary>
public sealed class Player : IDisposable, IPhysicsBody
{
    public const int Width = 40;
    public const int Height = 40;
    public const float Speed = 250f;
    public const float JumpVelocity = -500f;

    public const int MaxHp = 100;
    public const int BaseAttack = 10;
    public const int BaseDefense = 0;
    public const int BaseMaxHp = 100;
    public const float BaseMaxEnergy = 100f;
    /// <summary>Growth per level Tahap 19 (ditambah ke base Level 1).</summary>
    public const int HpPerLevel = 10;
    public const int AttackPerLevel = 2;
    public const int DefensePerLevel = 1;
    public const float EnergyPerLevel = 5f;
    /// <summary>Durasi feedback visual level-up.</summary>
    public const float LevelUpDuration = 1.5f;
    public const int AttackDamage = 10;
    public const float AttackCooldown = 0.35f;
    public const float AttackDuration = 0.12f;
    public const float InvulnDuration = 0.7f;
    public const float DeathDelay = 1.0f;
    public const float HitFlashDuration = 0.15f;
    public const float MaxEnergy = 100f;
    public const float EnergyRegen = 20f;
    public const int AttackWidth = 45;
    public const int AttackHeight = 32;

    // Layout sheet, harus sama dengan Assets/Player/player.png:
    // 192x128, cell 32x32.
    // Baris 0: Idle 4 (kol 0..3). Baris 1: Walk 6 (kol 0..5).
    // Baris 2: Jump 2 (kol 0..1), Fall 2 (kol 2..3), Attack 2 (kol 4..5).
    // Baris 3: Hurt 2 (kol 0..1), Death 4 (kol 2..5, non-looping).
    private const int FrameSize = 32;
    private const int IdleFrameCount = 4;
    private const int WalkFrameCount = 6;
    private const int JumpFrameCount = 2;
    private const int FallFrameCount = 2;
    private const int AttackFrameCount = 2;
    private const int HurtFrameCount = 2;
    private const int DeathFrameCount = 4;
    private const float IdleFrameDuration = 0.18f;
    private const float WalkFrameDuration = 0.12f;
    private const float JumpFrameDuration = 0.12f;
    private const float FallFrameDuration = 0.15f;
    private const float AttackFrameDuration = 0.06f;
    private const float HurtFrameDuration = 0.08f;
    private const float DeathFrameDuration = 0.2f;
    private const int DashFrameCount = 2;
    private const float DashFrameDuration = 0.075f;
    private const int BashFrameCount = 3;
    private const float BashFrameDuration = 0.065f;
    private const int CastFrameCount = 2;
    private const float CastFrameDuration = 0.10f;
    public const float HurtDuration = 0.25f;
    private const string AssetRelativePath = "Assets/Player/player.png";

    public float X { get; set; }
    public float Y { get; set; }
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }
    public bool Grounded { get; set; }
    public Facing Facing { get; private set; } = Facing.Right;
    public bool IsMoving { get; private set; }
    public int Hp { get; private set; } = MaxHp;
    public bool Alive { get; private set; } = true;
    public float DeathTimeLeft { get; private set; }
    public float Energy { get; private set; } = MaxEnergy;
    public DashSkill Dash { get; } = new();
    public ShieldBashSkill ShieldBash { get; } = new();
    public ProjectileSkill Projectile { get; } = new();
    public Inventory Inventory { get; } = new();
    public Equipment Equipment { get; } = new();
    public PlayerProgression Progression { get; } = new();
    public float LevelUpTimeLeft { get; private set; }
    public bool ShowLevelUp => LevelUpTimeLeft > 0f;

    /// <summary>Base Level 1 + growth per level (tanpa equipment).</summary>
    public int LevelBaseMaxHp => BaseMaxHp + (Progression.Level - 1) * HpPerLevel;
    public int LevelBaseAttack => BaseAttack + (Progression.Level - 1) * AttackPerLevel;
    public int LevelBaseDefense => BaseDefense + (Progression.Level - 1) * DefensePerLevel;
    public float LevelBaseEnergy => BaseMaxEnergy + (Progression.Level - 1) * EnergyPerLevel;

    /// <summary>Stat efektif Tahap 19: level-base + bonus equipment.</summary>
    public int EffectiveAttack => LevelBaseAttack + (ItemDatabase.Get(Equipment.WeaponId ?? string.Empty)?.AttackBonus ?? 0);
    public int EffectiveDefense => LevelBaseDefense + (ItemDatabase.Get(Equipment.ArmorId ?? string.Empty)?.DefenseBonus ?? 0);
    public int EffectiveMaxHp => LevelBaseMaxHp + (ItemDatabase.Get(Equipment.ArmorId ?? string.Empty)?.MaxHpBonus ?? 0);
    public float EffectiveMaxEnergy => LevelBaseEnergy + (ItemDatabase.Get(Equipment.AccessoryId ?? string.Empty)?.MaxEnergyBonus ?? 0);

    /// <summary>
    /// Tambah XP; tiap level heal +10 HP / +5 EN (clamp max).
    /// Kembalikan jumlah level naik (0 bila mati / tidak naik).
    /// </summary>
    public int GainXp(int amount)
    {
        if (!Alive)
            return 0;
        int ups = Progression.AddXp(amount);
        for (int i = 0; i < ups; i++)
        {
            Hp = Math.Min(EffectiveMaxHp, Hp + HpPerLevel);
            Energy = Math.Min(EffectiveMaxEnergy, Energy + EnergyPerLevel);
        }
        if (ups > 0)
        {
            LevelUpTimeLeft = LevelUpDuration;
            _flashTime = Math.Max(_flashTime, LevelUpDuration);
        }
        return ups;
    }

    /// <summary>Bonus attack untuk damage skill: EffectiveAttack - BaseAttack.</summary>
    public int AttackBonus => EffectiveAttack - BaseAttack;

    /// <summary>
    /// Clamp HP/Energy ke max efektif (setelah equip/unequip/load).
    /// Tidak pernah membunuh player.
    /// </summary>
    public void ClampStatsToEffective()
    {
        Hp = Math.Clamp(Hp, 1, EffectiveMaxHp);
        Energy = Math.Clamp(Energy, 0f, EffectiveMaxEnergy);
    }
    public bool HasIframes => Dash.HasIframes;
    public int BashId { get; private set; }
    public bool IsBashActive => ShieldBash.IsActive;
    public Hitbox BashHitbox => _bashBox;
    public const float CastDuration = 0.20f;
    public bool IsFlashing => _flashTime > 0f;
    public bool IsAttackActive => _attackTimeLeft > 0f;
    public int AttackId { get; private set; }
    public Hitbox AttackHitbox => _attackBox;
    public bool HasSprite => _texture is not null && _texture.IsValid && _current is not null;
    public string CurrentAnimationName => _current?.Name ?? "Fallback";
    public PlayerAnimState AnimState { get; private set; } = PlayerAnimState.Idle;

    /// <summary>Event gameplay untuk audio: lompat, serang, kena damage, mati.</summary>
    public event Action? Jumped;
    public event Action? Attacked;
    public event Action? Hurt;
    public event Action? Died;
    public Aabb Bounds => new(X, Y, Width, Height);
    public int BodyWidth => Width;
    public int BodyHeight => Height;

    private Texture? _texture;
    private Animation? _idle;
    private Animation? _walk;
    private Animation? _jump;
    private Animation? _fall;
    private Animation? _attackAnim;
    private Animation? _hurt;
    private Animation? _death;
    private Animation? _dashAnim;
    private Animation? _bashAnim;
    private Animation? _castAnim;
    private Animation? _current;
    private Sprite? _sprite;
    private bool _contentLoaded;
    private bool _disposed;

    private float _attackCooldown;
    private float _attackTimeLeft;
    private float _invulnTime;
    private float _flashTime;
    private float _hurtTime;
    private float _castTime;
    private int _hitIdCounter;
    private Hitbox _attackBox;
    private Hitbox _bashBox;

    /// <summary>Id hit unik lintas attack/bash agar single-hit tetap benar.</summary>
    public int NextHitId() => ++_hitIdCounter;

    public Player(float startX, float startY)
    {
        X = startX;
        Y = startY;
    }

    public void LoadContent(IntPtr renderer)
    {
        if (_contentLoaded)
            return;
        _contentLoaded = true;

        string? path = ResolveAssetPath();
        if (path is null)
        {
            Console.Error.WriteLine($"[Player] asset '{AssetRelativePath}' not found, using fallback rectangle.");
            return;
        }

        _texture = Texture.Load(renderer, path);
        if (!_texture.IsValid)
        {
            Console.Error.WriteLine("[Player] sprite unavailable, using fallback rectangle.");
            _texture.Dispose();
            _texture = null;
            return;
        }

        var idleFrames = BuildRowFrames(row: 0, colStart: 0, count: IdleFrameCount);
        var walkFrames = BuildRowFrames(row: 1, colStart: 0, count: WalkFrameCount);
        if (idleFrames.Count == 0 || walkFrames.Count == 0)
        {
            Console.Error.WriteLine("[Player] sprite sheet too small, using fallback rectangle.");
            _texture.Dispose();
            _texture = null;
            return;
        }

        _idle = new Animation("Idle", idleFrames, IdleFrameDuration, loop: true);
        _walk = new Animation("Walk", walkFrames, WalkFrameDuration, loop: true);
        _jump = TryMakeAnimation("Jump", BuildRowFrames(row: 2, colStart: 0, count: JumpFrameCount), JumpFrameDuration, loop: true);
        _fall = TryMakeAnimation("Fall", BuildRowFrames(row: 2, colStart: 2, count: FallFrameCount), FallFrameDuration, loop: true);
        _attackAnim = TryMakeAnimation("Attack", BuildRowFrames(row: 2, colStart: 4, count: AttackFrameCount), AttackFrameDuration, loop: false);
        _hurt = TryMakeAnimation("Hurt", BuildRowFrames(row: 3, colStart: 0, count: HurtFrameCount), HurtFrameDuration, loop: false);
        _death = TryMakeAnimation("Death", BuildRowFrames(row: 3, colStart: 2, count: DeathFrameCount), DeathFrameDuration, loop: false);
        _dashAnim = TryMakeAnimation("Dash", BuildRowFrames(row: 4, colStart: 0, count: DashFrameCount), DashFrameDuration, loop: true);
        _bashAnim = TryMakeAnimation("ShieldBash", BuildRowFrames(row: 5, colStart: 0, count: BashFrameCount), BashFrameDuration, loop: false);
        _castAnim = TryMakeAnimation("ProjectileCast", BuildRowFrames(row: 5, colStart: 3, count: CastFrameCount), CastFrameDuration, loop: false);
        _current = _idle;
        AnimState = PlayerAnimState.Idle;
        _sprite = new Sprite(_texture, _current.Current, Width, Height);
        Console.WriteLine($"[Player] sprite ready (Idle {idleFrames.Count}, Walk {walkFrames.Count}, Jump {_jump?.FrameCount ?? 0}, Fall {_fall?.FrameCount ?? 0}, Attack {_attackAnim?.FrameCount ?? 0}, Hurt {_hurt?.FrameCount ?? 0}, Death {_death?.FrameCount ?? 0}, Dash {_dashAnim?.FrameCount ?? 0}, Bash {_bashAnim?.FrameCount ?? 0}, Cast {_castAnim?.FrameCount ?? 0}).");
    }

    /// <summary>Animasi opsional: null bila frame tidak tersedia (dipakai fallback Idle).</summary>
    private static Animation? TryMakeAnimation(string name, List<SDL.SDL_Rect> frames, float frameDuration, bool loop)
    {
        if (frames.Count == 0)
        {
            Console.Error.WriteLine($"[Player] animation '{name}' frames missing, falling back to Idle.");
            return null;
        }
        return new Animation(name, frames, frameDuration, loop);
    }

    public void Update(float dirX, bool jumpPressed, float deltaTime, World world)
    {
        if (!Alive)
        {
            // Animasi Death tetap berjalan; physics beku; respawn ikut DeathDelay.
            _current?.Update(deltaTime);
            SyncSprite();
            return;
        }

        bool dashing = Dash.IsActive;
        if (dashing)
            VelocityX = Dash.Direction * DashSkill.DashSpeed;
        else
            VelocityX = dirX * Speed;

        if (jumpPressed && Grounded)
        {
            VelocityY = JumpVelocity;
            Grounded = false;
            Jumped?.Invoke();
        }

        VelocityY = Physics.ApplyGravity(VelocityY, deltaTime);
        Physics.MoveX(this, world.Solids, deltaTime);
        if (dashing && VelocityX == 0f)
            Dash.Cancel(); // Menabrak wall: dash berakhir aman di posisi snap.
        Physics.MoveY(this, world.Solids, deltaTime);

        Dash.Update(deltaTime);
        if (!dashing)
            Energy = Math.Min(EffectiveMaxEnergy, Energy + EnergyRegen * deltaTime);

        if (_attackCooldown > 0f)
        {
            _attackCooldown -= deltaTime;
            if (_attackCooldown < 0f)
                _attackCooldown = 0f;
        }
        if (_invulnTime > 0f)
        {
            _invulnTime -= deltaTime;
            if (_invulnTime < 0f)
                _invulnTime = 0f;
        }
        if (_flashTime > 0f)
        {
            _flashTime -= deltaTime;
            if (_flashTime < 0f)
                _flashTime = 0f;
        }
        if (LevelUpTimeLeft > 0f)
        {
            LevelUpTimeLeft -= deltaTime;
            if (LevelUpTimeLeft < 0f)
                LevelUpTimeLeft = 0f;
        }
        if (_hurtTime > 0f)
        {
            _hurtTime -= deltaTime;
            if (_hurtTime < 0f)
                _hurtTime = 0f;
        }
        if (IsAttackActive)
        {
            _attackTimeLeft -= deltaTime;
            if (_attackTimeLeft <= 0f)
                _attackTimeLeft = 0f;
            else
                UpdateAttackBox();
        }
        ShieldBash.Update(deltaTime);
        Projectile.Update(deltaTime);
        if (IsBashActive)
            UpdateBashBox();
        if (_castTime > 0f)
        {
            _castTime -= deltaTime;
            if (_castTime < 0f)
                _castTime = 0f;
        }

        if (X < 0f) X = 0f;
        if (Y < 0f) Y = 0f;
        float maxX = world.Width - Width;
        float maxY = world.Height - Height;
        if (X > maxX) X = maxX;
        if (Y > maxY) Y = maxY;

        if (dirX < 0f)
            Facing = Facing.Left;
        else if (dirX > 0f)
            Facing = Facing.Right;

        IsMoving = dirX != 0f;

        UpdateAnimState(deltaTime);
    }

    /// <summary>
    /// Pilih animation state dari gameplay state (prioritas:
    /// Death > Hurt > Attack > Jump > Fall > Walk > Idle).
    /// Reset hanya saat state berubah; animasi hilang fallback ke Idle.
    /// </summary>
    private void UpdateAnimState(float deltaTime)
    {
        PlayerAnimState wanted = SelectAnimState();
        Animation? target = ResolveAnimation(wanted) ?? _idle;
        if (AnimState != wanted || !ReferenceEquals(target, _current))
        {
            AnimState = wanted;
            if (target is not null && !ReferenceEquals(target, _current))
            {
                _current = target;
                _current.Reset();
            }
        }
        _current?.Update(deltaTime);
        SyncSprite();
    }

    private PlayerAnimState SelectAnimState()
    {
        if (!Alive)
            return PlayerAnimState.Death;
        if (_hurtTime > 0f)
            return PlayerAnimState.Hurt;
        if (IsBashActive)
            return PlayerAnimState.ShieldBash;
        if (IsAttackActive)
            return PlayerAnimState.Attack;
        if (_castTime > 0f)
            return PlayerAnimState.ProjectileCast;
        if (Dash.IsActive)
            return PlayerAnimState.Dash;
        if (!Grounded)
            return VelocityY < 0f ? PlayerAnimState.Jump : PlayerAnimState.Fall;
        return IsMoving ? PlayerAnimState.Walk : PlayerAnimState.Idle;
    }

    private Animation? ResolveAnimation(PlayerAnimState state)
    {
        return state switch
        {
            PlayerAnimState.Idle => _idle,
            PlayerAnimState.Walk => _walk,
            PlayerAnimState.Jump => _jump,
            PlayerAnimState.Fall => _fall,
            PlayerAnimState.Attack => _attackAnim,
            PlayerAnimState.Hurt => _hurt,
            PlayerAnimState.Death => _death,
            PlayerAnimState.Dash => _dashAnim,
            PlayerAnimState.ShieldBash => _bashAnim,
            PlayerAnimState.ProjectileCast => _castAnim,
            _ => _idle,
        };
    }

    private void SyncSprite()
    {
        if (_sprite is not null && _current is not null)
            _sprite.Source = _current.Current;
    }

    public void ResetToSpawn(float spawnX, float spawnY)
    {
        X = spawnX;
        Y = spawnY;
        VelocityX = 0f;
        VelocityY = 0f;
        Grounded = false;
        IsMoving = false;
        Hp = EffectiveMaxHp;
        Energy = EffectiveMaxEnergy;
        Dash.Reset();
        ShieldBash.Reset();
        Projectile.Reset();
        _castTime = 0f;
        _invulnTime = 0f;
        _attackCooldown = 0f;
        _attackTimeLeft = 0f;
        _hurtTime = 0f;
        LevelUpTimeLeft = 0f;
        AnimState = PlayerAnimState.Idle;
        if (_idle is not null)
        {
            _current = _idle;
            _current.Reset();
            if (_sprite is not null)
                _sprite.Source = _current.Current;
        }
    }

    /// <summary>
    /// Mulai attack bila hidup, cooldown habis, dan tidak sedang menyerang.
    /// Hitbox mengikuti Facing dan diperbarui tiap frame selama aktif.
    /// True bila attack baru dimulai (untuk SFX sekali per swing).
    /// </summary>
    public bool TryStartAttack()
    {
        if (!Alive || _hurtTime > 0f || Dash.IsActive || IsBashActive || _attackCooldown > 0f || IsAttackActive)
            return false;
        AttackId = NextHitId();
        _attackCooldown = AttackCooldown;
        _attackTimeLeft = AttackDuration;
        UpdateAttackBox();
        Attacked?.Invoke();
        return true;
    }

    /// <summary>
    /// Terima damage; defense equipment mengurangi: final = max(1, amount - Defense).
    /// Diabaikan saat mati atau selama invulnerability window.
    /// Lethal damage mematikan player dan memulai death delay.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (!Alive || amount <= 0 || _invulnTime > 0f || Dash.HasIframes)
            return;
        int finalDamage = Math.Max(1, amount - EffectiveDefense);
        Hp = Math.Max(0, Hp - finalDamage);
        _invulnTime = InvulnDuration;
        _flashTime = HitFlashDuration;
        if (Hp <= 0)
        {
            Alive = false;
            DeathTimeLeft = DeathDelay;
            VelocityX = 0f;
            VelocityY = 0f;
            _attackTimeLeft = 0f;
            _hurtTime = 0f;
            Dash.Reset();
            ShieldBash.Reset();
            Projectile.Reset();
            _castTime = 0f;
            AnimState = PlayerAnimState.Death;
            _current = ResolveAnimation(PlayerAnimState.Death) ?? _idle;
            _current?.Reset();
            SyncSprite();
            Died?.Invoke();
        }
        else
        {
            _hurtTime = HurtDuration;
            Dash.Cancel(); // Dash tidak boleh aktif saat hurt.
            ShieldBash.Cancel(); // Begitu pula Shield Bash (cooldown tetap jalan).
            Hurt?.Invoke();
        }
    }

    /// <summary>
    /// Hitung mundur death delay; true saat delay habis (siap respawn).
    /// </summary>
    public bool TickDeathDelay(float deltaTime)
    {
        if (Alive)
            return false;
        DeathTimeLeft -= deltaTime;
        return DeathTimeLeft <= 0f;
    }

    /// <summary>
    /// Hidup kembali penuh di titik spawn/checkpoint: HP, velocity,
    /// animasi Idle, facing kanan, invuln + cooldown + flash dibersihkan.
    /// </summary>
    public void Respawn(float spawnX, float spawnY)
    {
        ResetToSpawn(spawnX, spawnY);
        Alive = true;
        DeathTimeLeft = 0f;
        _flashTime = 0f;
        Facing = Facing.Right;
    }

    private void DrawSprite(IntPtr renderer, Camera camera, float sx, float sy)
    {
        var flip = Facing == Facing.Left
            ? SDL.SDL_RendererFlip.SDL_FLIP_HORIZONTAL
            : SDL.SDL_RendererFlip.SDL_FLIP_NONE;
        SDL.SDL_Rect source = _sprite!.Source;
        SpriteRenderer.Draw(renderer, _texture!.Handle, in source,
            (int)sx, (int)sy, _sprite.RenderWidth, _sprite.RenderHeight, flip);
    }

    /// <summary>
    /// Dash ke arah Facing. Ditolak saat mati/hurt/attack-aktif/cooldown/energy kurang.
    /// True bila dash baru dimulai (untuk SFX sekali per dash).
    /// </summary>
    public bool TryDash()
    {
        if (!Alive || _hurtTime > 0f || IsAttackActive || IsBashActive)
            return false;
        float direction = Facing == Facing.Left ? -1f : 1f;
        if (!Dash.TryDash(Energy, direction))
            return false;
        Energy = Math.Max(0f, Energy - DashSkill.DashCost);
        return true;
    }

    /// <summary>
    /// Shield Bash ke arah Facing. Saling tolak dengan attack/dash/fire,
    /// mati, dan hurt. True bila aktivasi baru dimulai (SFX sekali).
    /// </summary>
    public bool TryShieldBash()
    {
        if (!Alive || _hurtTime > 0f || IsAttackActive || Dash.IsActive || IsBashActive)
            return false;
        if (!ShieldBash.TryBash(Energy))
            return false;
        Energy = Math.Max(0f, Energy - ShieldBashSkill.BashCost);
        BashId = NextHitId();
        UpdateBashBox();
        return true;
    }

    /// <summary>
    /// Tembak projectile ke arah Facing. Instant; cooldown + cost via skill.
    /// True bila tembakan baru terjadi (SFX + spawn sekali).
    /// </summary>
    public bool TryFireProjectile()
    {
        if (!Alive || _hurtTime > 0f || IsAttackActive || IsBashActive || Dash.IsActive)
            return false;
        if (!Projectile.TryFire(Energy))
            return false;
        Energy = Math.Max(0f, Energy - ProjectileSkill.FireCost);
        _castTime = CastDuration;
        return true;
    }

    /// <summary>
    /// Minum small_potion (tombol H): heal, clamp ke MaxHP efektif.
    /// Ditolak saat mati, HP penuh, atau potion habis.
    /// </summary>
    public bool TryDrinkPotion()
    {
        if (!Alive)
            return false;
        if (Hp >= EffectiveMaxHp)
            return false;
        var def = ItemDatabase.Get("small_potion");
        if (def is null || !Inventory.HasItem("small_potion"))
            return false;
        if (!Inventory.RemoveItem("small_potion"))
            return false;
        Hp = Math.Min(EffectiveMaxHp, Hp + def.HealAmount);
        return true;
    }

    private void UpdateAttackBox()
    {
        float x = Facing == Facing.Right ? X + Width : X - AttackWidth;
        float y = Y + (Height - AttackHeight) / 2f;
        _attackBox = new Hitbox(x, y, AttackWidth, AttackHeight);
    }

    private void UpdateBashBox()
    {
        float x = Facing == Facing.Right ? X + Width : X - ShieldBashSkill.BashWidth;
        float y = Y + (Height - ShieldBashSkill.BashHeight) / 2f;
        _bashBox = new Hitbox(x, y, ShieldBashSkill.BashWidth, ShieldBashSkill.BashHeight);
    }

    public void Render(IntPtr renderer, Camera camera)
    {
        float sx = X - camera.RenderX;
        float sy = Y - camera.RenderY;

        if (!Alive)
        {
            if (HasSprite && _sprite is not null && _current is not null && _texture is not null)
            {
                DrawSprite(renderer, camera, sx, sy);
                return;
            }
            // Corpse sederhana bila sprite tidak ada: rectangle merah gelap.
            SDL.SDL_Rect corpse = new()
            {
                x = (int)sx,
                y = (int)sy,
                w = Width,
                h = Height
            };
            SDL.SDL_SetRenderDrawColor(renderer, 140, 45, 45, 255);
            SDL.SDL_RenderFillRect(renderer, ref corpse);
            return;
        }

        if (IsFlashing && (int)(_flashTime / 0.05f) % 2 == 0)
            return; // Kedip damage feedback; sistem animasi tidak diubah.

        if (HasSprite && _sprite is not null && _current is not null && _texture is not null)
        {
            DrawSprite(renderer, camera, sx, sy);
            return;
        }

        SDL.SDL_Rect rect = new()
        {
            x = (int)sx,
            y = (int)sy,
            w = Width,
            h = Height
        };

        SDL.SDL_SetRenderDrawColor(renderer, 220, 90, 60, 255);
        SDL.SDL_RenderFillRect(renderer, ref rect);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _texture?.Dispose();
        _texture = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private List<SDL.SDL_Rect> BuildRowFrames(int row, int colStart, int count)
    {
        var frames = new List<SDL.SDL_Rect>();
        if (_texture is null)
            return frames;
        for (int col = colStart; col < colStart + count; col++)
        {
            int x = col * FrameSize;
            int y = row * FrameSize;
            if (x + FrameSize <= _texture.Width && y + FrameSize <= _texture.Height)
                frames.Add(new SDL.SDL_Rect { x = x, y = y, w = FrameSize, h = FrameSize });
        }
        return frames;
    }

    private static string? ResolveAssetPath()
    {
        var candidates = new[]
        {
            AssetRelativePath,
            Path.Combine(AppContext.BaseDirectory, AssetRelativePath),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", AssetRelativePath)),
        };
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }
}
