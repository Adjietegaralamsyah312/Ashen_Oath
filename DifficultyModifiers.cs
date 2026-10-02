namespace AshenOath;

/// <summary>
/// Multiplier terpusat Tahap 20 agar tidak ada "if (Hard)" tersebar.
/// Normal selalu 1.0 (nilai existing). Player base stats, XP, loot,
/// harga shop, dan quest reward TIDAK disentuh.
/// </summary>
public static class DifficultyModifiers
{
    public const float HardEnemyHpMult = 1.30f;
    public const float HardEnemyDamageMult = 1.20f;
    public const float HardEnemySpeedMult = 1.10f;
    public const float HardBossHpMult = 1.25f;
    public const float HardBossDamageMult = 1.15f;
    public const float HardHazardDamageMult = 1.15f;

    public static float EnemyHpMult(Difficulty d) => d == Difficulty.Hard ? HardEnemyHpMult : 1f;
    public static float EnemyDamageMult(Difficulty d) => d == Difficulty.Hard ? HardEnemyDamageMult : 1f;
    public static float EnemySpeedMult(Difficulty d) => d == Difficulty.Hard ? HardEnemySpeedMult : 1f;
    public static float BossHpMult(Difficulty d) => d == Difficulty.Hard ? HardBossHpMult : 1f;
    public static float BossDamageMult(Difficulty d) => d == Difficulty.Hard ? HardBossDamageMult : 1f;
    public static float HazardDamageMult(Difficulty d) => d == Difficulty.Hard ? HardHazardDamageMult : 1f;

    public static int ScaleEnemyHp(int baseHp, Difficulty d)
        => Math.Max(1, (int)Math.Round(baseHp * EnemyHpMult(d)));

    public static int ScaleEnemyDamage(int baseDamage, Difficulty d)
        => Math.Max(1, (int)Math.Round(baseDamage * EnemyDamageMult(d)));

    public static float ScaleEnemySpeed(float baseSpeed, Difficulty d)
        => baseSpeed * EnemySpeedMult(d);

    public static int ScaleBossHp(int baseHp, Difficulty d)
        => Math.Max(1, (int)Math.Round(baseHp * BossHpMult(d)));

    public static int ScaleBossDamage(int baseDamage, Difficulty d)
        => Math.Max(1, (int)Math.Round(baseDamage * BossDamageMult(d)));

    public static int ScaleHazardDamage(int baseDamage, Difficulty d)
        => Math.Max(1, (int)Math.Round(baseDamage * HazardDamageMult(d)));

    public static bool IsDefined(Difficulty d) => d is Difficulty.Normal or Difficulty.Hard;
}
