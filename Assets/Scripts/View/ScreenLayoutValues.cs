using System;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>판 배치(픽셀). 판 사이 간격은 로직 값(GeometryUtils.BOARD_GAP, 칸 단위)을 따른다.</summary>
    [Serializable]
    public class ScreenLayoutValues
    {
        [Tooltip("왼쪽 판 0열 0행 칸의 왼쪽 위(px)")]
        public Vector2 BoardOriginPx = new Vector2(32f, 72f);
        [Tooltip("칸 하나 크기(px). 키 그림은 16x20")]
        public Vector2 CellSizePx = new Vector2(16f, 20f);
        [Tooltip("키 왼쪽 위에서 글자(5x7) 왼쪽 위까지(px)")]
        public Vector2 GlyphOffsetPx = new Vector2(5f, 5f);
        [Tooltip("플레이어를 칸보다 띄우는 높이(px)")]
        public float PlayerLiftPx = 6f;
    }
}
