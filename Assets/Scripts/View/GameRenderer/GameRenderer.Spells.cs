using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 단어별 공격 마법 연출(attackPatterns.json spells의 fx). 앞 55%는 맞춘 줄(또는 슬롯)에서 손까지 날아가고, 뒤 45%는 손에서 터진다.
    /// 웹 canvas 도형 대신 Assets/Art/Fx 스프라이트를 쓰고, 거리는 웹 px × 0.32(칸 50px → 16px)로 줄였다.
    /// </summary>
    public partial class GameRenderer
    {

        private struct SpellContext
        {
            public FxEvent Fx;
            public Vector2 From;
            public Vector2 To;
            public float V;   // 날아가는 진행도(0~1)
            public float W;   // 터지는 진행도(0~1, 날아가는 중에는 음수)
            public Color Color;
            public int Count;
        }

        private void DrawSpell(FxEvent fx)
        {
            float u = 1f - fx.T / fx.Duration;
            var context = new SpellContext
            {
                Fx = fx,
                From = ToPixel(fx.From),
                To = ToPixel(_game.Hand.Pos) - new Vector2(0f, 3f),
                V = u / _spellTravelRatio,
                W = (u - _spellTravelRatio) / (1f - _spellTravelRatio),
                Color = fx.Color,
                Count = Mathf.Max(1, fx.Count),
            };

            switch (fx.Style)
            {
                case "fireball":
                case "dragon":
                    DrawFireball(context, fx.Style == "dragon");
                    break;
                case "rocket":
                    DrawRocket(context);
                    break;
                case "shards":
                    DrawShards(context);
                    break;
                case "bolt":
                    DrawBolt(context);
                    break;
                case "beam":
                    DrawBeam(context);
                    break;
                case "gust":
                    DrawGust(context);
                    break;
                case "nova":
                case "sparkle":
                    DrawStars(context, fx.Style == "sparkle");
                    break;
                case "slash":
                    DrawSlash(context);
                    break;
                case "shift":
                    DrawShiftSpell(context);
                    break;
                default:
                    DrawBeam(context);
                    break;
            }
        }

        private static float Ease(float v)
        {
            return 1f - (1f - v) * (1f - v);
        }

        // 시드 고정 난수(0~1): 같은 마법은 프레임마다 같은 모양
        private static float SeededRandom(FxEvent fx, float i)
        {
            float s = Mathf.Sin(fx.Seed + i * 12.9898f) * 43758.5453f;
            return s - Mathf.Floor(s);
        }

        // 맞춘 줄에서 손까지 포물선. arc는 위로 휘는 정도, side는 옆으로 퍼지는 정도(웹 px)
        private Vector2 Along(SpellContext context, float v, float arc, float side)
        {
            float e = Ease(Mathf.Clamp01(v));
            Vector2 position = Vector2.Lerp(context.From, context.To, e);
            position.x += side * Mathf.Sin(e * Mathf.PI) * 30f * _spellDistanceScale;
            position.y -= Mathf.Sin(e * Mathf.PI) * arc * _spellDistanceScale;
            return position;
        }

        private static float AngleOf(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            return delta.sqrMagnitude < 0.0001f ? 0f : -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        }

        private void DrawFireball(SpellContext context, bool isDragon)
        {
            int balls = isDragon ? 3 : 1;
            float scale = isDragon ? 1.3f : Mathf.Clamp(context.Fx.Size / 10f, 0.8f, 1.5f);
            for (int ball = 0; ball < balls; ball++)
            {
                float vb = context.V - ball * 0.18f;
                if (vb < 0f || vb > 1f)
                {
                    continue;
                }

                float arc = 50f + ball * 25f;
                float side = ball - (balls - 1) * 0.5f;
                for (int i = 4; i >= 1; i--)
                {
                    Vector2 trail = Along(context, vb - i * 0.05f, arc, side);
                    _canvas.DrawDisc(trail, 4f * scale * (1f - i * 0.15f), context.Color.WithAlpha((1f - i / 5f) * 0.7f));
                }

                Vector2 position = Along(context, vb, arc, side);
                float angle = AngleOf(Along(context, vb - 0.03f, arc, side), position);
                _canvas.DrawFrameCentered(Animated("Fireball", "Fly", _game.Time), position.x, position.y, Color.Lerp(Color.white, context.Color, 0.35f), angle, scale);
            }

            if (context.W > 0f)
            {
                float explosionScale = isDragon ? 2.2f : 1.2f + scale * 0.2f;
                _canvas.DrawFrameCentered(Animated("Explosion", "Burst", context.W * 0.4f, false), context.To.x, context.To.y, Color.white.WithAlpha(1f - context.W * 0.6f), 0f, explosionScale);
                _canvas.DrawRing(context.To, (isDragon ? 32f : 20f) * context.W, 1, context.Color.WithAlpha(1f - context.W));
            }
        }

        private void DrawRocket(SpellContext context)
        {
            if (context.V <= 1f)
            {
                for (int i = 1; i < 8; i++)
                {
                    Vector2 smoke = Along(context, context.V - i * 0.035f, 70f, 0.6f);
                    _canvas.DrawDisc(smoke, 1.5f + i * 0.5f, Colors.Smoke.WithAlpha(0.5f * (1f - i / 8f)));
                }

                Vector2 position = Along(context, context.V, 70f, 0.6f);
                float angle = AngleOf(position, Along(context, context.V + 0.02f, 70f, 0.6f));
                _canvas.DrawFrameCentered(Animated("Rocket", "Fly", _game.Time), position.x, position.y, Color.white, angle);
            }

            if (context.W > 0f)
            {
                for (int i = 0; i < 3; i++)
                {
                    var offset = new Vector2((SeededRandom(context.Fx, i) - 0.5f) * 14f, (SeededRandom(context.Fx, i + 5) - 0.5f) * 10f);
                    _canvas.DrawFrameCentered(Animated("Explosion", "Burst", context.W * 0.4f + i * 0.05f, false), context.To.x + offset.x, context.To.y + offset.y, Color.white.WithAlpha(1f - context.W * 0.5f), 0f, 1.6f - i * 0.3f);
                }

                _canvas.DrawRing(context.To, 30f * context.W, 2, context.Color.WithAlpha(1f - context.W));
            }
        }

        private void DrawShards(SpellContext context)
        {
            for (int i = 0; i < context.Count; i++)
            {
                float vi = context.V - i * 0.05f;
                if (vi < 0f || vi > 1f)
                {
                    continue;
                }

                Vector2 position = Along(context, vi, 40f + SeededRandom(context.Fx, i) * 60f, (SeededRandom(context.Fx, i + 9) - 0.5f) * 3f);
                _canvas.DrawFrameCentered(Frame("IceShard", "Shard"), position.x, position.y, Color.white, (_game.Time * 8f + i) * Mathf.Rad2Deg);
            }

            if (context.W > 0f)
            {
                float length = 16f * Ease(context.W);
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4f;
                    var tip = context.To + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;
                    _canvas.DrawLine(context.To, tip, 1f, context.Color.WithAlpha(1f - context.W));
                    _canvas.DrawFrameCentered(Frame("IceShard", "Shard"), tip.x, tip.y, Color.white.WithAlpha(1f - context.W), -angle * Mathf.Rad2Deg + 90f);
                }

                _canvas.DrawDisc(context.To, 14f * Ease(context.W), context.Color.WithAlpha((1f - context.W) * 0.35f));
            }
        }

        private void DrawBolt(SpellContext context)
        {
            if (context.V <= 1f)
            {
                // 모으는 동안 출발 자리에서 불꽃이 튄다
                int tick = Mathf.FloorToInt(_game.Time * 30f);
                for (int i = 0; i < 4; i++)
                {
                    var spark = context.From + new Vector2((SeededRandom(context.Fx, i + tick) - 0.5f) * 16f, (SeededRandom(context.Fx, i + 3 + tick) - 0.5f) * 10f);
                    _canvas.DrawRect(spark.x, spark.y, 1f, 1f, context.Color);
                }
            }

            if (context.W > 0f)
            {
                ArtFrame strike = context.W < 0.7f ? Animated("Lightning", "Strike", _game.Time) : Frame("Lightning", "Fade");
                float height = Mathf.Max(48f, context.To.y + 6f);
                for (int i = 0; i < context.Count; i++)
                {
                    float offset = (i - (context.Count - 1) * 0.5f) * 12f;
                    _canvas.DrawFrameStretched(strike, new Vector2(context.To.x + offset, context.To.y - height * 0.5f + 6f), new Vector2(1f, height / 48f), 0f, Color.white.WithAlpha(1f - context.W * 0.8f));
                }

                _canvas.DrawRect(0f, 0f, ViewUtils.SCREEN_W, ViewUtils.SCREEN_H, Color.white.WithAlpha((1f - context.W) * 0.25f));
            }
        }

        private void DrawBeam(SpellContext context)
        {
            float length = Mathf.Min(1f, context.V);
            bool isTriple = context.Count > 1;
            int beams = isTriple ? 3 : 1;
            float thickness = isTriple ? 1f : 2f;
            float grow = context.W > 0f ? 1f + context.W : 1f;
            float alpha = context.W > 0f ? 1f - context.W : 0.95f;
            for (int i = 0; i < beams; i++)
            {
                // 여러 줄 광선은 진행 방향에 수직으로 벌린다
                Vector2 direction = (context.To - context.From).normalized;
                var offset = new Vector2(-direction.y, direction.x) * ((i - (beams - 1) * 0.5f) * 4f);
                Vector2 start = context.From + offset;
                Vector2 end = Vector2.Lerp(context.From, context.To, length) + offset;
                float distance = Vector2.Distance(start, end);
                if (distance < 1f)
                {
                    continue;
                }

                float angle = AngleOf(start, end);
                _canvas.DrawFrameStretched(Frame("Beam", "Segment"), (start + end) * 0.5f, new Vector2(distance / 8f, thickness * grow * 0.5f), angle, context.Color.WithAlpha(alpha));
                _canvas.DrawFrameCentered(Frame("Beam", "Head"), end.x, end.y, context.Color.WithAlpha(alpha), angle, thickness * 0.5f + 0.5f);
            }

            if (context.W > 0f)
            {
                _canvas.DrawDisc(context.To, 10f * Ease(context.W), context.Color.WithAlpha(1f - context.W));
            }
        }

        private void DrawGust(SpellContext context)
        {
            for (int i = 0; i < context.Count; i++)
            {
                float vi = context.V - i * 0.06f;
                if (vi < 0f)
                {
                    continue;
                }

                bool isFlying = vi <= 1f;
                Vector2 position = isFlying ? Along(context, vi, 30f, i % 2 == 1 ? 1f : -1f) : context.To;
                float scale = isFlying ? 1f : 1f + context.W * 2.5f;
                float alpha = isFlying ? 0.95f : 1f - context.W;
                _canvas.DrawFrameCentered(Animated("Gust", "Spin", _game.Time + i * 0.1f), position.x, position.y, context.Color.WithAlpha(alpha), 0f, scale);
            }
        }

        private void DrawStars(SpellContext context, bool isSparkle)
        {
            int count = isSparkle ? context.Count : 1;
            float starScale = isSparkle ? 1f : 2f;
            for (int i = 0; i < count; i++)
            {
                float vi = context.V - i * 0.04f;
                if (vi < 0f || vi > 1f)
                {
                    continue;
                }

                Vector2 position = Along(context, vi, 40f + Mathf.Sin(vi * 12f + i) * 20f, (SeededRandom(context.Fx, i) - 0.5f) * 3f);
                _canvas.DrawFrameCentered(Animated("Star", "Twinkle", _game.Time + i * 0.1f), position.x, position.y, context.Color, (_game.Time * 6f + i) * Mathf.Rad2Deg, starScale);
            }

            if (context.W > 0f)
            {
                _canvas.DrawRing(context.To, 26f * Ease(context.W), 2, context.Color.WithAlpha(1f - context.W));
                _canvas.DrawRing(context.To, 16f * Ease(context.W), 1, context.Color.WithAlpha(1f - context.W));
                if (isSparkle)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * Mathf.PI / 4f + context.W;
                        Vector2 position = context.To + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (18f * Ease(context.W));
                        _canvas.DrawFrameCentered(Animated("Star", "Twinkle", _game.Time), position.x, position.y, context.Color.WithAlpha(1f - context.W));
                    }
                }
            }
        }

        private void DrawSlash(SpellContext context)
        {
            if (context.V <= 1f)
            {
                Vector2 position = Along(context, context.V, 20f, 0f);
                _canvas.DrawFrameCentered(Frame("Slash", "Swing", 2), position.x, position.y, context.Color, _game.Time * 20f * Mathf.Rad2Deg, 0.5f);
            }

            if (context.W <= 0f)
            {
                return;
            }

            for (int i = 0; i < context.Count; i++)
            {
                float wi = Mathf.Clamp01(context.W * 2f - i * 0.35f);
                if (wi <= 0f)
                {
                    continue;
                }

                int frame = Mathf.Min(2, Mathf.FloorToInt(wi * 3f));
                var position = context.To + new Vector2((i - 1) * 6f, -6f + i * 5f);
                _canvas.DrawFrameCentered(Frame("Slash", "Swing", frame), position.x, position.y, context.Color.WithAlpha(1f - context.W), -30f + i * 30f, 1f, i % 2 == 1);
            }
        }

        private void DrawShiftSpell(SpellContext context)
        {
            if (context.V <= 1f)
            {
                Vector2 position = Along(context, context.V, 90f, 0f);
                _canvas.DrawFrameCentered(Frame("ShiftArrow", "Arrow"), position.x, position.y, Color.white, 0f, 2f);
            }

            if (context.W > 0f)
            {
                _canvas.DrawRing(context.To, 84f * Ease(context.W), 3, context.Color.WithAlpha(1f - context.W));
                _canvas.DrawRing(context.To, 52f * Ease(context.W), 1, context.Color.WithAlpha(1f - context.W));
                _canvas.DrawRect(0f, 0f, ViewUtils.SCREEN_W, ViewUtils.SCREEN_H, context.Color.WithAlpha((1f - context.W) * 0.3f));
            }
        }
    }
}
