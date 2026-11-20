namespace ShiftMagicSpell.Core
{
    public static class AttackTypeExtensions
    {
        /// <summary>attackPatterns.json의 공격 키("click" 등)</summary>
        public static string ToId(this AttackType type)
        {
            return type.ToString().ToLowerInvariant();
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
