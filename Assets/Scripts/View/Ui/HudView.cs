using ShiftMagicSpell.Core;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 플레이 중 화면 글자(Hud 오브젝트 아래): 플레이어 HP, 페이즈, 손 HP, 목표 단어 판, 슬롯 발동 안내, 토스트, 페이즈 배너.
    /// 위치·문구·색은 각 자식 오브젝트에서 고친다. 문구의 {0}, {1} 자리는 코드가 채운다.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [Tooltip("시작 화면에서는 통째로 숨기는 묶음")]
        [SerializeField] private GameObject _content;
        [SerializeField] private CountRowView _playerHp;
        [Tooltip("{0} = 지금 페이즈, {1} = 전체 페이즈 수")]
        [SerializeField] private PixelText _phase;
        [SerializeField] private CountRowView _handHp;
        [Tooltip("{0} = 손 HP, {1} = 최대")]
        [SerializeField] private PixelText _handHpLabel;
        [Tooltip("손이 멈췄을 때 문구. {0} = 손 HP, {1} = 최대")]
        [SerializeField] private string _handStunFormat = "STUN  HAND {0}/{1}";
        [SerializeField] private WordPanelView _wordPanel;
        [SerializeField] private SlotPromptView _slotPrompt;
        [SerializeField] private PixelText _toast;
        [SerializeField] private PixelText _banner;

        private string _handFormat;

        public void Render(GameSession game)
        {
            bool isVisible = game.State != GameState.Ready;
            if (_content.activeSelf != isVisible)
            {
                _content.SetActive(isVisible);
            }

            if (!isVisible)
            {
                return;
            }

            ViewColors colors = ViewAssets.Instance.Colors;
            _playerHp.Render(game.Player.Hp, game.Config.PlayerHp);
            _phase.SetArgs(game.Phase + 1, game.Data.Phases.Count);

            HandController hand = game.Hand;
            _handHp.Render(hand.Hp, hand.MaxHp);
            _handFormat ??= _handHpLabel.Text;
            bool isStunned = hand.State == HandState.Stun;
            _handHpLabel.SetDisplay(string.Format(isStunned ? _handStunFormat : _handFormat, hand.Hp, hand.MaxHp));
            _handHpLabel.Color = isStunned ? colors.Glow : colors.Dim;

            _wordPanel.Render(game);
            _slotPrompt.Render(game);

            _toast.gameObject.SetActive(game.Toast != null);
            if (game.Toast != null)
            {
                _toast.SetDisplay(game.Toast.Text);
                _toast.Alpha = Mathf.Min(1f, game.Toast.T * 2f);
            }

            _banner.gameObject.SetActive(game.Banner != null && game.State == GameState.Play);
            if (game.Banner != null)
            {
                _banner.SetDisplay(game.Banner.Text);
                _banner.Alpha = Mathf.Min(1f, game.Banner.T);
            }
        }
    }
}
