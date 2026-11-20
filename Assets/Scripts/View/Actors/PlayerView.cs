using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 플레이어(Player 프리팹). 루트 위치 = 서 있는 칸 그림(16x20)의 왼쪽 아래. 몸은 Body 자식(칸보다 띄우는 높이는 Body의 위치).
    /// 피격 무적 동안 깜빡이고, Shift를 누르고 있으면 하늘색 테, 디버그 무적이면 노란 테.
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private SpriteRenderer _shiftOutline;
        [SerializeField] private SpriteRenderer _debugOutline;
        [SerializeField] private int _sortingOrder = RenderOrderUtils.PLAYER;
        [Tooltip("무적 동안 깜빡이는 빠르기(초당)")]
        [SerializeField] private float _blinkRate = 16f;
        [SerializeField, Range(0f, 1f)] private float _blinkAlpha = 0.35f;
        [Tooltip("맞은 뒤 Hurt 그림을 보여 주는 시간(초)")]
        [SerializeField] private float _hurtShowTime = 0.35f;

        public void Render(GameSession game)
        {
            bool isVisible = game.State != GameState.Ready;
            gameObject.SetActive(isVisible);
            if (!isVisible)
            {
                return;
            }

            ArtCatalog catalog = ViewAssets.Instance.Catalog;
            ViewColors colors = ViewAssets.Instance.Colors;
            PlayerController player = game.Player;
            Vector3 offset = SlideAnim.GetOffset(player.Slide);
            Vector2 topLeft = ViewUtils.CellTopLeft(player.Board, player.R, player.C) + ViewUtils.ToPixelDelta(offset);
            transform.localPosition = ViewUtils.TopLeftToWorld(topLeft, ViewUtils.CellH);

            bool isBlinking = player.Invuln > 0f && Mathf.FloorToInt(game.Time * _blinkRate) % 2 == 0;
            bool isHurt = player.Invuln > game.Config.HitInvuln - _hurtShowTime && player.Hp < game.Config.PlayerHp && game.Stats.Hits.Count > 0;
            _body.sprite = isHurt ? catalog.GetFrame("Player", "Hurt", 0).Sprite : catalog.GetAnimated("Player", "Idle", game.Time).Sprite;
            _body.color = new Color(1f, 1f, 1f, offset.z * (isBlinking ? _blinkAlpha : 1f));
            _body.sortingOrder = _sortingOrder;

            _shiftOutline.enabled = player.IsShiftHeld;
            _shiftOutline.color = colors.ShiftHi;
            _shiftOutline.sortingOrder = _sortingOrder + 1;
            _debugOutline.enabled = game.IsInvincible;
            _debugOutline.color = colors.Glow;
            _debugOutline.sortingOrder = _sortingOrder + 1;
        }
    }
}
