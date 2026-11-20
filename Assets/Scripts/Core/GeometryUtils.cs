using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 게임 로직 좌표: 1 = 칸 하나, x는 오른쪽, y는 아래(행). 웹 프로토타입의 픽셀 값은 WEB_CELL_PX(50)로 나눠서 쓴다.
    /// 화면(320x200, 칸 16x20px)으로 바꾸는 일은 View 쪽이 한다.
    /// </summary>
    public static class GeometryUtils
    {
        public const int ROWS = 5;
        public const int COLS = 7;
        public const float WEB_CELL_PX = 50f;

        // 손이 도는 둘레: 판 바깥 0.92칸 (웹 MARGIN 46px)
        public const float MARGIN = 46f / WEB_CELL_PX;

        // 두 판 사이 간격(칸). 웹은 2.72칸, 픽셀 화면에 맞춰 2칸으로 둔다(둘레가 겹치지 않으면 된다).
        public const float BOARD_GAP = 2f;

        public static float BoardOffsetX(int board)
        {
            return board == 0 ? 0f : COLS + BOARD_GAP;
        }

        public static Vector2 CellCenter(int board, int r, int c)
        {
            return new Vector2(BoardOffsetX(board) + c + 0.5f, r + 0.5f);
        }

        // 칸 번호. 슬롯이 열 -1, 7에 있어서 한 행에 16칸 폭을 잡는다(웹과 같은 방식).
        public static int CellKey(int r, int c)
        {
            return r * 16 + c + 4;
        }

        public static Vector2Int UnKey(int cellKey)
        {
            return new Vector2Int(cellKey % 16 - 4, Mathf.FloorToInt(cellKey / 16f));
        }

        public static Vector2Int ToDelta(MoveDir dir)
        {
            switch (dir)
            {
                case MoveDir.Up:
                    return new Vector2Int(0, -1);
                case MoveDir.Down:
                    return new Vector2Int(0, 1);
                case MoveDir.Left:
                    return new Vector2Int(-1, 0);
                case MoveDir.Right:
                    return new Vector2Int(1, 0);
                default:
                    return Vector2Int.zero;
            }
        }

        public static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            float t = lengthSq > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq) : 0f;
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
