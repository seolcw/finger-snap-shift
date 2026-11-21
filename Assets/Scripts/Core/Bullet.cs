namespace ShiftMagicSpell.Core
{
    /// <summary>손의 소환 탄: 줄을 따라 칸 단위로 날아간다. Pos = 줄 안의 위치(칸).</summary>
    public class Bullet
    {
        public int Board;
        public LineAxis Axis;
        public int Index;
        public float Pos;
        public int Dir;
        public float Speed;
        public bool IsDead;
    }
}
