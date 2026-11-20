using ShiftMagicSpell.Core;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    public partial class GameRenderer
    {
        private const float GLYPH_OFFSET_X = 5f;
        private const float GLYPH_OFFSET_Y = 5f;
        private const float PLAYER_LIFT = 6f;

        private void DrawBoard(int boardIndex)
        {
            BoardModel board = _game.Boards[boardIndex];
            PlayerController player = _game.Player;

            // 칸 자리(홈)·SHIFT 칸·슬롯은 움직이지 않는다
            foreach (BoardCell cell in board.AllCells)
            {
                Vector2 topLeft = ViewUtils.CellTopLeft(boardIndex, cell.R, cell.C);
                if (cell.IsSlot)
                {
                    bool isActive = player.Board == boardIndex && cell.Is(player.R, player.C);
                    _canvas.DrawFrame(Frame("AttackSlot", isActive ? "Active" : "Empty"), topLeft.x, topLeft.y, Color.white);
                    continue;
                }

                if (cell.IsShift)
                {
                    _canvas.DrawFrame(Frame("Keycap", "Shift"), topLeft.x, topLeft.y, Color.white);
                    if (player.ShiftCd > 0f)
                    {
                        float ratio = player.ShiftCd / _game.Config.ShiftCooldown;
                        _canvas.DrawRect(topLeft.x, topLeft.y, ViewUtils.CELL_W, Mathf.Round(16f * ratio), PaletteUtils.Hex(0x0a0c12, 0.6f));
                    }

                    continue;
                }

                // 키 아래 빈 홈: 키가 미끄러지는 동안 보인다
                _canvas.DrawRect(topLeft.x + 1f, topLeft.y + 1f, ViewUtils.CELL_W - 2f, ViewUtils.CELL_H - 2f, PaletteUtils.Hex(0x0f1116));
            }

            foreach (BoardCell cell in board.KeyCells())
            {
                DrawKey(boardIndex, cell, false);
            }
        }

        private void DrawKey(int boardIndex, BoardCell cell, bool isMovingPass)
        {
            KeyTile key = cell.Key;
            Vector3 offset = SlideAnim.GetOffset(key.Slide);
            Vector2 topLeft = ViewUtils.CellTopLeft(boardIndex, cell.R, cell.C) + ViewUtils.ToPixelDelta(offset);
            var tint = new Color(1f, 1f, 1f, offset.z);
            _canvas.DrawFrame(Frame("Keycap", key.HasLetter ? "Base" : "Blank"), topLeft.x, topLeft.y, tint);
            if (key.HasLetter && _text.TryGetGlyph(key.Letter, out var glyph))
            {
                _canvas.DrawFrame(glyph, topLeft.x + GLYPH_OFFSET_X, topLeft.y + GLYPH_OFFSET_Y, PaletteUtils.Text.WithAlpha(offset.z));
            }
        }

        private void DrawPlayer()
        {
            PlayerController player = _game.Player;
            if (_game.State == GameState.Ready)
            {
                return;
            }

            Vector3 offset = SlideAnim.GetOffset(player.Slide);
            Vector2 topLeft = ViewUtils.CellTopLeft(player.Board, player.R, player.C) + ViewUtils.ToPixelDelta(offset);
            bool isBlinking = player.Invuln > 0f && Mathf.FloorToInt(_game.Time * 16f) % 2 == 0;
            bool isHurt = player.Invuln > _game.Config.HitInvuln - 0.35f && _game.Stats.Hits.Count > 0 && player.Hp < _game.Config.PlayerHp;
            var tint = new Color(1f, 1f, 1f, offset.z * (isBlinking ? 0.35f : 1f));
            _canvas.DrawFrame(isHurt ? Frame("Player", "Hurt") : Animated("Player", "Idle", _game.Time), topLeft.x, topLeft.y - PLAYER_LIFT, tint);
            if (player.IsShiftHeld)
            {
                _canvas.DrawRectOutline(topLeft.x - 1f, topLeft.y - PLAYER_LIFT - 1f, 18f, 18f, 1f, PaletteUtils.ShiftHi);
            }

            if (_game.IsInvincible)
            {
                _canvas.DrawRectOutline(topLeft.x - 2f, topLeft.y - PLAYER_LIFT - 2f, 20f, 20f, 1f, PaletteUtils.Glow);
            }
        }

        // 칸에 붙는 짧은 연출: 순간이동 고리, 못 하는 동작 표시
        private void DrawCellFx()
        {
            foreach (FxEvent fx in _game.Fx)
            {
                switch (fx.Kind)
                {
                    case FxKind.Warp:
                        {
                            Vector2 center = ViewUtils.CellCenter(fx.Board, fx.R, fx.C);
                            float u = 1f - fx.T / fx.Duration;
                            _canvas.DrawRing(center, 4f + u * 28f, 1, PaletteUtils.Shift.WithAlpha(fx.T / fx.Duration));
                            break;
                        }

                    case FxKind.Deny:
                        {
                            Vector2 topLeft = ViewUtils.CellTopLeft(fx.Board, fx.R, fx.C);
                            _canvas.DrawRectOutline(topLeft.x - 1f, topLeft.y - 1f, 18f, 18f, 1f, PaletteUtils.Dim.WithAlpha(fx.T / fx.Duration));
                            break;
                        }

                    case FxKind.Float:
                        {
                            Vector2 position = ToPixel(fx.From) - new Vector2(0f, (1f - fx.T / fx.Duration) * 10f);
                            _text.Draw(fx.Text, position.x, position.y, fx.Color.WithAlpha(Mathf.Min(1f, fx.T * 2f)), TextAlign.Center, 1f, Color.black);
                            break;
                        }

                    default:
                        break;
                }
            }
        }
    }
}
