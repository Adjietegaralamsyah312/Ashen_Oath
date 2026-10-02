using SDL2;

namespace AshenOath;

/// <summary>
/// HUD rectangle-only (tanpa font/dependency baru):
/// HP bar + Energy bar player kiri atas, HP bar di atas tiap slime hidup.
/// </summary>
public static class Hud
{
    private const int BarX = 12;
    private const int BarY = 12;
    private const int BarW = 200;
    private const int BarH = 20;
    private const int EnergyBarY = BarY + BarH + 4;
    private const int EnergyBarH = 14;

    public static void RenderPlayerHp(IntPtr renderer, int hp, int maxHp)
    {
        float ratio = maxHp <= 0 ? 0f : Math.Clamp((float)hp / maxHp, 0f, 1f);

        SDL.SDL_Rect back = new() { x = BarX, y = BarY, w = BarW, h = BarH };
        SDL.SDL_SetRenderDrawColor(renderer, 18, 18, 26, 255);
        SDL.SDL_RenderFillRect(renderer, ref back);

        int fillW = (int)(BarW * ratio);
        if (fillW > 0)
        {
            SDL.SDL_Rect fill = new() { x = BarX, y = BarY, w = fillW, h = BarH };
            if (ratio > 0.5f)
                SDL.SDL_SetRenderDrawColor(renderer, 80, 200, 110, 255);
            else if (ratio > 0.25f)
                SDL.SDL_SetRenderDrawColor(renderer, 220, 180, 60, 255);
            else
                SDL.SDL_SetRenderDrawColor(renderer, 220, 70, 60, 255);
            SDL.SDL_RenderFillRect(renderer, ref fill);
        }

        SDL.SDL_Rect frame = back;
        SDL.SDL_SetRenderDrawColor(renderer, 230, 230, 235, 255);
        SDL.SDL_RenderDrawRect(renderer, ref frame);
    }

    public static void RenderPlayerEnergy(IntPtr renderer, float energy, float maxEnergy)
    {
        float ratio = maxEnergy <= 0f ? 0f : Math.Clamp(energy / maxEnergy, 0f, 1f);

        SDL.SDL_Rect back = new() { x = BarX, y = EnergyBarY, w = BarW, h = EnergyBarH };
        SDL.SDL_SetRenderDrawColor(renderer, 14, 20, 28, 255);
        SDL.SDL_RenderFillRect(renderer, ref back);

        int fillW = (int)(BarW * ratio);
        if (fillW > 0)
        {
            SDL.SDL_Rect fill = new() { x = BarX, y = EnergyBarY, w = fillW, h = EnergyBarH };
            SDL.SDL_SetRenderDrawColor(renderer, 70, 180, 220, 255);
            SDL.SDL_RenderFillRect(renderer, ref fill);
        }

        SDL.SDL_Rect frame = back;
        SDL.SDL_SetRenderDrawColor(renderer, 200, 215, 225, 255);
        SDL.SDL_RenderDrawRect(renderer, ref frame);
    }

    private const int SkillBarY = EnergyBarY + EnergyBarH + 4;
    private const int SkillBarH = 10;
    private const int SkillBarW = 62;
    private const int SkillBarGap = 4;
    private const int XpBarY = SkillBarY + SkillBarH + 4;
    private const int XpBarH = 8;

    /// <summary>Indikator cooldown 3 skill: penuh = ready.</summary>
    public static void RenderSkills(IntPtr renderer, DashSkill dash, ShieldBashSkill bash, ProjectileSkill fire)
    {
        RenderSkillBar(renderer, BarX, SkillBarY, ReadyRatio(dash), 220, 180, 70);
        RenderSkillBar(renderer, BarX + SkillBarW + SkillBarGap, SkillBarY, ReadyRatio(bash), 220, 120, 60);
        RenderSkillBar(renderer, BarX + (SkillBarW + SkillBarGap) * 2, SkillBarY, ReadyRatio(fire), 150, 120, 220);
    }

    private static float ReadyRatio(Skill skill)
    {
        if (skill.Cooldown <= 0f)
            return 1f;
        return Math.Clamp(1f - skill.CooldownRemaining / skill.Cooldown, 0f, 1f);
    }

    private static void RenderSkillBar(IntPtr renderer, int x, int y, float ratio, byte r, byte g, byte b)
    {
        SDL.SDL_Rect back = new() { x = x, y = y, w = SkillBarW, h = SkillBarH };
        SDL.SDL_SetRenderDrawColor(renderer, 14, 14, 22, 255);
        SDL.SDL_RenderFillRect(renderer, ref back);

        int fillW = (int)(SkillBarW * ratio);
        if (fillW > 0)
        {
            SDL.SDL_Rect fill = new() { x = x, y = y, w = fillW, h = SkillBarH };
            SDL.SDL_SetRenderDrawColor(renderer, r, g, b, 255);
            SDL.SDL_RenderFillRect(renderer, ref fill);
        }

        SDL.SDL_Rect frame = back;
        SDL.SDL_SetRenderDrawColor(renderer, 160, 160, 170, 255);
        SDL.SDL_RenderDrawRect(renderer, ref frame);
    }

    /// <summary>
    /// XP bar + indikator level Tahap 19 (di bawah skill bar).
    /// Bar penuh saat max level.
    /// </summary>
    public static void RenderProgression(IntPtr renderer, int level, int currentXp, int xpToNext)
    {
        float ratio = xpToNext <= 0 ? 0f : Math.Clamp((float)currentXp / xpToNext, 0f, 1f);
        if (level >= PlayerProgression.MaxLevel)
            ratio = 1f;

        SDL.SDL_Rect back = new() { x = BarX, y = XpBarY, w = BarW, h = XpBarH };
        SDL.SDL_SetRenderDrawColor(renderer, 14, 14, 22, 255);
        SDL.SDL_RenderFillRect(renderer, ref back);

        int fillW = (int)(BarW * ratio);
        if (fillW > 0)
        {
            SDL.SDL_Rect fill = new() { x = BarX, y = XpBarY, w = fillW, h = XpBarH };
            SDL.SDL_SetRenderDrawColor(renderer, 150, 120, 230, 255);
            SDL.SDL_RenderFillRect(renderer, ref fill);
        }

        SDL.SDL_Rect frame = back;
        SDL.SDL_SetRenderDrawColor(renderer, 160, 160, 170, 255);
        SDL.SDL_RenderDrawRect(renderer, ref frame);

        BitmapFont.DrawText(renderer, $"LV {level}", BarX, XpBarY + XpBarH + 4, 2, 235, 235, 240);
    }

    public static void RenderEnemyHp(IntPtr renderer, Enemy enemy, Camera camera)
    {
        if (!enemy.Alive)
            return;
        // Boss punya bar khusus; jangan gambar bar kecil di atas boss.
        if (enemy is Boss)
            return;

        float ratio = enemy.MaxHp <= 0 ? 0f : Math.Clamp((float)enemy.Hp / enemy.MaxHp, 0f, 1f);
        int x = (int)(enemy.X - camera.RenderX);
        int y = (int)(enemy.Y - camera.RenderY) - 9;
        int w = enemy.Width;

        SDL.SDL_Rect back = new() { x = x, y = y, w = w, h = 5 };
        SDL.SDL_SetRenderDrawColor(renderer, 60, 20, 20, 255);
        SDL.SDL_RenderFillRect(renderer, ref back);

        int fillW = (int)(w * ratio);
        if (fillW > 0)
        {
            SDL.SDL_Rect fill = new() { x = x, y = y, w = fillW, h = 5 };
            SDL.SDL_SetRenderDrawColor(renderer, 90, 210, 110, 255);
            SDL.SDL_RenderFillRect(renderer, ref fill);
        }
    }

    private const int BossBarW = 600;
    private const int BossBarH = 24;

    /// <summary>
    /// Boss bar rectangle-only center/top: HP ratio + phase indicator visual.
    /// Hanya dipanggil saat encounter aktif dan boss hidup.
    /// </summary>
    public static void RenderBossBar(IntPtr renderer, int hp, int maxHp, int phase, int windowWidth)
    {
        float ratio = maxHp <= 0 ? 0f : Math.Clamp((float)hp / maxHp, 0f, 1f);
        int x = (windowWidth - BossBarW) / 2;
        const int y = 12;

        SDL.SDL_Rect back = new() { x = x, y = y, w = BossBarW, h = BossBarH };
        SDL.SDL_SetRenderDrawColor(renderer, 12, 12, 18, 255);
        SDL.SDL_RenderFillRect(renderer, ref back);

        int fillW = (int)(BossBarW * ratio);
        if (fillW > 0)
        {
            SDL.SDL_Rect fill = new() { x = x, y = y, w = fillW, h = BossBarH };
            if (phase >= 2)
                SDL.SDL_SetRenderDrawColor(renderer, 200, 60, 60, 255);
            else
                SDL.SDL_SetRenderDrawColor(renderer, 150, 120, 200, 255);
            SDL.SDL_RenderFillRect(renderer, ref fill);
        }

        SDL.SDL_Rect frame = back;
        SDL.SDL_SetRenderDrawColor(renderer, 230, 230, 235, 255);
        SDL.SDL_RenderDrawRect(renderer, ref frame);

        // Phase indicator: kotak kecil di kanan bar (emas P1, merah P2).
        SDL.SDL_Rect pip = new() { x = x + BossBarW + 8, y = y + 4, w = 16, h = 16 };
        if (phase >= 2)
            SDL.SDL_SetRenderDrawColor(renderer, 230, 70, 70, 255);
        else
            SDL.SDL_SetRenderDrawColor(renderer, 240, 200, 80, 255);
        SDL.SDL_RenderFillRect(renderer, ref pip);
    }
}
