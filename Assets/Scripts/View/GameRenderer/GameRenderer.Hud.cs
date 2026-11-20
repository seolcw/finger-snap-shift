using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
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
            DrawHandHp();
            DrawWordPanel();
            DrawSlotPrompt();
            if (_game.Toast != null)
            {
                _text.Draw(_game.Toast.Text, PixelCanvas.SCREEN_W * 0.5f, 191f, PaletteUtils.Glow.WithAlpha(Mathf.Min(1f, _game.Toast.T * 2f)), TextAlign.Center, 1f, Color.black);
            }
        }

        // 손 HP: 오른쪽 위에 HP 한 칸 = 4px 막대
        private void DrawHandHp()
        {
            HandController hand = _game.Hand;
            int maxHp = hand.MaxHp;
            float right = PixelCanvas.SCREEN_W - 8f;
            float left = right - maxHp * 4f;
            for (int i = 0; i < maxHp; i++)
            {
                _canvas.DrawFrame(Frame("HpBar", i < hand.Hp ? "Fill" : "Empty"), left + i * 4f, 6f, Color.white);
            }

            string label = hand.State == HandState.Stun ? $"STUN  HAND {hand.Hp}/{maxHp}" : $"HAND {hand.Hp}/{maxHp}";
            _text.Draw(label, right, 17f, hand.State == HandState.Stun ? PaletteUtils.Glow : PaletteUtils.Dim, TextAlign.Right);
        }

        // 목표 단어 판: 마법 아이콘 + 단어(가장 많이 맞춘 줄에서 제자리에 온 글자는 초록) + 진행도. 어느 줄인지는 안 알려 준다.
        private void DrawWordPanel()
        {
            string word = _game.Words.Current;
            if (string.IsNullOrEmpty(word))
            {
                return;
            }

            AlignResult best = _game.Words.BestOrNull();
            BoardModel bestBoard = best != null ? _game.Boards[best.Board] : null;
            SpellDef spell = _game.Data.GetSpellOrNull(word);
            int count = best != null ? best.Count : 0;
            string progress = $"{count}/{word.Length}";
            float wordWidth = _text.Measure(word);
            float progressWidth = _text.Measure(progress);
            float width = 4f + 12f + 4f + wordWidth + 6f + progressWidth + 5f;
            float x = Mathf.Round((PixelCanvas.SCREEN_W - width) * 0.5f);
            const float y = 4f;
            _canvas.DrawNineSlice(Frame("Panel", "Panel"), x, y, width, 16f, 3, Color.white);
            _canvas.DrawFrame(Frame("SpellIcon", spell?.Icon), x + 4f, y + 2f, Color.white);
            float cursor = x + 20f;
            for (int i = 0; i < word.Length; i++)
            {
                bool isOk = bestBoard != null && i < best.Cells.Count && bestBoard.LetterAt(best.Cells[i].R, best.Cells[i].C) == word[i];
                _text.Draw(word[i].ToString(), cursor, y + 5f, isOk ? PaletteUtils.Ok : PaletteUtils.Text);
                cursor += 6f;
            }

            Color progressColor = _game.HintAlpha() > 0f ? PaletteUtils.Glow : PaletteUtils.Dim;
            _text.Draw(progress, cursor + 5f, y + 5f, progressColor);
            string next = _game.Words.Next != null ? $"NEXT {_game.Words.Next}" : "FINISHER";
            _text.Draw(next, PixelCanvas.SCREEN_W * 0.5f, y + 19f, _game.Words.Next != null ? PaletteUtils.Dim : PaletteUtils.Glow, TextAlign.Center);
        }

        // 슬롯 위에 서 있으면 아래쪽에 발동 안내(콤보가 가능한 시간이면 COMBO!)
        private void DrawSlotPrompt()
        {
            BoardCell here = _game.Player.Here;
            if (_game.State != GameState.Play || here == null || !here.IsSlot || here.Block == null)
            {
                return;
            }

            string label = $"CAST {here.Block.Word}" + (_game.IsComboOpen(_game.Player.Board) ? "  COMBO!" : string.Empty);
            float width = 24f + 4f + _text.Measure(label);
            float x = Mathf.Round((PixelCanvas.SCREEN_W - width) * 0.5f);
            bool isPressedFrame = Mathf.FloorToInt(_game.Time * 2.5f) % 2 == 0;
            _canvas.DrawFrame(Frame("SpaceKey", isPressedFrame ? "Pressed" : "Base"), x, 175f, Color.white);
            _text.Draw(label, x + 28f, 179f, PaletteUtils.Glow, TextAlign.Left, 1f, Color.black);
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
