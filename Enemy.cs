namespace AshenOath;

/// <summary>
/// Base semua musuh: posisi, velocity, HP, damage, alive.
/// TakeDamage mengatur HP + death; knockback diserahkan ke subclass.
/// </summary>
public abstract class Enemy : IPhysicsBody
{
    public float X { get; set; }
    public float Y { get; set; }
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }
    public bool Grounded { get; set; }

    public int Hp { get; protected set; }
    public int MaxHp { get; protected set; }
    public virtual int Damage { get; protected set; }
    public bool Alive { get; protected set; } = true;
    public EnemyState State { get; protected set; } = EnemyState.Patrol;

    /// <summary>Difficulty saat spawn (Normal = nilai existing).</summary>
    public Difficulty Difficulty { get; protected set; } = Difficulty.Normal;

    /// <summary>Pengali kecepatan gerak (1.0 Normal).</summary>
    protected float SpeedMultiplier { get; private set; } = 1f;

    private bool _difficultyApplied;

    /// <summary>SFX saat kena hit (Slime default "hit").</summary>
    public virtual string HurtSfx => "hit";

    /// <summary>Id attack terakhir yang sudah mengenai enemy ini (anti multi-hit).</summary>
    public int LastHitAttackId { get; set; } = -1;

    /// <summary>True saat stunned (tidak patrol/menyerang, gravity tetap jalan).</summary>
    public virtual bool IsStunned => false;

    /// <summary>Event gameplay untuk audio: kena hit (selamat), mati.</summary>
    public event Action? Damaged;
    public event Action? Died;

    public abstract int Width { get; }
    public abstract int Height { get; }
    public int BodyWidth => Width;
    public int BodyHeight => Height;
    public Aabb Bounds => new(X, Y, Width, Height);

    private readonly float _spawnX;
    private readonly float _spawnY;

    protected Enemy(float spawnX, float spawnY, int maxHp, int damage)
    {
        _spawnX = spawnX;
        _spawnY = spawnY;
        X = spawnX;
        Y = spawnY;
        MaxHp = maxHp;
        Hp = maxHp;
        Damage = damage;
    }

    public virtual void TakeDamage(int amount, float direction, bool applyKnockback = true, float? knockbackForce = null)
    {
        if (!Alive || amount <= 0)
            return;
        Hp -= amount;
        if (Hp <= 0)
        {
            Hp = 0;
            Alive = false;
            State = EnemyState.Dead;
            Died?.Invoke();
        }
        else
        {
            if (applyKnockback)
                ApplyKnockback(direction, knockbackForce ?? DefaultKnockbackForce);
            Damaged?.Invoke();
        }
    }

    protected virtual float DefaultKnockbackForce => 0f;

    protected virtual void ApplyKnockback(float direction, float force)
    {
    }

    public virtual void ApplyStun(float duration)
    {
    }

    /// <summary>
    /// Terapkan difficulty sekali saat spawn via <see cref="DifficultyModifiers"/>.
    /// Panggilan ulang diabaikan agar tidak compounding.
    /// </summary>
    public virtual void ApplyDifficulty(Difficulty difficulty)
    {
        if (_difficultyApplied)
            return;
        _difficultyApplied = true;
        Difficulty = DifficultyModifiers.IsDefined(difficulty) ? difficulty : Difficulty.Normal;
        MaxHp = DifficultyModifiers.ScaleEnemyHp(MaxHp, Difficulty);
        Hp = MaxHp;
        Damage = DifficultyModifiers.ScaleEnemyDamage(Damage, Difficulty);
        SpeedMultiplier = DifficultyModifiers.EnemySpeedMult(Difficulty);
    }

    public virtual void ResetToSpawn()
    {
        X = _spawnX;
        Y = _spawnY;
        VelocityX = 0f;
        VelocityY = 0f;
        Grounded = false;
    }

    /// <summary>
    /// Hidup kembali penuh: posisi/velocity, HP, Alive, anti multi-hit.
    /// State sisa (mis. knockback) dibersihkan via <see cref="OnRevived"/>.
    /// </summary>
    public virtual void Revive()
    {
        ResetToSpawn();
        Hp = MaxHp;
        Alive = true;
        State = EnemyState.Patrol;
        LastHitAttackId = -1;
        OnRevived();
    }

    protected virtual void OnRevived()
    {
    }

    /// <summary>Jarak Euclidean 2D (detection konsisten semua enemy).</summary>
    protected static float DistanceTo(float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public abstract void Update(float deltaTime, World world, Player player);
    public abstract void Render(IntPtr renderer, Camera camera);
}
