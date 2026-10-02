namespace AshenOath;

/// <summary>
/// Dash horizontal: burst 800 px/s selama 0.15 detik (~120 px),
/// dengan i-frame 0.10 detik di awal. Arah dikunci saat aktivasi.
/// </summary>
public sealed class DashSkill : Skill
{
    public const float DashCooldown = 1.0f;
    public const int DashCost = 25;
    public const float DashDuration = 0.15f;
    public const float DashSpeed = 800f;
    public const float IframeDuration = 0.10f;

    public float Direction { get; private set; } = 1f;
    public float TimeLeft { get; private set; }
    public float IframesLeft { get; private set; }
    public bool HasIframes => IframesLeft > 0f;

    public DashSkill()
        : base("Dash", DashCooldown, DashCost)
    {
    }

    public bool TryDash(float energy, float direction)
    {
        Direction = direction >= 0f ? 1f : -1f;
        if (!TryActivate(energy))
            return false;
        TimeLeft = DashDuration;
        IframesLeft = IframeDuration;
        return true;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (IframesLeft > 0f)
        {
            IframesLeft -= deltaTime;
            if (IframesLeft < 0f)
                IframesLeft = 0f;
        }
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
        IframesLeft = 0f;
        base.Reset();
    }
}
