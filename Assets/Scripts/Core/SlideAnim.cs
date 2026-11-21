using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 줄 돌리기 연출: 키(또는 같이 밀린 플레이어)가 예전 자리에서 새 자리로 미끄러진다.
    /// 끝을 넘은 키(IsWrap)는 밖으로 튀며 사라졌다가 반대편 끝에서 들어온다. 값은 칸 단위.
    /// </summary>
    public class SlideAnim
    {
        private const float WRAP_OVERSHOOT = 1.6f;

        public Vector2 Start;     // 새 자리 기준 처음 오프셋
        public Vector2 Unit;      // 미는 방향
        public bool IsWrap;
        public float Delay;
        public float Duration;
        public float T;

        public bool IsDone => T >= Delay + Duration;

        /// <summary>
        /// carry: 이전 미끄러짐이 아직 남아 있으면 그 자리에서 이어서 움직인다(Shift+방향키를 누르고 있을 때 끊기지 않게).
        /// </summary>
        public static SlideAnim Create(BoardCell from, BoardCell to, Vector2 unit, bool isWrap, float delay, float duration, Vector3 carry)
        {
            var start = new Vector2(from.C - to.C, from.R - to.R);
            if (!isWrap)
            {
                start += new Vector2(carry.x, carry.y);
            }

            return new SlideAnim { Start = start, Unit = unit, IsWrap = isWrap, Delay = delay, Duration = duration, T = 0f };
        }

        /// <summary>지금 오프셋(x, y)과 투명도(z). null이면 제자리.</summary>
        public static Vector3 GetOffset(SlideAnim slide)
        {
            return slide == null ? new Vector3(0f, 0f, 1f) : slide.GetOffsetAt(slide.T);
        }

        public Vector3 GetOffsetAt(float time)
        {
            float u = (time - Delay) / Duration;
            if (u <= 0f)
            {
                return new Vector3(Start.x, Start.y, 1f);
            }

            if (u >= 1f)
            {
                return new Vector3(0f, 0f, 1f);
            }

            if (!IsWrap)
            {
                float eased = 1f - (1f - u) * (1f - u);
                Vector2 offset = Start * (1f - eased);
                return new Vector3(offset.x, offset.y, 1f);
            }

            if (u < 0.5f)
            {
                Vector2 outward = Start + Unit * (WRAP_OVERSHOOT * u * 2f);
                return new Vector3(outward.x, outward.y, 1f - u * 2f);
            }

            float v = (u - 0.5f) * 2f;
            Vector2 inward = -Unit * (WRAP_OVERSHOOT * (1f - v));
            return new Vector3(inward.x, inward.y, v);
        }
    }
}
