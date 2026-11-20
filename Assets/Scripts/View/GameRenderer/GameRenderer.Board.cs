using System.Collections.Generic;
using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
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

            // 단어 위치 힌트(점멸할 때만): 가장 많이 맞춘 자리는 노란 점선, 단어 글자는 노랑, 제자리에 맞은 글자는 초록
            string word = _game.Words.Current ?? string.Empty;
            float hint = _game.State == GameState.Play ? _game.HintAlpha() : 0f;
            AlignResult best = _game.Words.BestOrNull();
            var okCells = new HashSet<BoardCell>();
            if (best != null && best.Board == boardIndex && best.Count > 0)
            {
                for (int i = 0; i < best.Cells.Count; i++)
                {
                    if (board.LetterAt(best.Cells[i].R, best.Cells[i].C) == word[i])
                    {
                        okCells.Add(best.Cells[i]);
                    }
                }

                if (hint > 0f)
                {
                    foreach (BoardCell cell in best.Cells)
                    {
                        Vector2 topLeft = ViewUtils.CellTopLeft(boardIndex, cell.R, cell.C);
                        _canvas.DrawDashedRect(topLeft.x - 1f, topLeft.y - 1f, ViewUtils.CELL_W + 2f, ViewUtils.CELL_H + 1f, 2f, PaletteUtils.Glow.WithAlpha(0.85f * hint));
                    }
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

                    var style = new KeyStyle
                    {
                        IsOnBand = isShowingBand && (cell.R == player.R || cell.C == player.C),
                        IsOk = okCells.Contains(cell),
                        IsInWord = cell.Key.HasLetter && word.IndexOf(cell.Key.Letter) >= 0,
                        Hint = hint,
                    };
                    DrawKey(boardIndex, cell, style);
                }
            }

            foreach (BoardCell slot in board.Slots)
            {
                if (slot.Block != null)
                {
                    DrawBlock(boardIndex, slot);
                }
            }
        }

        private static readonly (float Lag, float Alpha)[] GhostTrail = { (0.07f, 0.28f), (0.035f, 0.45f) };

        private struct KeyStyle
        {
            public bool IsOnBand;
            public bool IsOk;
            public bool IsInWord;
            public float Hint;
        }

        private void DrawKey(int boardIndex, BoardCell cell, KeyStyle style)
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
            else if (style.IsOnBand)
            {
                _canvas.DrawFrame(Frame("KeyRowBand", "Band", 0), topLeft.x, topLeft.y, tint);
            }

            bool isHinted = style.Hint > 0f && (style.IsOk || style.IsInWord);
            if (isHinted && style.IsOk)
            {
                _canvas.DrawFrame(Frame("KeyMatched", "Glow"), topLeft.x, topLeft.y, tint.WithAlpha(offset.z * style.Hint));
            }
            else if (isHinted)
            {
                _canvas.DrawRect(topLeft.x + 2f, topLeft.y + 2f, 12f, 12f, PaletteUtils.Glow.WithAlpha(0.22f * style.Hint * offset.z));
            }

            if (key.HasLetter && _text.TryGetGlyph(key.Letter, out var glyph))
            {
                Color letterColor = PaletteUtils.Text;
                if (isHinted)
                {
                    letterColor = Color.Lerp(PaletteUtils.Text, style.IsOk ? PaletteUtils.Ok : PaletteUtils.Glow, style.Hint);
                }

                _canvas.DrawFrame(glyph, topLeft.x + GLYPH_OFFSET_X, topLeft.y + GLYPH_OFFSET_Y, letterColor.WithAlpha(offset.z));
            }
        }

        /// <summary>
        /// 공격 블록: 마법 색으로 틴트한 블록 위에 마법 아이콘. 들어올 때 커지며 나타나고, 손에게 맞으면 흔들리고 금이 간다.
        /// </summary>
        private void DrawBlock(int boardIndex, BoardCell slot)
        {
            AttackBlock block = slot.Block;
            SpellDef spell = _game.Data.GetSpellOrNull(block.Word);
            Color color = spell != null ? spell.Color : PaletteUtils.Glow;
            float pop = Mathf.Min(1f, block.Age / 0.25f);
            float scale = pop < 1f ? 0.4f + 0.6f * pop + Mathf.Sin(pop * Mathf.PI) * 0.25f : 1f;
            float shake = block.HitT > 0f ? Mathf.Sin(_game.Time * 70f) * 3f * block.HitT / 0.4f : 0f;
            Vector2 topLeft = ViewUtils.CellTopLeft(boardIndex, slot.R, slot.C);
            Vector2 center = topLeft + new Vector2(8f + Mathf.Round(shake), 10f);
            bool isCracked = block.Hp < _game.Config.BlockHp;
            ArtFrame blockFrame = isCracked ? Frame("AttackBlock", "Cracked") : Animated("AttackBlock", "Idle", block.Age);
            _canvas.DrawFrameCentered(blockFrame, center.x, center.y, color, 0f, scale);
            _canvas.DrawFrameCentered(Frame("SpellIcon", spell?.Icon), center.x, center.y - 2f, Color.white, 0f, scale);
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
                    case FxKind.Match:
                        {
                            // 맞춘 칸이 초록으로 번쩍이며 퍼진다
                            float u = fx.T / fx.Duration;
                            float grow = (1f - u) * 3f;
                            foreach (Vector2Int cell in fx.Cells)
                            {
                                Vector2 topLeft = ViewUtils.CellTopLeft(fx.Board, cell.y, cell.x);
                                _canvas.DrawRect(topLeft.x - grow, topLeft.y - grow, ViewUtils.CELL_W + grow * 2f, ViewUtils.CELL_W + grow * 2f, PaletteUtils.Ok.WithAlpha(u * 0.8f));
                            }

                            break;
                        }

                    case FxKind.Store:
                        {
                            // 완성한 단어가 블록이 되어 반대편 슬롯으로 포물선을 그리며 날아간다
                            float u = 1f - fx.T / fx.Duration;
                            float eased = 1f - (1f - u) * (1f - u);
                            Vector2 from = ToPixel(fx.From);
                            Vector2 to = ToPixel(fx.To);
                            Vector2 position = Vector2.Lerp(from, to, eased) - new Vector2(0f, Mathf.Sin(u * Mathf.PI) * 28f);
                            SpellDef spell = _game.Data.GetSpellOrNull(fx.Word);
                            _canvas.DrawFrameCentered(Frame("AttackBlock", "Idle"), position.x, position.y, fx.Color);
                            _canvas.DrawFrameCentered(Frame("SpellIcon", spell?.Icon), position.x, position.y - 2f, Color.white);
                            break;
                        }

                    case FxKind.Fizzle:
                        {
                            // 블록이 깨지거나 사라질 때 조각이 흩어진다
                            Vector2 center = ViewUtils.CellCenter(fx.Board, fx.R, fx.C);
                            float u = 1f - fx.T / fx.Duration;
                            for (int i = 0; i < 6; i++)
                            {
                                float angle = i * Mathf.PI / 3f;
                                Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (14f * u);
                                _canvas.DrawFrameCentered(Animated("BlockDebris", "Debris", u), position.x, position.y + u * u * 8f, Color.white.WithAlpha(1f - u));
                            }

                            break;
                        }

                    case FxKind.Click:
                        {
                            Vector2 center = ToPixel(fx.From);
                            _canvas.DrawRing(center, 3f + (fx.Duration - fx.T) * 40f, 1, PaletteUtils.Warn.WithAlpha(fx.T / fx.Duration));
                            break;
                        }

                    case FxKind.Impact:
                        foreach (Vector2Int cell in fx.Cells)
                        {
                            Vector2 topLeft = ViewUtils.CellTopLeft(fx.Board, cell.y, cell.x);
                            _canvas.DrawRect(topLeft.x + 1f, topLeft.y + 1f, 14f, 14f, PaletteUtils.Warn.WithAlpha(fx.T / fx.Duration * 0.6f));
                        }

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
                            if (fx.Age < fx.Delay)
                            {
                                break;
                            }

                            Vector2 position = ToPixel(fx.From) - new Vector2(0f, Mathf.Clamp01(1f - fx.T / fx.Duration) * 10f);
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
