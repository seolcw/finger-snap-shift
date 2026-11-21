using ShiftMagicSpell.Core;

namespace ShiftMagicSpell.Tests
{
    public enum BotActionKind
    {
        None,
        Move,
        Rotate,
        Fire,
    }

    public struct BotAction
    {
        public BotActionKind Kind;
        public MoveDir Dir;
    }
}
