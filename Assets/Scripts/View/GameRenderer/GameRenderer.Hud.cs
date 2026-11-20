using ShiftMagicSpell.Core;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    public partial class GameRenderer
    {
        private static readonly Color OverlayColor = PaletteUtils.Hex(0x0c0d11, 0.88f);

        private void DrawHud()
        {
            if (_game.State == GameState.Ready)
            {
                return;
            }

            // 플레이어 HP: 왼쪽 위 칸
            for (int i = 0; i < _game.Config.PlayerHp; i++)
            {
                _canvas.DrawFrame(Frame("HpCell", i < _game.Player.Hp ? "Full" : "Empty"), 8f + i * 9f, 6f, Color.white);
            }

            _text.Draw($"PHASE {_game.Phase + 1}/{_game.Data.Phases.Count}", 8f, 17f, PaletteUtils.Dim);
            if (_game.Toast != null)
            {
                _text.Draw(_game.Toast.Text, PixelCanvas.SCREEN_W * 0.5f, 192f, PaletteUtils.Glow.WithAlpha(Mathf.Min(1f, _game.Toast.T * 2f)), TextAlign.Center, 1f, Color.black);
            }
        }

        private void DrawOverlay()
        {
            switch (_game.State)
            {
                case GameState.Ready:
                    DrawCenterPanel("SHIFT MAGIC SPELL", "CLICK TO START", PaletteUtils.Text);
                    DrawReadyHelp();
                    break;
                case GameState.Dead:
                    DrawCenterPanel("YOU FELL", $"PHASE {_game.Phase + 1} AGAIN", PaletteUtils.Hp);
                    break;
                default:
                    if (_game.Banner != null)
                    {
                        _text.Draw(_game.Banner.Text, PixelCanvas.SCREEN_W * 0.5f, 52f, PaletteUtils.Text.WithAlpha(Mathf.Min(1f, _game.Banner.T)), TextAlign.Center, 2f, Color.black);
                    }

                    break;
            }

            if (_game.IsPaused)
            {
                DrawCenterPanel("PAUSED", "P: RESUME  R: RESTART PHASE", PaletteUtils.Text);
            }
        }

        private void DrawCenterPanel(string title, string subtitle, Color titleColor)
        {
            _canvas.DrawRect(0f, 0f, PixelCanvas.SCREEN_W, PixelCanvas.SCREEN_H, OverlayColor);
            _text.Draw(title, PixelCanvas.SCREEN_W * 0.5f, 70f, titleColor, TextAlign.Center, 2f, Color.black);
            _text.Draw(subtitle, PixelCanvas.SCREEN_W * 0.5f, 96f, PaletteUtils.Dim, TextAlign.Center);
        }

        private void DrawReadyHelp()
        {
            string[] lines =
            {
                "MOVE: WASD / ↑↓←→",
                "SHIFT + ↑↓←→: ROTATE YOUR ROW / COLUMN",
                "LINE UP THE WORD IN ONE ROW OR COLUMN",
                "SPACE ON A SLOT: CAST   P: PAUSE   R: RESTART",
            };
            for (int i = 0; i < lines.Length; i++)
            {
                _text.Draw(lines[i], PixelCanvas.SCREEN_W * 0.5f, 120f + i * 11f, PaletteUtils.Dim, TextAlign.Center);
            }
        }
    }
}
