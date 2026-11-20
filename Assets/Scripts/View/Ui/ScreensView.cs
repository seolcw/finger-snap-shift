using ShiftMagicSpell.Core;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 화면 전체를 덮는 패널(Screens 오브젝트 아래): 시작, 일시정지, 쓰러짐, 페이즈 클리어, 승리.
    /// 패널마다 자식 오브젝트(덮개, 글자)를 에디터에서 고친다. 상태에 따라 하나만 켠다. 문구의 {0} 자리는 코드가 채운다.
    /// </summary>
    public class ScreensView : MonoBehaviour
    {
        [SerializeField] private GameObject _title;
        [Tooltip("첫 화면 오른쪽 아래 버전 글자")]
        [SerializeField] private PixelText _version;
        [SerializeField] private GameObject _paused;
        [SerializeField] private GameObject _dead;
        [Tooltip("{0} = 페이즈, {1} = 재시작할 손 HP, {2} = 최대")]
        [SerializeField] private PixelText _deadSubtitle;
        [SerializeField] private GameObject _phaseClear;
        [Tooltip("{0} = 클리어한 페이즈")]
        [SerializeField] private PixelText _phaseClearTitle;
        [Tooltip("마법이 터지는 걸 먼저 보여 주고 클리어 화면을 띄우는 시점(남은 시간, 초)")]
        [SerializeField] private float _phaseClearShowAt = 1.2f;
        [SerializeField] private GameObject _win;
        [Tooltip("{0} = 분, {1} = 초, {2} = 사망, {3} = 피격")]
        [SerializeField] private PixelText _winLine1;
        [Tooltip("{0} = 완성 단어, {1} = 줄 돌리기, {2} = 콤보")]
        [SerializeField] private PixelText _winLine2;
        [Tooltip("승리 뒤 결과 화면을 띄우기까지(초)")]
        [SerializeField] private float _winShowAfter = 0.9f;

        private void Start()
        {
            if (_version != null)
            {
                _version.SetDisplay(Application.version);
            }
        }

        public void Render(GameSession game)
        {
            bool isPhaseClear = game.State == GameState.PhaseClear && game.StateT < _phaseClearShowAt;
            bool isWin = game.State == GameState.Win && game.StateT > _winShowAfter;
            SetActive(_title, game.State == GameState.Ready);
            SetActive(_paused, game.IsPaused && game.State != GameState.Ready);
            SetActive(_dead, game.State == GameState.Dead);
            SetActive(_phaseClear, isPhaseClear);
            SetActive(_win, isWin);

            if (game.State == GameState.Dead)
            {
                _deadSubtitle.SetArgs(game.Phase + 1, game.GetRecoveredHandHp(), game.Hand.MaxHp);
            }

            if (isPhaseClear)
            {
                _phaseClearTitle.SetArgs(game.Phase + 1);
            }

            if (isWin)
            {
                GameStats stats = game.Stats;
                int hits = 0;
                foreach (int count in stats.Hits.Values)
                {
                    hits += count;
                }

                int seconds = Mathf.FloorToInt(stats.PlayTime);
                _winLine1.SetArgs(seconds / 60, seconds % 60, stats.Deaths, hits);
                _winLine2.SetArgs(stats.WordsDone, stats.Rotations, stats.Combos);
            }
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }
    }
}
