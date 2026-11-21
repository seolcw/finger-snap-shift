using System.Collections.Generic;

namespace ShiftMagicSpell.Core
{
    /// <summary>단어가 한 줄의 연속한 칸에 가장 많이 맞춰진 자리. Cells[i]에 단어의 i번째 글자가 와야 한다.</summary>
    public class AlignResult
    {
        public int Board;
        public LineAxis Axis;
        public int Index;
        public int Start;
        public int Count;
        public List<BoardCell> Cells = new List<BoardCell>();
    }
}
