using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 손(거대한 마우스 커서). 위치는 로직 좌표(칸 단위). Anim은 Hand.aseprite 태그 이름(Idle, FlickCharge, FlickRelease, Hurt…)
    /// 또는 멈춤(Stun)이다.
    /// </summary>
    public class HandController
    {
        public const string ANIM_IDLE = "Idle";
        public const string ANIM_STUN = "Stun";
        public const string ANIM_HURT = "Hurt";

        private readonly GameSession _game;

        public int Hp;
        public Vector2 Pos;
        public HandState State;
        public float T;
        public string Anim = ANIM_IDLE;
        public float AnimT;

        public HandController(GameSession game)
        {
            _game = game;
        }

        public int MaxHp => _game.CurrentPhase.HandHp;

        public void Reset()
        {
            Hp = MaxHp;
            Pos = new Vector2(GeometryUtils.BoardOffsetX(0) + GeometryUtils.COLS * 0.5f, -GeometryUtils.MARGIN);
            State = HandState.Idle;
            T = 1.6f;
            SetAnim(ANIM_IDLE);
        }

        public void SetAnim(string animName)
        {
            Anim = animName;
            AnimT = 0f;
        }

        public void Stun(float seconds)
        {
            State = HandState.Stun;
            T = seconds;
            SetAnim(ANIM_STUN);
        }

        public void Update(float dt)
        {
            AnimT += dt;
            if (State == HandState.Stun)
            {
                T -= dt;
                if (T <= 0f)
                {
                    State = HandState.Idle;
                    T = 0.3f;
                    SetAnim(ANIM_IDLE);
                }
            }
        }
    }
}
