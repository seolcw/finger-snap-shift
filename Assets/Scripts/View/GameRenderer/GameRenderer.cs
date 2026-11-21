using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 매 프레임 GameSession 상태를 화면에 반영한다.
    /// - 씬 오브젝트(에디터에서 고침): 판·키·슬롯(BoardView), 플레이어(PlayerView), 손(HandView)
    /// - 연출 프리팹 풀(개수가 매번 바뀜, 겉모양은 프리팹에서): 예고 칸(WarningCell), 소환 탄(Bullet), 떠오르는 글자(FloatText)
    /// - 즉시 그리기 캔버스(모양 자체가 매번 계산되는 것만): 잔상·점선(under), 줄 돌리기·맞춤·순간이동 연출(over),
    ///   조준선·드래그 사각형·조준점(warning), 마법(fx). 캔버스마다 앞뒤 순서 시작 값이 다르다.
    /// - 화면 글자(HudView)와 전체 화면 패널(ScreensView)도 씬 오브젝트
    /// 판 위 연출(BoardFx), 손 예고(Warnings), 마법(Spells)은 partial 파일로 나눴다.
    /// </summary>
    public partial class GameRenderer : MonoBehaviour
    {
        [Header("씬 오브젝트")]
        [Tooltip("흔들림을 줄 때 움직이는 루트(판·플레이어·손·연출). 화면 글자는 여기 밖에 둔다")]
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private BoardView[] _boards = new BoardView[2];
        [SerializeField] private PlayerView _player;
        [SerializeField] private HandView _hand;
        [SerializeField] private HudView _hud;
        [SerializeField] private ScreensView _screens;

        [Header("즉시 그리기 캔버스")]
        [SerializeField] private PixelCanvas _underCanvas;
        [SerializeField] private PixelCanvas _overCanvas;
        [SerializeField] private PixelCanvas _warningCanvas;
        [SerializeField] private PixelCanvas _fxCanvas;

        [Header("연출 프리팹 (매 프레임 필요한 개수만 꺼내 씀)")]
        [Tooltip("풀 인스턴스를 둘 곳(World 아래라 같이 흔들림)")]
        [SerializeField] private Transform _poolRoot;
        [SerializeField] private WarningCellView _warningCellPrefab;
        [SerializeField] private SpriteRenderer _bulletPrefab;
        [SerializeField] private PixelText _floatTextPrefab;

        [Header("마법 연출")]
        [Tooltip("마법 시간 중 날아가는 비율(나머지는 손에서 터짐)")]
        [SerializeField, Range(0.1f, 0.9f)] private float _spellTravelRatio = 0.55f;
        [Tooltip("웹 프로토타입 px 값(궤적 높이·퍼짐)을 화면 px로 바꾸는 배율(칸 50px → 16px)")]
        [SerializeField] private float _spellDistanceScale = 16f / 50f;

        [Header("화면 흔들림")]
        [Tooltip("흔들림 세기 1일 때 최대 이동(px)")]
        [SerializeField] private float _shakePx = 4f;

        private PixelCanvas _canvas;
        private PixelTextDrawer _text;
        private GameSession _game;
        private float _shakeSeed;
        private PrefabPool<WarningCellView> _warningPool;
        private PrefabPool<SpriteRenderer> _bulletPool;
        private PrefabPool<PixelText> _floatTextPool;

        private static ViewColors Colors => ViewAssets.Instance.Colors;
        private static ArtCatalog Catalog => ViewAssets.Instance.Catalog;

        private void Awake()
        {
            _text = new PixelTextDrawer(_fxCanvas, Catalog);
            _warningPool = new PrefabPool<WarningCellView>(_warningCellPrefab, _poolRoot);
            _bulletPool = new PrefabPool<SpriteRenderer>(_bulletPrefab, _poolRoot);
            _floatTextPool = new PrefabPool<PixelText>(_floatTextPrefab, _poolRoot);
        }

        public void Render(GameSession game)
        {
            _game = game;
            PixelCanvas[] canvases = { _underCanvas, _overCanvas, _warningCanvas, _fxCanvas };
            foreach (PixelCanvas canvas in canvases)
            {
                canvas.BeginFrame();
            }

            _warningPool.BeginFrame();
            _bulletPool.BeginFrame();
            _floatTextPool.BeginFrame();

            ApplyShake();
            foreach (BoardView board in _boards)
            {
                board.Render(game, _underCanvas);
            }

            UseCanvas(_warningCanvas);
            DrawWarnings();
            DrawBullets();
            _player.Render(game);
            _hand.Render(game);
            DrawCellFx();
            UseCanvas(_overCanvas);
            DrawSlotArrows();
            _hud.Render(game);
            _screens.Render(game);
            foreach (PixelCanvas canvas in canvases)
            {
                canvas.EndFrame();
            }

            _warningPool.EndFrame();
            _bulletPool.EndFrame();
            _floatTextPool.EndFrame();
        }

        private void UseCanvas(PixelCanvas canvas)
        {
            _canvas = canvas;
            _text.Canvas = canvas;
        }

        // 화면 흔들림: 판과 연출만 흔들고 화면 글자는 고정
        private void ApplyShake()
        {
            Vector2 offset = Vector2.zero;
            if (_game.Shake > 0f)
            {
                _shakeSeed += 1f;
                float amount = _shakePx * _game.Shake;
                offset = new Vector2(
                    Mathf.Round((Mathf.PerlinNoise(_shakeSeed * 0.7f, 0f) - 0.5f) * 2f * amount),
                    Mathf.Round((Mathf.PerlinNoise(0f, _shakeSeed * 0.7f) - 0.5f) * 2f * amount));
            }

            _worldRoot.localPosition = new Vector3(offset.x, -offset.y, 0f) / ViewUtils.PPU;
        }

        private static ArtFrame Frame(string sheet, string tag, int index = 0)
        {
            return Catalog.GetFrame(sheet, tag, index);
        }

        private static ArtFrame Animated(string sheet, string tag, float time, bool isLooping = true)
        {
            return Catalog.GetAnimated(sheet, tag, time, isLooping);
        }

        private static Vector2 ToPixel(Vector2 logic)
        {
            return ViewUtils.ToPixel(logic);
        }
    }
}
