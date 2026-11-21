namespace ShiftMagicSpell.Core
{
    public static class AttackTypeExtensions
    {
        /// <summary>attackPatterns.json의 공격 키("click" 등)</summary>
        public static string ToId(this AttackType type)
        {
            return type.ToString().ToLowerInvariant();
        }

        /// <summary>화면에 보이는 영어 이름(사망 원인 등)</summary>
        public static string ToDisplayName(this AttackType type)
        {
            switch (type)
            {
                case AttackType.Click:
                    return "CLICK";
                case AttackType.Aim:
                    return "AIMED DASH";
                case AttackType.Summon:
                    return "SUMMONED SHOT";
                case AttackType.Flick:
                    return "LINE SHOVE";
                case AttackType.Track:
                    return "SLAM";
                case AttackType.Charge:
                    return "EDGE CHARGE";
                case AttackType.Drag:
                    return "DRAG SELECT";
                case AttackType.Full:
                    return "FULL SLAM";
                case AttackType.Slot:
                    return "SLOT SMASH";
                default:
                    return type.ToString().ToUpperInvariant();
            }
        }

        public static bool TryParseId(string id, out AttackType type)
        {
            foreach (AttackType candidate in System.Enum.GetValues(typeof(AttackType)))
            {
                if (candidate.ToId() == id)
                {
                    type = candidate;
                    return true;
                }
            }

            type = AttackType.Click;
            return false;
        }
    }
}
