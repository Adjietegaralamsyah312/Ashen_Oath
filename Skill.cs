namespace AshenOath;

/// <summary>
/// Base semua skill: cooldown, energy cost, active state.
/// Subclass (mis. DashSkill) menambah behavior + durasi sendiri.
/// </summary>
public abstract class Skill
{
    public string Name { get; }
    public float Cooldown { get; }
    public float CooldownRemaining { get; protected set; }
    public int EnergyCost { get; }
    public bool IsActive { get; protected set; }
    public bool IsReady => !IsActive && CooldownRemaining <= 0f;

    protected Skill(string name, float cooldown, int energyCost)
    {
        Name = name;
        Cooldown = cooldown;
        EnergyCost = energyCost;
    }

    public virtual bool CanActivate(float energy) => IsReady && energy >= EnergyCost;

    /// <summary>Cooldown mulai saat aktivasi BERHASIL; gagal = tidak ada efek.</summary>
    public bool TryActivate(float energy)
    {
        if (!CanActivate(energy))
            return false;
        CooldownRemaining = Cooldown;
        IsActive = true;
        OnActivated();
        return true;
    }

    public virtual void Update(float deltaTime)
    {
        if (CooldownRemaining > 0f)
        {
            CooldownRemaining -= deltaTime;
            if (CooldownRemaining < 0f)
                CooldownRemaining = 0f;
        }
    }

    public void Cancel()
    {
        if (!IsActive)
            return;
        IsActive = false;
        OnDeactivated();
    }

    public virtual void Reset()
    {
        CooldownRemaining = 0f;
        Cancel();
    }

    protected virtual void OnActivated()
    {
    }

    protected virtual void OnDeactivated()
    {
    }
}
