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
                    WarningCellView view = _warningPool.Next();
                    view.transform.localPosition = ViewUtils.TopLeftToWorld(topLeft, ViewUtils.CellH);

                    // 줄 섞기만 칸마다 미는 양(→1 …)을 붙인다
                    string badge = attack.Type == AttackType.Flick && !cell.IsShift ? attack.Badge : null;
                    view.Render(isWarning, isBlinkOn, _game.Time, badge);
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
                ArtFrame frame = Animated("SummonBullet", "Spin", _game.Time);
                SpriteRenderer view = _bulletPool.Next();
                view.sprite = frame.Sprite;
                view.transform.localPosition = ViewUtils.PixelToWorld(position - (Vector2)frame.CanvasSize * 0.5f + new Vector2(0f, frame.CanvasSize.y));
            }
        }
    }
}
