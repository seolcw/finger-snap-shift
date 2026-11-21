using UnityEngine;

namespace ShiftMagicSpell.Data
{
    /// <summary>
    /// 키보드 두 판의 마스크·시작 배치·공격 슬롯 위치. 웹 프로토타입 layouts.json과 layoutRules.js 값을 옮긴다.
    /// 마스크: '#' 칸, 'S' SHIFT 칸, '.' 칸 없음. 배치: 글자, '_' 빈 키, '^' SHIFT, '.' 칸 없음.
    /// </summary>
    [CreateAssetMenu(fileName = "BoardLayouts", menuName = "Shift Magic Spell/Board Layouts")]
    public class BoardLayoutData : ScriptableObject
    {
        public string[] LeftMask = new string[0];
        public string[] RightMask = new string[0];
        public string[] LeftLayout = new string[0];
        public string[] RightLayout = new string[0];

        // x = 열(c), y = 행(r). 슬롯은 판 바깥 열(-1 또는 7)에 있다.
        public Vector2Int[] LeftSlots = new Vector2Int[0];
        public Vector2Int[] RightSlots = new Vector2Int[0];

        public string[] GetMask(int board)
        {
            return board == 0 ? LeftMask : RightMask;
        }

        public string[] GetLayout(int board)
        {
            return board == 0 ? LeftLayout : RightLayout;
        }

        public Vector2Int[] GetSlots(int board)
        {
            return board == 0 ? LeftSlots : RightSlots;
        }
    }
}
