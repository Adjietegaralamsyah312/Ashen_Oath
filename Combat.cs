namespace AshenOath;

/// <summary>
/// Resolusi combat per frame. Dipanggil Game setelah updatesets selesai;
/// tidak menyimpan state, tidak tahu tentang renderer.
/// </summary>
public static class Combat
{
    /// <summary>
    /// Hitbox attack aktif player vs semua enemy hidup. Satu attack id hanya
    /// bisa melukai tiap enemy sekali (via LastHitAttackId).
    /// Damage = EffectiveAttack (level-base + equipment).
    /// </summary>
    public static void ResolvePlayerAttack(Player player, IEnumerable<Enemy> enemies)
    {
        if (!player.Alive || !player.IsAttackActive)
            return;

        Aabb hitbox = player.AttackHitbox.ToAabb();
        float playerCenter = player.X + Player.Width / 2f;
        int damage = Player.AttackDamage + player.AttackBonus;

        foreach (var enemy in enemies)
        {
            if (!enemy.Alive || enemy.LastHitAttackId == player.AttackId)
                continue;
            if (!enemy.Bounds.Overlaps(hitbox))
                continue;

            float enemyCenter = enemy.X + enemy.Width / 2f;
            float direction = enemyCenter >= playerCenter ? 1f : -1f;
            enemy.TakeDamage(damage, direction);
            enemy.LastHitAttackId = player.AttackId;
        }
    }

    /// <summary>
    /// Kontak enemy-player: player menerima damage (invulnerability window
    /// milik Player mencegah damage tiap frame). Slime stunned tidak menyerang.
    /// </summary>
    public static void ResolveContactDamage(IEnumerable<Enemy> enemies, Player player)
    {
        if (!player.Alive)
            return;
        foreach (var enemy in enemies)
        {
            if (!enemy.Alive || enemy.IsStunned)
                continue;
            if (enemy.Bounds.Overlaps(player.Bounds))
                player.TakeDamage(enemy.Damage);
        }
    }

    /// <summary>
    /// Hitbox Shield Bash aktif vs semua enemy hidup. Satu activation id hanya
    /// melukai tiap enemy sekali; korban selamat kena knockback + stun.
    /// Damage = 20 + (EffectiveAttack - 10): progression + equipment.
    /// </summary>
    public static void ResolveShieldBash(Player player, IEnumerable<Enemy> enemies)
    {
        if (!player.Alive || !player.IsBashActive)
            return;

        Aabb hitbox = player.BashHitbox.ToAabb();
        float playerCenter = player.X + Player.Width / 2f;
        int damage = ShieldBashSkill.BashDamage + player.AttackBonus;

        foreach (var enemy in enemies)
        {
            if (!enemy.Alive || enemy.LastHitAttackId == player.BashId)
                continue;
            if (!enemy.Bounds.Overlaps(hitbox))
                continue;

            float enemyCenter = enemy.X + enemy.Width / 2f;
            float direction = enemyCenter >= playerCenter ? 1f : -1f;
            enemy.TakeDamage(damage, direction, true, ShieldBashSkill.BashKnockback);
            enemy.ApplyStun(ShieldBashSkill.BashStun);
            enemy.LastHitAttackId = player.BashId;
        }
    }

    /// <summary>
    /// Projectile vs semua enemy hidup: damage satu kali tanpa knockback,
    /// projectile mati saat mengenai (tidak menembus).
    /// Damage = 12 + (EffectiveAttack - 10): progression + equipment.
    /// </summary>
    public static void ResolveProjectiles(Player player, IEnumerable<Projectile> projectiles, IEnumerable<Enemy> enemies)
    {
        int damage = Projectile.Damage + player.AttackBonus;
        foreach (var projectile in projectiles)
        {
            if (!projectile.Alive)
                continue;
            Aabb bounds = projectile.Bounds;
            foreach (var enemy in enemies)
            {
                if (!enemy.Alive)
                    continue;
                if (!enemy.Bounds.Overlaps(bounds))
                    continue;

                float direction = projectile.Direction;
                enemy.TakeDamage(damage, direction, false);
                projectile.Kill();
                break;
            }
        }
    }

    /// <summary>Overload kompatibilitas tanpa bonus (bonus 0).</summary>
    public static void ResolveProjectiles(IEnumerable<Projectile> projectiles, IEnumerable<Enemy> enemies)
    {
        foreach (var projectile in projectiles)
        {
            if (!projectile.Alive)
                continue;
            Aabb bounds = projectile.Bounds;
            foreach (var enemy in enemies)
            {
                if (!enemy.Alive)
                    continue;
                if (!enemy.Bounds.Overlaps(bounds))
                    continue;

                float direction = projectile.Direction;
                enemy.TakeDamage(Projectile.Damage, direction, false);
                projectile.Kill();
                break;
            }
        }
    }
}
