using System.Collections.Generic;
using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 플레이어: 칸 이동(누르고 있으면 반복), SHIFT 칸 순간이동, 피격.
    /// 입력은 Press/Release로 받는다(키보드 읽기는 Input 쪽).
    /// </summary>
    public class PlayerController
    {
        private readonly GameSession _game;
        private readonly List<MoveDir> _heldDirs = new List<MoveDir>();
        private float _repeatT;

        public int Board;
        public int R;
        public int C;
        public int Hp;
        public float Invuln;
        public float ShiftCd;
        public float Dwell;
        public float BoardTime;
        public float SinceMove;
        public Vector2Int LastDir;   // 웹과 같이 (dr, dc): x = 행 변화, y = 열 변화
        public SlideAnim Slide;
        public bool IsShiftHeld;

        public PlayerController(GameSession game)
        {
            _game = game;
            Reset();
        }

        public void Reset()
        {
            Board = 0;
            R = 2;
            C = 2;
            Hp = _game.Config.PlayerHp;
            Invuln = 0f;
            ShiftCd = 0f;
            Dwell = 0f;
            BoardTime = 0f;
            LastDir = Vector2Int.zero;
            SinceMove = 99f;
            _repeatT = 0f;
            Slide = null;
        }

        public BoardCell Here => _game.Boards[Board].GetCellOrNull(R, C);

        public void Press(MoveDir dir)
        {
            if (_heldDirs.Contains(dir))
            {
                return;
            }

            _heldDirs.Add(dir);
            Act(dir);
        }

        public void Release(MoveDir dir)
        {
            _heldDirs.Remove(dir);
        }

        public void ReleaseAll()
        {
            _heldDirs.Clear();
            IsShiftHeld = false;
        }

        public void Update(float dt)
        {
            Invuln = Mathf.Max(0f, Invuln - dt);
            ShiftCd = Mathf.Max(0f, ShiftCd - dt);
            Dwell += dt;
            SinceMove += dt;
            BoardTime += dt;
            if (Slide != null)
            {
                Slide.T += dt;
                if (Slide.IsDone)
                {
                    Slide = null;
                }
            }

            if (_heldDirs.Count > 0)
            {
                _repeatT -= dt;
                if (_repeatT <= 0f)
                {
                    Act(_heldDirs[_heldDirs.Count - 1]);
                }
            }
        }

        public bool TryMove(MoveDir dir)
        {
            if (_game.State != GameState.Play)
            {
                return false;
            }

            Vector2Int delta = GeometryUtils.ToDelta(dir);
            if (_game.Boards[Board].GetCellOrNull(R + delta.y, C + delta.x) == null)
            {
                return false;
            }

            R += delta.y;
            C += delta.x;
            LastDir = new Vector2Int(delta.y, delta.x);
            SinceMove = 0f;
            Dwell = 0f;
            Slide = null;
            EnterCell();
            return true;
        }

        public bool Hurt(string source)
        {
            if (_game.IsInvincible || Invuln > 0f || _game.State != GameState.Play)
            {
                return false;
            }

            Hp--;
            Invuln = _game.Config.HitInvuln;
            _game.OnPlayerHurt(source);
            return true;
        }

        // Shift를 누른 채 방향키 = 서 있는 줄 돌리기(←→ 행, ↑↓ 열). Shift 없이 = 한 칸 이동. 누르고 있으면 반복.
        private void Act(MoveDir dir)
        {
            TryMove(dir);
            _repeatT = _game.Config.MoveRepeat;
        }

        // SHIFT 칸을 밟으면 반대편 판의 SHIFT 칸으로 순간이동(쿨다운 중이면 그냥 서 있는다)
        private void EnterCell()
        {
            BoardCell here = Here;
            if (!here.IsShift || ShiftCd > 0f)
            {
                return;
            }

            BoardModel other = _game.Boards[1 - Board];
            BoardCell dest = other.ShiftCell;
            _game.Fx.Add(FxEvent.AtCell(FxKind.Warp, Board, R, C, 0.4f));
            Board = other.Index;
            R = dest.R;
            C = dest.C;
            ShiftCd = _game.Config.ShiftCooldown;
            Invuln = Mathf.Max(Invuln, _game.Config.ShiftInvuln);
            BoardTime = 0f;
            Dwell = 0f;
            _game.Stats.Teleports++;
            _game.Fx.Add(FxEvent.AtCell(FxKind.Warp, Board, R, C, 0.4f));
        }
    }
}
