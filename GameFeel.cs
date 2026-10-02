namespace AshenOath;

/// <summary>
/// Game-feel transient Tahap 20: hit stop + flash overlay.
/// Bukan state save; di-reset saat Continue/mati/pindah state.
/// Update hanya berjalan saat Playing (freeze di menu/pause).
/// </summary>
public sealed class GameFeel
{
    public const float HitStopNormal = 0.04f;
    public const float HitStopBoss = 0.06f;
    public const float HurtFlashDuration = 0.15f;
    public const float BossHitFlashDuration = 0.12f;

    public float HitStopTimeLeft { get; private set; }
    public float HurtFlashTimeLeft { get; private set; }
    public float BossHitFlashTimeLeft { get; private set; }

    public bool IsHitStopped => HitStopTimeLeft > 0f;

    /// <summary>
    /// Picu hit stop dari hit enemy (event-based). Refresh tanpa stacking:
    /// durasi terpanjang menang, tidak ada trigger ganda.
    /// </summary>
    public void TriggerHitStop(bool isBoss)
    {
        float duration = isBoss ? HitStopBoss : HitStopNormal;
        HitStopTimeLeft = Math.Max(HitStopTimeLeft, duration);
    }

    public void TriggerHurtFlash()
        => HurtFlashTimeLeft = Math.Max(HurtFlashTimeLeft, HurtFlashDuration);

    public void TriggerBossHitFlash()
        => BossHitFlashTimeLeft = Math.Max(BossHitFlashTimeLeft, BossHitFlashDuration);

    public void Update(float deltaTime)
    {
        if (HitStopTimeLeft > 0f)
        {
            HitStopTimeLeft -= deltaTime;
            if (HitStopTimeLeft < 0f)
                HitStopTimeLeft = 0f;
        }
        if (HurtFlashTimeLeft > 0f)
        {
            HurtFlashTimeLeft -= deltaTime;
            if (HurtFlashTimeLeft < 0f)
                HurtFlashTimeLeft = 0f;
        }
        if (BossHitFlashTimeLeft > 0f)
        {
            BossHitFlashTimeLeft -= deltaTime;
            if (BossHitFlashTimeLeft < 0f)
                BossHitFlashTimeLeft = 0f;
        }
    }

    /// <summary>Bersihkan semua transient (mati/Continue/pindah state).</summary>
    public void Clear()
    {
        HitStopTimeLeft = 0f;
        HurtFlashTimeLeft = 0f;
        BossHitFlashTimeLeft = 0f;
    }
}
