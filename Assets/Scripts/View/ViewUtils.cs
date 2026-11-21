using ShiftMagicSpell.Core;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 로직 좌표(칸 단위) → 화면 픽셀 → 월드 좌표. 판 위치·칸 크기는 ViewAssets(인스펙터)에서 읽는다.
    /// 화면은 320x200 픽셀, 왼쪽 위 원점, 1유닛 = 16px.
    /// </summary>
    public static class ViewUtils
    {
        public const float PPU = 16f;
        public const float SCREEN_W = 320f;
        public const float SCREEN_H = 200f;

        private static ScreenLayoutValues Layout => ViewAssets.Instance.Layout;

        public static float CellW => Layout.CellSizePx.x;
        public static float CellH => Layout.CellSizePx.y;

        public static Vector2 ToPixel(Vector2 logic)
        {
            return Layout.BoardOriginPx + Vector2.Scale(logic, Layout.CellSizePx);
        }

        /// <summary>칸 오프셋(칸 단위, 미끄러짐 등)을 픽셀로</summary>
        public static Vector2 ToPixelDelta(Vector2 logicDelta)
        {
            return Vector2.Scale(logicDelta, Layout.CellSizePx);
        }

        public static Vector2 CellTopLeft(int board, int r, int c)
        {
            return ToPixel(new Vector2(GeometryUtils.BoardOffsetX(board) + c, r));
        }

        /// <summary>키 윗면(정사각형) 가운데</summary>
        public static Vector2 CellCenter(int board, int r, int c)
        {
            return CellTopLeft(board, r, c) + new Vector2(CellW * 0.5f, CellW * 0.5f);
        }

        /// <summary>화면 픽셀(왼쪽 위 원점, y 아래) → 월드 좌표</summary>
        public static Vector3 PixelToWorld(Vector2 pixel)
        {
            return new Vector3(pixel.x / PPU, (SCREEN_H - pixel.y) / PPU, 0f);
        }

        /// <summary>스프라이트(캔버스 왼쪽 아래 피벗)의 왼쪽 위를 pixel에 맞출 때 월드 위치</summary>
        public static Vector3 TopLeftToWorld(Vector2 pixel, float canvasHeightPx)
        {
            return PixelToWorld(new Vector2(pixel.x, pixel.y + canvasHeightPx));
        }
    }
}
