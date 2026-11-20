using System.Collections.Generic;
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

            // Shift를 누르고 있으면 돌릴 수 있는 행·열을 하늘색 테로 보여 준다
            bool isShowingBand = player.IsShiftHeld && player.Board == boardIndex && _game.State == GameState.Play
                && player.Here != null && !player.Here.IsShift && !player.Here.IsSlot;

            // 줄 돌리기 잔상: 미끄러지는 키 뒤로 옅은 키 두 장을 남겨 움직이는 방향이 보이게 한다
            foreach (BoardCell cell in board.KeyCells())
            {
                SlideAnim slide = cell.Key.Slide;
                if (slide == null || slide.T < slide.Delay)
                {
                    continue;
                }

                Vector2 home = ViewUtils.CellTopLeft(boardIndex, cell.R, cell.C);
                foreach ((float lag, float alpha) in GhostTrail)
                {
                    Vector3 ghost = slide.GetOffsetAt(slide.T - lag);
                    Vector2 position = home + ViewUtils.ToPixelDelta(ghost);
                    _canvas.DrawRect(position.x + 3f, position.y + 3f, ViewUtils.CELL_W - 6f, ViewUtils.CELL_W - 6f, PaletteUtils.ShiftHi.WithAlpha(alpha * ghost.z));
                }
            }

            // 움직이는 키를 위에 그린다
            foreach (bool isMovingPass in new[] { false, true })
            {
                foreach (BoardCell cell in board.KeyCells())
                {
                    if ((cell.Key.Slide != null) != isMovingPass)
                    {
                        continue;
                    }

                    bool isOnBand = isShowingBand && (cell.R == player.R || cell.C == player.C);
                    DrawKey(boardIndex, cell, isOnBand);
                }
            }
        }

        private static readonly (float Lag, float Alpha)[] GhostTrail = { (0.07f, 0.28f), (0.035f, 0.45f) };

        private void DrawKey(int boardIndex, BoardCell cell, bool isOnBand)
        {
            KeyTile key = cell.Key;
            Vector3 offset = SlideAnim.GetOffset(key.Slide);
            Vector2 topLeft = ViewUtils.CellTopLeft(boardIndex, cell.R, cell.C) + ViewUtils.ToPixelDelta(offset);
            var tint = new Color(1f, 1f, 1f, offset.z);
            bool isMoving = offset.x != 0f || offset.y != 0f;
            _canvas.DrawFrame(Frame("Keycap", key.HasLetter ? "Base" : "Blank"), topLeft.x, topLeft.y, tint);
            if (isMoving)
            {
                _canvas.DrawFrame(Frame("KeyRowBand", "Band", 1), topLeft.x, topLeft.y, tint);
            }
            else if (isOnBand)
            {
                _canvas.DrawFrame(Frame("KeyRowBand", "Band", 0), topLeft.x, topLeft.y, tint);
            }

            if (key.HasLetter && _text.TryGetGlyph(key.Letter, out var glyph))
            {
                _canvas.DrawFrame(glyph, topLeft.x + GLYPH_OFFSET_X, topLeft.y + GLYPH_OFFSET_Y, PaletteUtils.Text.WithAlpha(offset.z));
            }
        }

        /// <summary>
        /// 플레이어 줄 돌리기: 줄 전체에 파란 띠가 깔리고, 그 위로 › 가 돌리는 방향으로 흐른다.
        /// 양 끝 바깥에는 큰 화살표: 한쪽 끝에서 밀려 나간 키가 반대쪽 끝으로 들어온다는 뜻.
        /// </summary>
        private void DrawRotateFx(FxEvent fx)
        {
            float u = fx.T / fx.Duration;
            List<BoardCell> cells = _game.Boards[fx.Board].LineCells(fx.Axis, fx.Index);
            cells.RemoveAll(cell => cell.IsShift);
            if (cells.Count == 0)
            {
                return;
            }

            bool isRow = fx.Axis == LineAxis.Row;
            var unit = new Vector2(isRow ? fx.Sign : 0f, isRow ? 0f : fx.Sign);
            float angle = -Mathf.Atan2(unit.y, unit.x) * Mathf.Rad2Deg;
            float flow = ((1f - u) * 2.5f) % 1f;
            foreach (BoardCell cell in cells)
            {
                Vector2 topLeft = ViewUtils.CellTopLeft(fx.Board, cell.R, cell.C);
                _canvas.DrawRect(topLeft.x, topLeft.y, ViewUtils.CELL_W, ViewUtils.CELL_W, PaletteUtils.Shift.WithAlpha(u * 0.3f));
            }

            Color arrowTint = Color.white.WithAlpha(u);
            foreach (BoardCell cell in cells)
            {
                // 글자를 가리지 않게 키 가장자리(행은 아래, 열은 오른쪽)에
                Vector2 center = ViewUtils.CellCenter(fx.Board, cell.R, cell.C);
                Vector2 edge = isRow ? new Vector2(0f, 6f) : new Vector2(6f, 0f);
                Vector2 travel = ViewUtils.ToPixelDelta(unit * (flow - 0.5f));
                Vector2 position = center + edge + travel;
                _canvas.DrawFrameCentered(Frame("RotateChevron", "Chevron"), position.x, position.y, arrowTint, angle);
            }

            Vector2 first = ViewUtils.CellCenter(fx.Board, cells[0].R, cells[0].C);
            Vector2 last = ViewUtils.CellCenter(fx.Board, cells[cells.Count - 1].R, cells[cells.Count - 1].C);
            Vector2 outEnd = fx.Sign > 0 ? last : first;
            Vector2 inEnd = fx.Sign > 0 ? first : last;
            Vector2 gap = ViewUtils.ToPixelDelta(unit * 0.85f);
            _canvas.DrawFrameCentered(Frame("EdgeArrow", "Arrow"), outEnd.x + gap.x, outEnd.y + gap.y, arrowTint, angle);
            _canvas.DrawFrameCentered(Frame("EdgeArrow", "Arrow"), inEnd.x - gap.x, inEnd.y - gap.y, arrowTint, angle);
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
                    case FxKind.Rotate:
                        DrawRotateFx(fx);
                        break;
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
