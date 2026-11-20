using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// GameSession 상태를 PixelCanvas에 그린다(웹 Game.render를 옮긴 것). 그리는 순서가 곧 앞뒤 순서다.
    /// 판·플레이어(Board), 손·예고(Hand), 마법(Spells), 화면 글자(Hud)는 partial 파일로 나눴다.
    /// </summary>
    public partial class GameRenderer : MonoBehaviour
    {
        [SerializeField] private PixelCanvas _canvas;
        [SerializeField] private ArtCatalog _catalog;

        private PixelTextDrawer _text;
        private GameSession _game;
        private float _shakeSeed;

        private void Awake()
        {
            _text = new PixelTextDrawer(_canvas, _catalog);
        }

        public void Render(GameSession game)
        {
            _game = game;
            _canvas.BeginFrame();
            ApplyShake();
            DrawBoard(0);
            DrawBoard(1);
            DrawWarnings();
            DrawBullets();
            DrawPlayer();
            DrawHand();
            DrawCellFx();
            _canvas.Offset = Vector2.zero;
            DrawHud();
            DrawOverlay();
            _canvas.EndFrame();
        }

        // 화면 흔들림: 판과 연출만 흔들고 화면 글자는 고정. 웹과 같은 세기(±4px × shake × 3)를 1/3로 줄여 픽셀 화면에 맞춘다.
        private void ApplyShake()
        {
            if (_game.Shake <= 0f)
            {
                _canvas.Offset = Vector2.zero;
                return;
            }

            _shakeSeed += 1f;
            float amount = 4f * _game.Shake;
            _canvas.Offset = new Vector2(
                Mathf.Round((Mathf.PerlinNoise(_shakeSeed * 0.7f, 0f) - 0.5f) * 2f * amount),
                Mathf.Round((Mathf.PerlinNoise(0f, _shakeSeed * 0.7f) - 0.5f) * 2f * amount));
        }

        private ArtFrame Frame(string sheet, string tag, int index = 0)
        {
            return _catalog.GetFrame(sheet, tag, index);
        }

        private ArtFrame Animated(string sheet, string tag, float time, bool isLooping = true)
        {
            return _catalog.GetAnimated(sheet, tag, time, isLooping);
        }

        private static Vector2 ToPixel(Vector2 logic)
        {
            return ViewUtils.ToPixel(logic);
        }
    }
}
