using System.Collections.Generic;

namespace ShiftMagicSpell.Core
{
    /// <summary>줄(행 또는 열) 하나: SHIFT 칸을 뺀 칸들. 줄 돌리기와 단어 읽기(행 왼→오, 열 위→아래)가 같은 순서를 쓴다.</summary>
    public class BoardLine
    {
        public LineAxis Axis;
        public int Index;
        public List<BoardCell> Cells = new List<BoardCell>();
    }
}
