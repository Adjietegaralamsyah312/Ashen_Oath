namespace AshenOath;

/// <summary>
/// Animation state player. Prioritas:
/// Death > Hurt > ShieldBash > Attack > ProjectileCast > Dash > Jump > Fall > Walk > Idle.
/// </summary>
public enum PlayerAnimState
{
    Idle,
    Walk,
    Jump,
    Fall,
    Attack,
    Hurt,
    Death,
    Dash,
    ShieldBash,
    ProjectileCast
}
