namespace AshenOath;

/// <summary>
/// Progression karakter Tahap 19: XP, level 1..20, curve 100/150/225/340...
/// Threshold(level) = round(100 * 1.5^(level-1)) ke kelipatan 5.
/// Logic independent/testable; Game hanya memanggil AddXp.
/// </summary>
public sealed class PlayerProgression
{
    public const int MaxLevel = 20;
    public const int StartLevel = 1;

    public int Level { get; private set; } = StartLevel;
    public int CurrentXP { get; private set; }
    public int TotalXP { get; private set; }

    public int XPToNextLevel => XpForLevel(Level);
    public bool IsMaxLevel => Level >= MaxLevel;

    public static int XpForLevel(int level)
    {
        int l = Math.Clamp(level, StartLevel, MaxLevel);
        return (int)(Math.Round(100.0 * Math.Pow(1.5, l - 1) / 5.0) * 5);
    }

    public void Reset()
    {
        Level = StartLevel;
        CurrentXP = 0;
        TotalXP = 0;
    }

    /// <summary>
    /// Tambah XP; kembalikan jumlah level naik (multi-level didukung,
    /// sisa XP dibawa). Nilai negatif diabaikan.
    /// </summary>
    public int AddXp(int amount)
    {
        if (amount <= 0)
            return 0;
        CurrentXP += amount;
        TotalXP += amount;
        int ups = 0;
        while (Level < MaxLevel && CurrentXP >= XpForLevel(Level))
        {
            CurrentXP -= XpForLevel(Level);
            Level++;
            ups++;
        }
        return ups;
    }

    /// <summary>
    /// Set dari save: clamp level 1..20, XP >= 0, lalu normalisasi
    /// carry-over bila XP melebihi threshold.
    /// </summary>
    public void SetLevelAndXp(int level, int xp)
    {
        Level = Math.Clamp(level, StartLevel, MaxLevel);
        CurrentXP = Math.Max(0, xp);
        TotalXP = CurrentXP;
        while (Level < MaxLevel && CurrentXP >= XpForLevel(Level))
        {
            CurrentXP -= XpForLevel(Level);
            Level++;
        }
    }
}

/// <summary>XP reward per tipe enemy (Tahap 19). Tipe tak dikenal = 0.</summary>
public static class XpRewards
{
    public const int SlimeXp = 15;
    public const int SkeletonXp = 30;
    public const int BatXp = 25;
    public const int BossXp = 150;

    public static int ForEnemy(Enemy enemy) => enemy switch
    {
        AshenWarden => BossXp,
        Slime => SlimeXp,
        Skeleton => SkeletonXp,
        Bat => BatXp,
        _ => 0,
    };
}
