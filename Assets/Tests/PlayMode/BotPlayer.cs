using System.Collections.Generic;
using ShiftMagicSpell.Core;
using UnityEngine;

namespace ShiftMagicSpell.Tests
{
    /// <summary>
    /// 웹 simtest.js의 봇을 옮긴 것. 위험하면 먼저 피하고, 이 판 슬롯에 블록이 있으면 쏘러 가고,
    /// 아니면 목표 단어를 가장 싸게 맞추는 계획의 첫 줄 돌리기를 하러 간다.
    /// </summary>
    public class BotPlayer
    {
        private struct RotateOp
        {
            public LineAxis Axis;
            public int Index;
            public int Sign;
        }

        private sealed class Plan
        {
            public int Cost;
            public int Board;
            public List<RotateOp> Ops;
        }

        public BotAction Decide(GameSession game, bool isDodging)
        {
            PlayerController player = game.Player;
            BoardModel board = game.Boards[player.Board];
            HandController hand = game.Hand;
            var danger = new HashSet<int>();
            if (isDodging)
            {
                if (hand.Attack != null && hand.Attack.Board == player.Board && hand.Attack.Cells != null && hand.State != HandState.Idle)
                {
                    foreach (BoardCell cell in hand.Attack.Cells)
                    {
                        danger.Add(GeometryUtils.CellKey(cell.R, cell.C));
                    }
                }

                foreach (Bullet bullet in hand.Bullets)
                {
                    if (bullet.Board == player.Board)
                    {
                        foreach (BoardCell cell in board.LineCells(bullet.Axis, bullet.Index))
                        {
                            danger.Add(GeometryUtils.CellKey(cell.R, cell.C));
                        }
                    }
                }
            }

            bool isFull = isDodging && hand.Attack != null && hand.Attack.Type == AttackType.Full && hand.Attack.Board == player.Board;
            bool isInDanger = danger.Contains(GeometryUtils.CellKey(player.R, player.C));

            BotAction Step(IEnumerable<BoardCell> goals, bool isAvoidingShift)
            {
                Dictionary<int, int> dist = WalkDist(board, goals, isAvoidingShift);
                int bestDist = dist.TryGetValue(GeometryUtils.CellKey(player.R, player.C), out int here) ? here : 99;
                BotAction best = default;
                foreach (MoveDir dir in BoardModel.AllDirs)
                {
                    Vector2Int delta = GeometryUtils.ToDelta(dir);
                    int nextKey = GeometryUtils.CellKey(player.R + delta.y, player.C + delta.x);
                    if (dist.TryGetValue(nextKey, out int d) && d < bestDist && (isInDanger || isFull || !danger.Contains(nextKey)))
                    {
                        bestDist = d;
                        best = new BotAction { Kind = BotActionKind.Move, Dir = dir };
                    }
                }

                return best;
            }

            if (isFull && player.ShiftCd <= 0.3f)
            {
                return Step(new[] { board.ShiftCell }, false);
            }

            if (isInDanger && !isFull)
            {
                var safe = new List<BoardCell>();
                foreach (BoardCell cell in board.AllCells)
                {
                    if (!cell.IsShift && !danger.Contains(GeometryUtils.CellKey(cell.R, cell.C)))
                    {
                        safe.Add(cell);
                    }
                }

                return Step(safe, true);
            }

            // 공격 슬롯: 이 판에 블록이 있으면 쏘러 간다(붙어 있는 두 칸이라 연달아 쏘면 콤보).
            // 반대편 슬롯이 꽉 찼거나 쌓인 피해로 손을 끝낼 수 있으면 반대편으로 건너간다.
            BoardCell standing = player.Here;
            if (standing.IsSlot && standing.Block != null)
            {
                return new BotAction { Kind = BotActionKind.Fire };
            }

            List<BoardCell> mine = board.Slots.FindAll(slot => slot.Block != null);
            if (mine.Count > 0)
            {
                return Step(mine, true);
            }

            BoardModel other = game.Boards[1 - player.Board];
            int stored = 0;
            foreach (BoardCell slot in other.Slots)
            {
                stored += slot.Block != null ? slot.Block.Word.Length : 0;
            }

            if (other.BlockCount() > 0 && (other.EmptySlotOrNull() == null || stored >= hand.Hp))
            {
                return player.ShiftCd <= 0.3f ? Step(new[] { board.ShiftCell }, false) : default;
            }

            Plan plan = FindPlanOrNull(game);
            if (plan == null || plan.Ops.Count == 0)
            {
                return default;
            }

            if (plan.Board != player.Board)
            {
                return player.ShiftCd <= 0.3f ? Step(new[] { board.ShiftCell }, false) : default;
            }

            RotateOp op = plan.Ops[0];
            bool isOnLine = op.Axis == LineAxis.Row ? player.R == op.Index : player.C == op.Index;
            if (!standing.IsShift && !standing.IsSlot && isOnLine)
            {
                MoveDir dir = op.Axis == LineAxis.Row ? (op.Sign > 0 ? MoveDir.Right : MoveDir.Left) : (op.Sign > 0 ? MoveDir.Down : MoveDir.Up);
                return new BotAction { Kind = BotActionKind.Rotate, Dir = dir };
            }

            List<BoardCell> lineCells = board.LineCells(op.Axis, op.Index);
            lineCells.RemoveAll(cell => cell.IsShift);
            return Step(lineCells, true);
        }

        /// <summary>로직 시뮬레이션에서 행동을 바로 적용(씬 테스트는 가상 키보드로 누른다)</summary>
        public static void Apply(GameSession game, BotAction action)
        {
            switch (action.Kind)
            {
                case BotActionKind.Fire:
                    game.Player.Fire();
                    break;
                case BotActionKind.Rotate:
                    game.Player.Rotate(action.Dir);
                    break;
                case BotActionKind.Move:
                    game.Player.TryMove(action.Dir);
                    break;
                default:
                    break;
            }
        }

        // 모든 줄·시작 칸·판을 시도해 비용(돌리기 수 + 줄 바꿀 때 걷기 + 반대편 판이면 이동)이 가장 낮은 계획
        private static Plan FindPlanOrNull(GameSession game)
        {
            string word = game.Words.Current;
            if (word == null)
            {
                return null;
            }

            Plan best = null;
            foreach (BoardModel board in game.Boards)
            {
                foreach (BoardLine line in board.Lines)
                {
                    for (int start = 0; start + word.Length <= line.Cells.Count; start++)
                    {
                        List<RotateOp> ops = Solve(board, line.Cells.GetRange(start, word.Length), word);
                        if (ops == null)
                        {
                            continue;
                        }

                        int cost = ops.Count;
                        for (int i = 1; i < ops.Count; i++)
                        {
                            if (ops[i].Axis != ops[i - 1].Axis || ops[i].Index != ops[i - 1].Index)
                            {
                                cost += 2;
                            }
                        }

                        if (board.Index != game.Player.Board)
                        {
                            cost += 6;
                        }

                        if (best == null || cost < best.Cost)
                        {
                            best = new Plan { Cost = cost, Board = board.Index, Ops = ops };
                        }
                    }
                }
            }

            return best;
        }

        // 목표 자리에 단어 글자를 앞에서부터 하나씩 옮긴다. 이미 놓은 글자가 있는 줄은 돌리지 않는다.
        // 글자 하나의 위치만 보고 BFS(줄 돌리기 ±1)로 최단 경로를 찾는다.
        private static List<RotateOp> Solve(BoardModel board, List<BoardCell> targets, string word)
        {
            var letters = new Dictionary<int, char>();
            foreach (BoardCell cell in board.KeyCells())
            {
                letters[GeometryUtils.CellKey(cell.R, cell.C)] = cell.Key.Letter;
            }

            BoardLine LineOf(LineAxis axis, int index)
            {
                return board.Lines.Find(line => line.Axis == axis && line.Index == index);
            }

            void Rotate(BoardLine line, int sign)
            {
                int length = line.Cells.Count;
                var values = new char[length];
                for (int i = 0; i < length; i++)
                {
                    values[i] = letters[GeometryUtils.CellKey(line.Cells[i].R, line.Cells[i].C)];
                }

                for (int i = 0; i < length; i++)
                {
                    letters[GeometryUtils.CellKey(line.Cells[i].R, line.Cells[i].C)] = values[((i - sign) % length + length) % length];
                }
            }

            var placed = new HashSet<int>();
            var ops = new List<RotateOp>();
            for (int i = 0; i < word.Length; i++)
            {
                int targetKey = GeometryUtils.CellKey(targets[i].R, targets[i].C);
                int startKey = -1;
                foreach (KeyValuePair<int, char> pair in letters)
                {
                    if (pair.Value == word[i])
                    {
                        startKey = pair.Key;
                    }
                }

                if (startKey < 0)
                {
                    return null;
                }

                bool IsAllowed(BoardLine line)
                {
                    return !line.Cells.Exists(cell => placed.Contains(GeometryUtils.CellKey(cell.R, cell.C)));
                }

                var previous = new Dictionary<int, (int From, RotateOp Op)> { [startKey] = (-1, default) };
                var queue = new Queue<int>();
                queue.Enqueue(startKey);
                while (queue.Count > 0 && !previous.ContainsKey(targetKey))
                {
                    int current = queue.Dequeue();
                    Vector2Int position = GeometryUtils.UnKey(current);
                    foreach (BoardLine line in new[] { LineOf(LineAxis.Row, position.y), LineOf(LineAxis.Col, position.x) })
                    {
                        if (line == null || !IsAllowed(line))
                        {
                            continue;
                        }

                        int j = line.Cells.FindIndex(cell => cell.Is(position.y, position.x));
                        int length = line.Cells.Count;
                        foreach (int sign in new[] { 1, -1 })
                        {
                            BoardCell next = line.Cells[((j + sign) % length + length) % length];
                            int nextKey = GeometryUtils.CellKey(next.R, next.C);
                            if (!previous.ContainsKey(nextKey))
                            {
                                previous[nextKey] = (current, new RotateOp { Axis = line.Axis, Index = line.Index, Sign = sign });
                                queue.Enqueue(nextKey);
                            }
                        }
                    }
                }

                if (!previous.ContainsKey(targetKey))
                {
                    return null;
                }

                var path = new List<RotateOp>();
                for (int key = targetKey; previous[key].From >= 0; key = previous[key].From)
                {
                    path.Insert(0, previous[key].Op);
                }

                foreach (RotateOp step in path)
                {
                    Rotate(LineOf(step.Axis, step.Index), step.Sign);
                    ops.Add(step);
                }

                placed.Add(targetKey);
            }

            return ops;
        }

        // goal 칸들까지 걷는 거리(SHIFT 칸을 피할 수도 있다)
        private static Dictionary<int, int> WalkDist(BoardModel board, IEnumerable<BoardCell> goals, bool isAvoidingShift)
        {
            var dist = new Dictionary<int, int>();
            var queue = new Queue<BoardCell>();
            foreach (BoardCell goal in goals)
            {
                dist[GeometryUtils.CellKey(goal.R, goal.C)] = 0;
                queue.Enqueue(goal);
            }

            while (queue.Count > 0)
            {
                BoardCell current = queue.Dequeue();
                int currentDist = dist[GeometryUtils.CellKey(current.R, current.C)];
                foreach (MoveDir dir in BoardModel.AllDirs)
                {
                    Vector2Int delta = GeometryUtils.ToDelta(dir);
                    BoardCell next = board.GetCellOrNull(current.R + delta.y, current.C + delta.x);
                    if (next == null || dist.ContainsKey(GeometryUtils.CellKey(next.R, next.C)) || (isAvoidingShift && next.IsShift))
                    {
                        continue;
                    }

                    dist[GeometryUtils.CellKey(next.R, next.C)] = currentDist + 1;
                    queue.Enqueue(next);
                }
            }

            return dist;
        }
    }
}
