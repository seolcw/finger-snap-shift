namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 판의 칸 하나. 칸 자체는 움직이지 않고, 줄 돌리기는 키(KeyTile)가 칸 사이를 옮겨 다닌다.
    /// SHIFT 칸과 공격 슬롯 칸은 키가 없는 고정 칸이다.
    /// </summary>
    public class BoardCell
    {
        public readonly int R;
        public readonly int C;
        public readonly bool IsShift;
        public readonly bool IsSlot;

        public KeyTile Key;
        public AttackBlock Block;

        public BoardCell(int r, int c, bool isShift, bool isSlot)
        {
            R = r;
            C = c;
            IsShift = isShift;
            IsSlot = isSlot;
        }

        public bool Is(int r, int c)
        {
            return R == r && C == c;
        }
    }
}
