namespace ShiftMagicSpell.Core
{
    /// <summary>손 상태: 쉬기 → 이동(이동이 곧 예고) → 예고 → 공격 → 쉬기. 마법에 맞으면 멈춤.</summary>
    public enum HandState
    {
        Idle,
        Travel,
        Warn,
        Exec,
        Stun,
    }
}
