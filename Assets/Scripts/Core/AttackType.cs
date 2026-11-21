namespace ShiftMagicSpell.Core
{
    /// <summary>손 공격 종류. attackPatterns.json의 attacks 키(click, aim…)와 짝을 이룬다.</summary>
    public enum AttackType
    {
        Click,
        Aim,
        Summon,
        Flick,
        Track,
        Charge,
        Drag,
        Full,
        Slot,
    }
}
