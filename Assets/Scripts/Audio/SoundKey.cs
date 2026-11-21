namespace ShiftMagicSpell.Audio
{
    /// <summary>소리를 내는 게임 이벤트. SoundLibrary에서 이벤트마다 클립·볼륨·피치를 정한다.</summary>
    public enum SoundKey
    {
        Start,
        Move,
        Rotate,
        Deny,
        LetterMatch,
        WordComplete,
        Store,
        SpellFire,
        SpellBolt,
        SpellBeam,
        SpellMagic,
        SpellShift,
        SpellImpact,
        Combo,
        PlayerHurt,
        HandWarn,
        HandClick,
        HandSlam,
        BlockBreak,
        Warp,
        PhaseClear,
        Win,
        Death,
    }
}
