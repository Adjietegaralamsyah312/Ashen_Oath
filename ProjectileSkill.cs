namespace AshenOath;

/// <summary>
/// Skill tembak projectile: instant fire (tanpa jendela aktif),
/// cooldown + cost dikelola base <see cref="Skill"/>.
/// </summary>
public sealed class ProjectileSkill : Skill
{
    public const int FireCost = 20;
    public const float FireCooldown = 0.55f;

    public ProjectileSkill()
        : base("Projectile", FireCooldown, FireCost)
    {
    }

    /// <summary>Aktif seketika lalu selesai; cooldown tetap berjalan.</summary>
    public bool TryFire(float energy)
    {
        if (!TryActivate(energy))
            return false;
        Cancel();
        return true;
    }
}
