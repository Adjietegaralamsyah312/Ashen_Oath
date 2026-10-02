namespace AshenOath;

/// <summary>
/// State boss sederhana (tanpa behavior tree).
/// Priority: Dead > PhaseTransition > Stunned > Attack > Chase > Idle.
/// </summary>
public enum BossState
{
    Idle,
    Chase,
    Attack,
    Stunned,
    PhaseTransition,
    Dead
}

/// <summary>
/// Base class untuk boss. Mewarisi Enemy agar kompatibel dengan
/// Basic Attack / Shield Bash / Projectile existing.
/// </summary>
public abstract class Boss : Enemy
{
    public const float DeathDelay = 1.5f;

    private readonly int _width;
    private readonly int _height;
    private readonly float _spawnX;
    private readonly float _spawnY;

    public override int Width => _width;
    public override int Height => _height;
    public override bool IsStunned => _stunTime > 0f || CurrentState == BossState.PhaseTransition;

    public BossState CurrentState { get; protected set; } = BossState.Idle;
    public int Phase => _phase;
    public bool EncounterActive => _encounterActive;
    public bool BossDefeated { get; protected set; }
    public float DeathTimeLeft { get; protected set; }
    public int FacingDirection => _facingDirection;
    public float StunTimeLeft => _stunTime;

    public event Action? EncounterStarted;
    public event Action? EncounterEnded;
    public event Action<int>? PhaseChanged;
    public event Action? Defeated;

    protected float _stunTime;
    protected float _attackCooldown;
    protected float _attackWindup;
    protected bool _attackActive;
    protected int _facingDirection = 1;
    protected int _phase = 1;
    protected float _phaseTransitionTime;
    protected bool _encounterActive;

    protected Boss(float spawnX, float spawnY, int maxHp, int damage, int width, int height)
        : base(spawnX, spawnY, maxHp, damage)
    {
        _spawnX = spawnX;
        _spawnY = spawnY;
        _width = width;
        _height = height;
    }

    public void StartEncounter()
    {
        if (!_encounterActive && Alive && !BossDefeated)
        {
            _encounterActive = true;
            if (CurrentState == BossState.Idle)
                CurrentState = BossState.Chase;
            EncounterStarted?.Invoke();
        }
    }

    public void EndEncounter()
    {
        if (_encounterActive)
        {
            _encounterActive = false;
            EncounterEnded?.Invoke();
        }
    }

    protected void TransitionToPhase(int newPhase)
    {
        if (newPhase <= _phase || !Alive || BossDefeated)
            return;
        _phase = newPhase;
        CurrentState = BossState.PhaseTransition;
        _phaseTransitionTime = 1.0f;
        _stunTime = 0f;
        _attackCooldown = 0f;
        _attackWindup = 0f;
        _attackActive = false;
        PhaseChanged?.Invoke(_phase);
    }

    protected void UpdatePhaseTransition(float deltaTime)
    {
        if (CurrentState != BossState.PhaseTransition)
            return;
        if (_phaseTransitionTime > 0f)
        {
            _phaseTransitionTime -= deltaTime;
            if (_phaseTransitionTime <= 0f)
            {
                _phaseTransitionTime = 0f;
                CurrentState = BossState.Chase;
            }
        }
    }

    protected bool UpdateDeath(float deltaTime)
    {
        if (Alive)
            return false;
        if (CurrentState != BossState.Dead)
            CurrentState = BossState.Dead;
        if (!BossDefeated)
        {
            DeathTimeLeft -= deltaTime;
            if (DeathTimeLeft <= 0f)
            {
                DeathTimeLeft = 0f;
                BossDefeated = true;
                EndEncounter();
                Defeated?.Invoke();
            }
        }
        return true;
    }

    public override void TakeDamage(int amount, float direction, bool applyKnockback = true, float? knockbackForce = null)
    {
        if (!Alive || BossDefeated || amount <= 0)
            return;
        // Selama PhaseTransition boss tetap bisa di-damage (HP berkurang)
        // tapi tidak ter-stun / tidak ter-interrupt.
        bool wasAlive = Alive;
        base.TakeDamage(amount, direction, applyKnockback, knockbackForce);
        if (!Alive && wasAlive)
        {
            CurrentState = BossState.Dead;
            DeathTimeLeft = DeathDelay;
            _stunTime = 0f;
            _attackCooldown = 0f;
            _attackWindup = 0f;
            _attackActive = false;
            OnDied();
        }
        else if (Alive)
        {
            OnDamaged();
        }
    }

    protected virtual void OnDied()
    {
    }

    protected virtual void OnDamaged()
    {
    }

    public override void ApplyStun(float duration)
    {
        if (!Alive || BossDefeated)
            return;
        if (CurrentState == BossState.PhaseTransition || CurrentState == BossState.Dead)
            return;
        if (duration > 0f)
            _stunTime = Math.Max(_stunTime, Math.Min(duration, MaxStunDuration));
    }

    protected virtual float MaxStunDuration => 0.15f;

    protected override void ApplyKnockback(float direction, float force)
    {
        // Boss: knockback kecil (25%).
        VelocityX = direction * force * 0.25f;
    }

    /// <summary>
    /// Dipanggil saat player mati di tengah encounter:
    /// HP dipertahankan, serangan aktif dibersihkan,
    /// kembali ke state aman (Chase bila encounter aktif).
    /// </summary>
    public virtual void OnPlayerDeathReset()
    {
        if (!Alive || BossDefeated)
            return;
        VelocityX = 0f;
        VelocityY = 0f;
        _stunTime = 0f;
        _attackCooldown = 0f;
        _attackWindup = 0f;
        _attackActive = false;
        OnClearAttacks();
        CurrentState = _encounterActive ? BossState.Chase : BossState.Idle;
    }

    protected virtual void OnClearAttacks()
    {
    }

    public virtual void FullReset()
    {
        X = _spawnX;
        Y = _spawnY;
        VelocityX = 0f;
        VelocityY = 0f;
        Grounded = false;
        CurrentState = BossState.Idle;
        _stunTime = 0f;
        _attackCooldown = 0f;
        _attackWindup = 0f;
        _attackActive = false;
        _phase = 1;
        _phaseTransitionTime = 0f;
        _encounterActive = false;
        BossDefeated = false;
        DeathTimeLeft = 0f;
        OnClearAttacks();
    }

    /// <summary>
    /// Terapkan status defeated dari save (Continue): boss tetap mati
    /// tanpa mengulang death delay. Tidak me-revive boss hidup.
    /// Silent: tidak menembakkan event Died agar tidak memicu XP/loot/SFX saat load.
    /// </summary>
    public void ApplySaveDefeated()
    {
        Hp = 0;
        Alive = false;
        State = EnemyState.Dead;
        CurrentState = BossState.Dead;
        DeathTimeLeft = 0f;
        BossDefeated = true;
        _encounterActive = false;
        _stunTime = 0f;
        _attackCooldown = 0f;
        _attackWindup = 0f;
        _attackActive = false;
        VelocityX = 0f;
        VelocityY = 0f;
        OnClearAttacks();
    }

    public override void ResetToSpawn()
    {
        base.ResetToSpawn();
        FullReset();
    }

    public override void Revive()
    {
        base.Revive();
        FullReset();
    }

    protected void UpdateStun(float deltaTime)
    {
        if (_stunTime > 0f)
        {
            _stunTime -= deltaTime;
            if (_stunTime < 0f)
                _stunTime = 0f;
        }
    }

    protected void FacePlayer(Player player)
    {
        float cx = X + Width / 2f;
        float px = player.X + Player.Width / 2f;
        _facingDirection = px >= cx ? 1 : -1;
    }

    protected float HorizontalDistanceToPlayer(Player player)
    {
        float cx = X + Width / 2f;
        float px = player.X + Player.Width / 2f;
        return Math.Abs(px - cx);
    }

    protected virtual void UpdateCooldowns(float deltaTime)
    {
        if (_attackCooldown > 0f)
        {
            _attackCooldown -= deltaTime;
            if (_attackCooldown < 0f)
                _attackCooldown = 0f;
        }
        if (_attackWindup > 0f)
        {
            _attackWindup -= deltaTime;
            if (_attackWindup < 0f)
            {
                _attackWindup = 0f;
                _attackActive = true;
            }
        }
    }
}
