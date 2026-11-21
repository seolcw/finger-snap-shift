using System.Collections.Generic;
using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 손이 고른(또는 고려한) 공격 하나. 공격 종류마다 쓰는 항목이 다르다(웹 HandBrain 후보 객체).
    /// 위치는 로직 좌표(칸 단위).
    /// </summary>
    public class AttackPlan
    {
        public AttackType Type;
        public string Name;
        public int Board;
        public List<BoardCell> Cells = new List<BoardCell>();
        public Vector2 Anchor;
        public float Warn;
        public float Travel;
        public float Score;
        public bool IsCooling;
        public bool IsRepeat;
        public bool IsFair;
        public string Why;

        // 줄 공격(줄 섞기, 소환, 둘레 돌진)
        public LineAxis Axis;
        public int Index;
        public int End;
        public int ShiftCount;
        public int Sign;

        // 조준 돌진: 남은 연속 횟수, 예고 시작 때 플레이어가 반대편에 있으면 취소(Recheck)
        public int Chain = 1;
        public bool IsRecheck;
        public bool IsCounted;

        // 클릭: 더블클릭이면 한 번 더
        public bool IsDouble;
        public bool IsSecond;

        // 드래그 선택 사각형
        public RectInt RectSelection;
        public float DragT;

        // 슬롯 깨기 대상
        public BoardCell Slot;

        // 추적 내려찍기: 따라다니는 중(IsLocked = false) → 잠금
        public bool IsLocked;

        public Vector2 Center;
        public Vector2 From;
        public Vector2 To;

        /// <summary>맞았을 때 원인 이름(사망 화면에 보임)</summary>
        public string HitName => IsDouble && IsSecond ? "DOUBLE CLICK" : Type.ToDisplayName();

        /// <summary>줄 섞기 예고 칸에 붙이는 미는 양(→1 …)</summary>
        public string Badge
        {
            get
            {
                if (Type != AttackType.Flick)
                {
                    return string.Empty;
                }

                string arrow = Axis == LineAxis.Row ? (Sign > 0 ? "→" : "←") : (Sign > 0 ? "↓" : "↑");
                return arrow + ShiftCount;
            }
        }
    }
}
