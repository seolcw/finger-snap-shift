using ShiftMagicSpell.Core;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 손 공격 예고 칸 하나(WarningCell 프리팹): 빨간 테두리(KeyWarning, 깜빡임) + 깜빡이는 바탕 + 줄 섞기 미는 양 표시(→1 …).
    /// 손이 이동 중일 때는 옅게 미리 보여 준다. 루트 위치 = 칸 그림(16x20)의 왼쪽 아래.
    /// </summary>
    public class WarningCellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _frame;
        [SerializeField] private SpriteRenderer _fill;
        [SerializeField] private GameObject _badge;
        [SerializeField] private PixelText _badgeText;
        [Tooltip("손이 아직 이동 중일 때(예고 전) 투명도")]
        [SerializeField, Range(0f, 1f)] private float _previewAlpha = 0.45f;
        [SerializeField, Range(0f, 1f)] private float _fillAlpha = 0.28f;

        public void Render(bool isWarning, bool isBlinkOn, float time, string badge)
        {
            _frame.sprite = ViewAssets.Instance.Catalog.GetAnimated("KeyWarning", "Blink", time).Sprite;
            _frame.color = new Color(1f, 1f, 1f, isWarning ? 1f : _previewAlpha);
            _fill.enabled = isWarning && isBlinkOn;
            _fill.color = ViewAssets.Instance.Colors.Warn.WithAlpha(_fillAlpha);
            bool hasBadge = !string.IsNullOrEmpty(badge);
            if (_badge.activeSelf != hasBadge)
            {
                _badge.SetActive(hasBadge);
            }

            if (hasBadge)
            {
                _badgeText.SetDisplay(badge);
            }
        }
    }
}
