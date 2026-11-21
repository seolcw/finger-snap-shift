using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using ShiftMagicSpell.Input;
using ShiftMagicSpell.View;
using UnityEngine;

namespace ShiftMagicSpell
{
    /// <summary>씬 진입점: 입력 → 로직(GameSession.Update) → 그리기 순서로 매 프레임 돌린다.</summary>
    public class GameRunner : MonoBehaviour
    {
        // 웹과 같이 한 프레임에 0.05초 넘게 진행하지 않는다(탭 전환 뒤 순간이동 방지)
        private const float MAX_DELTA = 0.05f;

        [SerializeField] private AttackPatternsData _attackPatterns;
        [SerializeField] private BoardLayoutData _boardLayouts;
        [SerializeField] private GameRenderer _renderer;
        [Tooltip("0이면 실행할 때마다 다른 난수")]
        [SerializeField] private int _seed;

        private readonly GameInput _input = new GameInput();

        public GameSession Session { get; private set; }

        private void Awake()
        {
#if !UNITY_WEBGL
            // WebGL은 브라우저 requestAnimationFrame에 맡긴다(값을 정하면 타이머로 맞춰서 고주사율 화면에서 끊길 수 있음)
            Application.targetFrameRate = 60;
#endif
            int seed = _seed != 0 ? _seed : System.Environment.TickCount;
            Session = new GameSession(_attackPatterns, _boardLayouts, seed);
            Debug.Log($"[INFO] GameRunner::Awake() - Game started. Details: [Version={Application.version}, Platform={Application.platform}]");
        }

        private void Update()
        {
            _input.Poll(Session);

            // 일시정지 동안 소리(배경음 포함)도 멈춘다
            AudioListener.pause = Session.IsPaused;
            Session.Update(Mathf.Min(MAX_DELTA, Time.deltaTime));
            _renderer.Render(Session);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && Session != null)
            {
                _input.OnFocusLost(Session);
            }
        }
    }
}
