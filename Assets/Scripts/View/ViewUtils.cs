using ShiftMagicSpell.Core;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 로직 좌표(칸 단위) → 화면 픽셀. 칸 하나는 가로 16px, 세로 20px(키 윗면 16 + 두께 4).
    /// 배치는 ArtSource/Preview/game-preview-v5.png 기준이고, 손이 판 사이를 지나가도록 판 간격만 2칸으로 넓혔다.
    /// </summary>
    public static class ViewUtils
    {
        public const float CELL_W = 16f;
        public const float CELL_H = 20f;
        public const float BOARD_LEFT_PX = 32f;
        public const float BOARD_TOP_PX = 72f;

        public static Vector2 ToPixel(Vector2 logic)
        {
            return new Vector2(BOARD_LEFT_PX + logic.x * CELL_W, BOARD_TOP_PX + logic.y * CELL_H);
        }

        /// <summary>칸 오프셋(칸 단위, 미끄러짐 등)을 픽셀로</summary>
        public static Vector2 ToPixelDelta(Vector2 logicDelta)
        {
            return new Vector2(logicDelta.x * CELL_W, logicDelta.y * CELL_H);
        }

        public static Vector2 CellTopLeft(int board, int r, int c)
        {
            return ToPixel(new Vector2(GeometryUtils.BoardOffsetX(board) + c, r));
        }

        public static Vector2 CellCenter(int board, int r, int c)
        {
            return CellTopLeft(board, r, c) + new Vector2(CELL_W * 0.5f, CELL_W * 0.5f);
        }
    }
}
