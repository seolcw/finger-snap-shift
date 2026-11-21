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

        [Header("화면 아래 안내 줄")]
        [SerializeField] private PixelText _guide;
        [Tooltip("연습 구간 단계별 안내: 0 이동, 1 줄 돌리기, 2 단어 맞추기, 3 슬롯에서 Space")]
        [SerializeField] private string[] _tutorialTexts =
        {
            "MOVE WITH WASD OR ARROW KEYS",
            "HOLD SHIFT + ARROW: ROTATE YOUR ROW / COLUMN",
            "LINE UP THE WORD IN ONE ROW OR COLUMN",
            "{0} GO TO THE {1} SLOT AND PRESS SPACE {2}",
        };
        [Tooltip("블록이 슬롯에 있을 때 안내. {0} = 왼쪽 화살표, {1} = LEFT/RIGHT, {2} = 오른쪽 화살표")]
        [SerializeField] private string _blockReminderFormat = "{0} BLOCK READY: {1} SLOT + SPACE {2}";
        [SerializeField] private float _guideBlinkRate = 4f;

        private string _handFormat;

        /// <summary>블록이 들어 있는데 플레이어가 그 슬롯 위에 있지 않은 판(없으면 -1)</summary>
        public static int GetPendingBlockBoard(GameSession game)
        {
            BoardCell here = game.Player.Here;
            if (here != null && here.IsSlot && here.Block != null)
            {
                return -1;
            }

            foreach (BoardModel board in game.Boards)
            {
                if (board.BlockCount() > 0)
                {
                    return board.Index;
                }
            }

            return -1;
        }

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
            RenderGuide(game);
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

        // 연습 구간에는 단계별 안내, 그 뒤에는 블록이 있으면 반대편 슬롯으로 가라는 안내를 깜빡이며 보여 준다
        private void RenderGuide(GameSession game)
        {
            string text = null;
            int board = GetPendingBlockBoard(game);
            string side = board == 0 ? "LEFT" : "RIGHT";
            string leftArrow = board == 0 ? "←" : string.Empty;
            string rightArrow = board == 1 ? "→" : string.Empty;
            if (game.State == GameState.Play && game.IsTutorial)
            {
                int step = Mathf.Clamp(game.TutorialStep, 0, _tutorialTexts.Length - 1);
                bool isWaitingToFire = step == _tutorialTexts.Length - 1;
                text = isWaitingToFire && board < 0 ? null : string.Format(_tutorialTexts[step], leftArrow, side, rightArrow);
            }
            else if (game.State == GameState.Play && board >= 0)
            {
                text = string.Format(_blockReminderFormat, leftArrow, side, rightArrow);
            }

            bool isVisible = !string.IsNullOrEmpty(text);
            if (_guide.gameObject.activeSelf != isVisible)
            {
                _guide.gameObject.SetActive(isVisible);
            }

            if (isVisible)
            {
                _guide.SetDisplay(text.Trim());
                _guide.Alpha = 0.6f + 0.4f * Mathf.Sin(game.Time * _guideBlinkRate * Mathf.PI);
            }
        }
    }
}
