using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    public partial class GameRenderer
    {
        // 예고: 빨간 테두리 + 깜빡임(KeyWarning), 조준선, 드래그 선택 사각형, 클릭 조준점. 이동 중에는 옅게 미리 보여 준다.
        private void DrawWarnings()
        {
            HandController hand = _game.Hand;
            AttackPlan attack = hand.Attack;
            if (attack == null || (hand.State != HandState.Warn && hand.State != HandState.Travel))
            {
                return;
            }

            bool isWarning = hand.State == HandState.Warn;
            bool isBlinkOn = isWarning && Mathf.FloorToInt(_game.Time * 10f) % 2 == 0;
            bool isTracking = attack.Type == AttackType.Track && !attack.IsLocked;
            Vector2 handPixel = ToPixel(hand.Pos);

            if (attack.Type == AttackType.Aim && isWarning)
            {
                Vector2 from = ToPixel(attack.From);
                Vector2 to = ToPixel(attack.To);
                _canvas.DrawLine(from, to, 8f, Colors.Warn.WithAlpha(isBlinkOn ? 0.35f : 0.2f));
                _canvas.DrawLine(from, to, 1f, Colors.Warn);
            }

            if (attack.Type == AttackType.Drag && isWarning && !hand.HasPath)
            {
                Vector2 from = ToPixel(attack.From);
                float x0 = Mathf.Min(from.x, handPixel.x) - 7f;
                float y0 = Mathf.Min(from.y, handPixel.y) - 7f;
                float width = Mathf.Abs(handPixel.x - from.x) + 14f;
                float height = Mathf.Abs(handPixel.y - from.y) + 14f;
                _canvas.DrawRect(x0, y0, width, height, Colors.Shift.WithAlpha(0.22f));
                _canvas.DrawDashedRect(x0, y0, width, height, 2f, Colors.Shift);
            }

            if (isTracking)
            {
                // 내려찍기 그림자가 플레이어를 따라다닌다
                _canvas.DrawEllipse(handPixel + new Vector2(0f, 6f), 18f, 9f, new Color(0f, 0f, 0f, 0.45f));
            }
            else if (attack.Cells != null && !(attack.IsRecheck && !isWarning))
            {
                foreach (BoardCell cell in attack.Cells)
                {
                    if (cell.IsShift && attack.Type == AttackType.Flick)
                    {
                        continue;
                    }

                    Vector2 topLeft = ViewUtils.CellTopLeft(attack.Board, cell.R, cell.C);
                    if (isWarning)
                    {
                        if (isBlinkOn)
                        {
                            _canvas.DrawRect(topLeft.x + 1f, topLeft.y + 1f, 14f, 14f, Colors.Warn.WithAlpha(0.28f));
                        }

                        _canvas.DrawFrame(Animated("KeyWarning", "Blink", _game.Time), topLeft.x, topLeft.y, Color.white);
                    }
                    else
                    {
                        _canvas.DrawRectOutline(topLeft.x + 1f, topLeft.y + 1f, 14f, 14f, 1f, Colors.Warn.WithAlpha(0.45f));
                    }

                    // 줄 섞기만 칸마다 미는 양(→1 …)을 붙인다
                    if (attack.Type == AttackType.Flick && !cell.IsShift)
                    {
                        _canvas.DrawRect(topLeft.x + 4f, topLeft.y - 4f, 13f, 9f, Colors.Warn);
                        _text.Draw(attack.Badge, topLeft.x + 5f, topLeft.y - 3f, Color.white);
                    }
                }
            }

            if ((attack.Type == AttackType.Click || attack.Type == AttackType.Slot) && isWarning)
            {
                Vector2 center = ToPixel(attack.Center);
                _canvas.DrawRing(center, 5f, 1, Colors.Warn);
                _canvas.DrawRect(center.x - 9f, center.y, 5f, 1f, Colors.Warn);
                _canvas.DrawRect(center.x + 5f, center.y, 5f, 1f, Colors.Warn);
                _canvas.DrawRect(center.x, center.y - 9f, 1f, 5f, Colors.Warn);
                _canvas.DrawRect(center.x, center.y + 5f, 1f, 5f, Colors.Warn);
            }
        }

        private void DrawBullets()
        {
            foreach (Bullet bullet in _game.Hand.Bullets)
            {
                Vector2 logic = bullet.Axis == LineAxis.Row
                    ? new Vector2(GeometryUtils.BoardOffsetX(bullet.Board) + bullet.Pos + 0.5f, bullet.Index + 0.5f)
                    : new Vector2(GeometryUtils.BoardOffsetX(bullet.Board) + bullet.Index + 0.5f, bullet.Pos + 0.5f);
                Vector2 position = ToPixel(logic) - new Vector2(0f, 2f);
                _canvas.DrawFrameCentered(Animated("SummonBullet", "Spin", _game.Time), position.x, position.y, Color.white);
            }
        }

        /// <summary>
        /// 손: 판 위로 올라오면 그림자와 함께 살짝 떠 있다. 줄 섞기는 줄 바깥으로 당겼다가 튕기고, 마법에 맞으면 뒤로 튕기며 깜빡인다.
        /// </summary>
        private void DrawHand()
        {
            if (_game.State == GameState.Ready)
            {
                return;
            }

            HandController hand = _game.Hand;
            AttackPlan attack = hand.Attack;
            bool isBig = attack != null && attack.Type == AttackType.Track;
            Vector2 position = ToPixel(hand.Pos);
            bool isOverBoard = !hand.IsCharging && IsOverBoard(hand.Pos);
            float lift = isOverBoard ? (isBig ? 9f : 3f) : 0f;
            float scale = isOverBoard && isBig ? 1.25f : 1f;

            Vector2 animOffset = Vector2.zero;
            if (hand.Anim == HandController.ANIM_FLICK_CHARGE && attack != null)
            {
                float u = Mathf.Min(1f, hand.AnimT / Mathf.Max(0.05f, attack.Warn));
                animOffset = -ViewUtils.ToPixelDelta(hand.AnimDir) * (0.3f * u);
            }
            else if (hand.Anim == HandController.ANIM_FLICK_RELEASE)
            {
                float u = 1f - Mathf.Min(1f, hand.AnimT / 0.16f);
                animOffset = ViewUtils.ToPixelDelta(hand.AnimDir) * (0.45f * u);
            }

            bool isHurt = hand.Anim == HandController.ANIM_HURT && hand.AnimT < 0.35f;
            if (isHurt)
            {
                float u = 1f - hand.AnimT / 0.35f;
                animOffset += new Vector2(Mathf.Round(Mathf.Sin(hand.AnimT * 90f) * 2f * u), -5f * u);
            }

            if (isOverBoard)
            {
                _canvas.DrawEllipse(position + new Vector2(0f, 2f), isBig ? 14f : 7f, isBig ? 7f : 3f, new Color(0f, 0f, 0f, 0.45f));
            }

            Color tint = Color.white;
            if (hand.IsCharging)
            {
                tint = Colors.HandCharge;
            }
            else if (hand.State == HandState.Stun)
            {
                tint = Colors.HandStun;
            }

            if (isHurt && Mathf.FloorToInt(hand.AnimT * 20f) % 2 == 0)
            {
                tint = tint.WithAlpha(0.4f);
            }

            _canvas.DrawFrameCentered(HandFrame(hand), Mathf.Round(position.x + animOffset.x), Mathf.Round(position.y + animOffset.y - lift), tint, 0f, scale);
        }

        private ArtFrame HandFrame(HandController hand)
        {
            AttackPlan attack = hand.Attack;
            if (hand.Anim == HandController.ANIM_HURT && hand.AnimT < 0.35f)
            {
                return Frame("Hand", "Hurt");
            }

            if (hand.Anim == HandController.ANIM_FLICK_CHARGE || hand.Anim == HandController.ANIM_FLICK_RELEASE)
            {
                return Animated("Hand", hand.Anim, hand.AnimT, false);
            }

            if (attack != null && hand.State == HandState.Warn)
            {
                switch (attack.Type)
                {
                    case AttackType.Click:
                    case AttackType.Slot:
                    case AttackType.Drag:
                        return Animated("Hand", "Click", _game.Time);
                    case AttackType.Track:
                    case AttackType.Full:
                        return Frame("Hand", "SlamRaise");
                    default:
                        break;
                }
            }

            if (attack != null && hand.State == HandState.Exec && attack.Type == AttackType.Full)
            {
                return Frame("Hand", "SlamHit");
            }

            return Animated("Hand", "Idle", _game.Time);
        }

        private static bool IsOverBoard(Vector2 logic)
        {
            for (int board = 0; board < 2; board++)
            {
                float x = GeometryUtils.BoardOffsetX(board);
                if (logic.x > x && logic.x < x + GeometryUtils.COLS && logic.y > 0f && logic.y < GeometryUtils.ROWS)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
