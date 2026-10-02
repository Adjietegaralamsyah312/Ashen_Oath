namespace AshenOath;

/// <summary>
/// Shield Bash: serangan melee depan, damage 20, knockback 380,
/// stun 0.8 detik. Jendela aktif 0.20 detik mengikuti Facing.
/// </summary>
public sealed class ShieldBashSkill : Skill
{
    public const int BashCost = 30;
    public const float BashCooldown = 1.25f;
    public const float BashDuration = 0.20f;
    public const int BashDamage = 20;
    public const float BashKnockback = 380f;
    public const float BashStun = 0.8f;
    public const int BashWidth = 55;
    public const int BashHeight = 38;

    public float TimeLeft { get; private set; }

    public ShieldBashSkill()
        : base("ShieldBash", BashCooldown, BashCost)
    {
    }

    public bool TryBash(float energy)
    {
        if (!TryActivate(energy))
            return false;
        TimeLeft = BashDuration;
        return true;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (IsActive)
        {
            TimeLeft -= deltaTime;
            if (TimeLeft <= 0f)
                Cancel();
        }
    }

    public override void Reset()
    {
        TimeLeft = 0f;
        base.Reset();
    }
}
