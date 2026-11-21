using System.Collections.Generic;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 손 AI: 상황 점수 → 공정성 검사(BFS: 걸어서 피할 수 있는가) → 상위 후보 가중치 랜덤. 같은 공격 연속 금지.
    /// 손은 피해 공격 위주. 줄 섞기만 판을 바꾸고, 쿨다운이 길어서 가끔만 나온다.
    /// </summary>
    public class HandBrain
    {
        private const int TOP_CANDIDATES = 4;

        private readonly GameSession _game;
        private readonly Dictionary<AttackType, float> _cooldowns = new Dictionary<AttackType, float>();
        private AttackType? _lastType;

        public HandBrain(GameSession game)
        {
            _game = game;
        }

        /// <summary>마지막으로 고른 이유(디버그 표시용)</summary>
        public string LastChoiceText { get; private set; } = string.Empty;

        public void Reset()
        {
            _lastType = null;
            _cooldowns.Clear();
            LastChoiceText = string.Empty;
        }

        public void Update(float dt)
        {
            var keys = new List<AttackType>(_cooldowns.Keys);
            foreach (AttackType type in keys)
            {
                _cooldowns[type] = Mathf.Max(0f, _cooldowns[type] - dt);
            }
        }

        public AttackDef Def(AttackType type)
        {
            return _game.Data.GetAttackOrNull(type.ToId());
        }

        public float WarnTime(AttackType type)
        {
            return _game.Config.WarnTime * Def(type).WarnMul * _game.CurrentPhase.WarnMul;
        }

        /// <summary>주어진 시간 안에 걸어서 닿을 수 있는 안전한 칸(또는 쓸 수 있는 SHIFT 칸)이 하나라도 있는가</summary>
        public bool CanEscape(int boardIndex, List<BoardCell> cells, float time, bool isShiftSafe)
        {
            PlayerController player = _game.Player;
            if (player.Board != boardIndex)
            {
                return true;
            }

            BoardModel board = _game.Boards[boardIndex];
            var danger = new HashSet<int>();
            foreach (BoardCell cell in cells)
            {
                if (!(cell.IsShift && isShiftSafe))
                {
                    danger.Add(GeometryUtils.CellKey(cell.R, cell.C));
                }
            }

            if (!danger.Contains(GeometryUtils.CellKey(player.R, player.C)))
            {
                return true;
            }

            ConfigValues config = _game.Config;
            int steps = Mathf.Max(1, Mathf.FloorToInt((time - config.Reaction) / config.MoveRepeat));
            foreach (KeyValuePair<int, int> pair in board.DistFrom(player.R, player.C))
            {
                int dist = pair.Value;
                if (dist == 0 || dist > steps)
                {
                    continue;
                }

                Vector2Int position = GeometryUtils.UnKey(pair.Key);
                if (board.GetCellOrNull(position.y, position.x).IsShift && player.ShiftCd <= dist * config.MoveRepeat + config.Reaction)
                {
                    return true;
                }

                if (!danger.Contains(pair.Key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>from→to 직선(조준 돌진)이 지나가는 칸</summary>
        public List<BoardCell> AimCells(BoardModel board, Vector2 from, Vector2 to)
        {
            var result = new List<BoardCell>();
            foreach (BoardCell cell in board.AllCells)
            {
                if (GeometryUtils.DistToSegment(GeometryUtils.CellCenter(board.Index, cell.R, cell.C), from, to) < 0.62f)
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        public AttackPlan ChooseOrNull()
        {
            var context = new ChoiceContext(_game);
            var candidates = new List<AttackPlan>();
            AddLineCandidates(context, candidates);
            AddBoardCandidates(context, candidates);
            AddSlotCandidates(context, candidates);

            var usable = candidates.FindAll(plan => !plan.IsCooling && !plan.IsRepeat && plan.IsFair && plan.Score > 0f);
            usable.Sort((a, b) => b.Score.CompareTo(a.Score));
            if (usable.Count > TOP_CANDIDATES)
            {
                usable.RemoveRange(TOP_CANDIDATES, usable.Count - TOP_CANDIDATES);
            }

            AttackPlan chosen = null;
            if (usable.Count > 0)
            {
                float total = 0f;
                foreach (AttackPlan plan in usable)
                {
                    total += plan.Score * plan.Score;
                }

                float pick = _game.Random.Value() * total;
                foreach (AttackPlan plan in usable)
                {
                    pick -= plan.Score * plan.Score;
                    if (pick <= 0f)
                    {
                        chosen = plan;
                        break;
                    }
                }

                chosen ??= usable[0];
            }

            if (chosen != null)
            {
                _lastType = chosen.Type;
                _cooldowns[chosen.Type] = Def(chosen.Type).Cooldown;
                LastChoiceText = $"{chosen.Type} {chosen.Score:0.0} ({chosen.Why})";
            }

            return chosen;
        }

        // 플레이어 주변 정보: 이웃 칸 수(구석), SHIFT 칸까지 거리, 멈춘 시간, 이동 방향, 2글자 이상 맞춘 줄
        private sealed class ChoiceContext
        {
            public readonly PlayerController Player;
            public readonly BoardModel Board;
            public readonly bool IsCorner;
            public readonly int ShiftDistance;
            public readonly bool IsMoving;
            public readonly AlignResult Progress;
            public readonly float DwellBonus;
            public readonly Vector2 PlayerCenter;

            public ChoiceContext(GameSession game)
            {
                Player = game.Player;
                Board = game.Boards[Player.Board];
                int neighbors = 0;
                foreach (MoveDir dir in BoardModel.AllDirs)
                {
                    Vector2Int delta = GeometryUtils.ToDelta(dir);
                    if (Board.GetCellOrNull(Player.R + delta.y, Player.C + delta.x) != null)
                    {
                        neighbors++;
                    }
                }

                IsCorner = neighbors <= 2;
                Dictionary<int, int> dist = Board.DistFrom(Player.R, Player.C);
                ShiftDistance = dist.TryGetValue(GeometryUtils.CellKey(Board.ShiftCell.R, Board.ShiftCell.C), out int shiftDist) ? shiftDist : 99;
                IsMoving = Player.SinceMove < 0.35f;
                AlignResult best = game.Words.BestOrNull();
                Progress = best != null && best.Board == Player.Board && best.Count >= 2 ? best : null;
                DwellBonus = Mathf.Min(Player.Dwell, 3f) * 1.5f;
                PlayerCenter = GeometryUtils.CellCenter(Board.Index, Player.R, Player.C);
            }
        }

        private sealed class LineTarget
        {
            public LineAxis Axis;
            public int Index;
            public float Weight;
            public bool IsProgressLine;
            public string Tags;
        }

        private AttackPlan Push(AttackType type, int board, List<BoardCell> cells, Vector2 anchor, float score, string why)
        {
            var plan = new AttackPlan
            {
                Type = type,
                Name = Def(type).Name,
                Board = board,
                Cells = cells,
                Anchor = anchor,
                Warn = WarnTime(type),
                Why = why,
            };
            plan.Travel = PerimeterUtils.FindPath(_game.Hand.Pos, board, anchor).Length / _game.Hand.Speed;
            plan.Score = Mathf.Round((score - plan.Travel * 1.5f) * 10f) / 10f;
            plan.IsCooling = _cooldowns.TryGetValue(type, out float cooldown) && cooldown > 0f;
            plan.IsRepeat = _lastType == type;
            plan.IsFair = CanEscape(board, cells, plan.Warn, type == AttackType.Flick);   // 줄 섞기는 SHIFT 칸을 건드리지 않는다
            return plan;
        }

        private bool IsAllowed(AttackType type)
        {
            return _game.CurrentPhase.Attacks.Contains(type.ToId());
        }

        private float Base(AttackType type)
        {
            return Def(type).Base;
        }

        // ── 줄 공격: 플레이어 행·열, 맞춘 줄을 가로지르는 줄, SHIFT 칸 근처 행 ──
        private void AddLineCandidates(ChoiceContext context, List<AttackPlan> candidates)
        {
            PlayerController player = context.Player;
            BoardModel board = context.Board;
            var lines = new List<LineTarget>();
            void AddLine(LineAxis axis, int index, float weight, string tag, bool isProgressLine)
            {
                LineTarget existing = lines.Find(line => line.Axis == axis && line.Index == index);
                if (existing != null)
                {
                    existing.Weight = Mathf.Max(existing.Weight, weight) + 0.15f;
                    existing.Tags += ", " + tag;
                    existing.IsProgressLine |= isProgressLine;
                    return;
                }

                lines.Add(new LineTarget { Axis = axis, Index = index, Weight = weight, Tags = tag, IsProgressLine = isProgressLine });
            }

            AddLine(LineAxis.Row, player.R, 1f, "player row", false);
            AddLine(LineAxis.Col, player.C, 1f, "player col", false);
            if (context.Progress != null && _game.CurrentPhase.IsTargetingProgressLine)
            {
                // 맞춰 둔 줄을 가로지르는 줄을 섞으면 맞춘 글자 하나가 빠진다
                string word = _game.Words.Current;
                var hits = new List<BoardCell>();
                for (int i = 0; i < context.Progress.Cells.Count; i++)
                {
                    if (board.LetterAt(context.Progress.Cells[i].R, context.Progress.Cells[i].C) == word[i])
                    {
                        hits.Add(context.Progress.Cells[i]);
                    }
                }

                BoardCell middle = hits[hits.Count / 2];
                if (context.Progress.Axis == LineAxis.Row)
                {
                    AddLine(LineAxis.Col, middle.C, 0.9f, "cross progress", true);
                }
                else
                {
                    AddLine(LineAxis.Row, middle.R, 0.9f, "cross progress", true);
                }
            }

            if (context.ShiftDistance <= 3 && player.ShiftCd < 1f)
            {
                AddLine(LineAxis.Row, board.ShiftCell.R, 1.1f, "near shift", false);
            }

            foreach (LineTarget line in lines)
            {
                List<BoardCell> cells = board.LineCells(line.Axis, line.Index);
                int movable = cells.FindAll(cell => !cell.IsShift).Count;
                bool isOnLine = line.Axis == LineAxis.Row ? line.Index == player.R : line.Index == player.C;
                bool isAlong = context.IsMoving && ((line.Axis == LineAxis.Row && player.LastDir.y != 0) || (line.Axis == LineAxis.Col && player.LastDir.x != 0));
                Vector2 anchor0 = PerimeterUtils.LineAnchor(board.Index, line.Axis, line.Index, 0);
                Vector2 anchor1 = PerimeterUtils.LineAnchor(board.Index, line.Axis, line.Index, 1);
                int end = TravelTime(anchor0, board.Index) <= TravelTime(anchor1, board.Index) ? 0 : 1;
                Vector2 anchor = end == 0 ? anchor0 : anchor1;
                float dwellBonus = isOnLine ? context.DwellBonus : 0f;
                string why = line.Tags;

                if (IsAllowed(AttackType.Flick) && movable >= 2)
                {
                    AttackDef def = Def(AttackType.Flick);
                    int shiftCount = _game.Random.RangeInclusive(def.ShiftMin, Mathf.Min(def.ShiftMax, movable - 1));
                    float score = Base(AttackType.Flick) * line.Weight + (isOnLine ? 1f : 0f) + (line.IsProgressLine ? 2f + context.Progress.Count : 0f);
                    AttackPlan plan = Push(AttackType.Flick, board.Index, cells, anchor, score, why);
                    SetLine(plan, line, end);
                    plan.ShiftCount = shiftCount;
                    plan.Sign = end == 0 ? 1 : -1;
                    candidates.Add(plan);
                }

                if (IsAllowed(AttackType.Summon))
                {
                    float score = Base(AttackType.Summon) * line.Weight + (isOnLine ? 2f : 0f) + (isAlong && isOnLine ? 2f : 0f) + dwellBonus;
                    AttackPlan plan = Push(AttackType.Summon, board.Index, cells, anchor, score, why);
                    SetLine(plan, line, end);
                    candidates.Add(plan);
                }

                if (IsAllowed(AttackType.Charge))
                {
                    float score = Base(AttackType.Charge) * line.Weight + (isOnLine ? 2f : 0f) + (context.IsCorner ? 2f : 0f) + dwellBonus;
                    AttackPlan plan = Push(AttackType.Charge, board.Index, cells, anchor, score, why);
                    SetLine(plan, line, end);
                    candidates.Add(plan);
                }
            }
        }

        private static void SetLine(AttackPlan plan, LineTarget line, int end)
        {
            plan.Axis = line.Axis;
            plan.Index = line.Index;
            plan.End = end;
        }

        private float TravelTime(Vector2 anchor, int board)
        {
            return PerimeterUtils.FindPath(_game.Hand.Pos, board, anchor).Length / _game.Hand.Speed;
        }

        // ── 판 위로 들어오는 공격(조준·잠금 시점에 공정성을 다시 본다) ──
        private void AddBoardCandidates(ChoiceContext context, List<AttackPlan> candidates)
        {
            PlayerController player = context.Player;
            BoardModel board = context.Board;
            int phase = _game.Phase;
            Vector2 handPos = _game.Hand.Pos;
            Vector2 entry = PerimeterUtils.Locate(handPos) == board.Index ? handPos : PerimeterUtils.Nearest(board.Index, handPos);
            Vector2 nearPlayer = PerimeterUtils.Nearest(board.Index, context.PlayerCenter);

            if (IsAllowed(AttackType.Aim))
            {
                AttackDef def = Def(AttackType.Aim);
                int chain = _game.Random.RangeInclusive(AttackDef.GetForPhaseOrDefault(def.ChainMin, phase, 1), AttackDef.GetForPhaseOrDefault(def.ChainMax, phase, 1));
                Vector2 exit = PerimeterUtils.RayExit(board.Index, entry, context.PlayerCenter);
                float score = Base(AttackType.Aim) + (context.IsMoving ? 2f : 0f) + context.DwellBonus + (chain - 1) * 1.5f;
                AttackPlan plan = Push(AttackType.Aim, board.Index, AimCells(board, entry, exit), entry, score, "aim at player");
                plan.Chain = chain;
                plan.IsRecheck = true;
                candidates.Add(plan);
            }

            if (IsAllowed(AttackType.Track))
            {
                float score = Base(AttackType.Track) + (context.IsCorner ? 5f : 0f);
                AttackPlan plan = Push(AttackType.Track, board.Index, new List<BoardCell>(), nearPlayer, score, context.IsCorner ? "cornered" : "track player");
                plan.IsRecheck = true;
                candidates.Add(plan);
            }

            if (IsAllowed(AttackType.Click))
            {
                AttackDef def = Def(AttackType.Click);
                bool isDouble = _game.Random.Value() < AttackDef.GetForPhaseOrDefault(def.DoubleChance, phase, 0f);
                float stay = player.Dwell > 0.8f ? Mathf.Min(player.Dwell, 3f) * 3f : 0f;
                float score = Base(AttackType.Click) + stay + (isDouble ? 1f : 0f);
                AttackPlan plan = Push(AttackType.Click, board.Index, new List<BoardCell> { player.Here }, nearPlayer, score, stay > 0f ? "player standing still" : "player cell");
                plan.IsDouble = isDouble;
                plan.IsRecheck = true;
                candidates.Add(plan);
            }

            if (IsAllowed(AttackType.Drag))
            {
                // 플레이어를 포함하는 사각형 하나
                var options = new List<RectInt>();
                foreach (Vector2Int shape in new[] { new Vector2Int(3, 2), new Vector2Int(2, 3), new Vector2Int(2, 2) })
                {
                    int width = shape.x;
                    int height = shape.y;
                    for (int r0 = player.R - height + 1; r0 <= player.R; r0++)
                    {
                        for (int c0 = player.C - width + 1; c0 <= player.C; c0++)
                        {
                            if (board.Area(r0, c0, height, width).Count >= height * width - 1)
                            {
                                options.Add(new RectInt(c0, r0, width, height));
                            }
                        }
                    }
                }

                if (options.Count > 0)
                {
                    RectInt selection = options[_game.Random.RangeInclusive(0, options.Count - 1)];
                    Vector2 corner = GeometryUtils.CellCenter(board.Index, selection.y, selection.x);
                    float score = Base(AttackType.Drag) + (context.IsCorner ? 2f : 0f);
                    AttackPlan plan = Push(AttackType.Drag, board.Index, board.Area(selection.y, selection.x, selection.height, selection.width), PerimeterUtils.Nearest(board.Index, corner), score, "select around player");
                    plan.RectSelection = selection;
                    candidates.Add(plan);
                }
            }

            if (IsAllowed(AttackType.Full))
            {
                float boardX = GeometryUtils.BoardOffsetX(board.Index);
                var top = new Vector2(boardX + GeometryUtils.COLS * 0.5f, -GeometryUtils.MARGIN);
                float score = Base(AttackType.Full) + Mathf.Min(player.BoardTime, 18f) * 0.4f;
                AttackPlan plan = Push(AttackType.Full, board.Index, new List<BoardCell>(board.AllCells), top, score, "stayed on this board");
                plan.Center = new Vector2(boardX + GeometryUtils.COLS * 0.5f, GeometryUtils.ROWS * 0.5f);
                candidates.Add(plan);
            }
        }

        // ── 슬롯 깨기: 공격 블록이 든 슬롯을 친다(어느 판이든, 플레이어가 없어도). 블록이 많이 쌓인 판일수록 가산.
        private void AddSlotCandidates(ChoiceContext context, List<AttackPlan> candidates)
        {
            if (!IsAllowed(AttackType.Slot))
            {
                return;
            }

            foreach (BoardModel board in _game.Boards)
            {
                foreach (BoardCell slot in board.Slots)
                {
                    if (slot.Block == null)
                    {
                        continue;
                    }

                    Vector2 center = GeometryUtils.CellCenter(board.Index, slot.R, slot.C);
                    float score = Base(AttackType.Slot) + board.BlockCount() * 3f + (slot.Block.Hp < _game.Config.BlockHp ? 2f : 0f);
                    AttackPlan plan = Push(AttackType.Slot, board.Index, new List<BoardCell> { slot }, PerimeterUtils.Nearest(board.Index, center), score, $"{board.BlockCount()} blocks");
                    plan.Slot = slot;
                    plan.Center = center;
                    candidates.Add(plan);
                }
            }
        }
    }
}
