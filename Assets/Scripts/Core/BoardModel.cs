using System.Collections.Generic;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>키보드 한 판: 마스크 + 칸마다 키 하나 + 공격 슬롯 2칸.</summary>
    public class BoardModel
    {
        public readonly int Index;
        public int Version { get; private set; }
        public BoardCell ShiftCell { get; private set; }
        public List<BoardCell> Slots { get; } = new List<BoardCell>();
        public List<BoardLine> Lines { get; } = new List<BoardLine>();

        private readonly string[] _mask;
        private readonly string[] _layout;
        private readonly Vector2Int[] _slotDefs;
        private readonly BoardCell[,] _cells = new BoardCell[GeometryUtils.ROWS, GeometryUtils.COLS];
        private readonly List<BoardCell> _allCells = new List<BoardCell>();

        public BoardModel(int index, BoardLayoutData layoutData)
        {
            Index = index;
            _mask = layoutData.GetMask(index);
            _layout = layoutData.GetLayout(index);
            _slotDefs = layoutData.GetSlots(index);
            Reset();
        }

        /// <summary>시작 배치로 되돌린다. 슬롯의 블록도 비운다.</summary>
        public void Reset()
        {
            _allCells.Clear();
            Slots.Clear();
            Lines.Clear();
            Version = 0;
            int keyId = 0;
            for (int r = 0; r < GeometryUtils.ROWS; r++)
            {
                for (int c = 0; c < GeometryUtils.COLS; c++)
                {
                    char maskChar = _mask[r][c];
                    if (maskChar == '.')
                    {
                        _cells[r, c] = null;
                        continue;
                    }

                    bool isShift = maskChar == 'S';
                    var cell = new BoardCell(r, c, isShift, false);
                    if (isShift)
                    {
                        ShiftCell = cell;
                    }
                    else
                    {
                        char layoutChar = _layout[r][c];
                        cell.Key = new KeyTile(keyId++, layoutChar >= 'A' && layoutChar <= 'Z' ? layoutChar : '\0');
                    }

                    _cells[r, c] = cell;
                    _allCells.Add(cell);
                }
            }

            foreach (Vector2Int slotDef in _slotDefs)
            {
                var slot = new BoardCell(slotDef.y, slotDef.x, false, true);
                Slots.Add(slot);
                _allCells.Add(slot);
            }

            BuildLines();
        }

        public IReadOnlyList<BoardCell> AllCells => _allCells;

        public BoardCell GetCellOrNull(int r, int c)
        {
            if (r >= 0 && r < GeometryUtils.ROWS && c >= 0 && c < GeometryUtils.COLS)
            {
                return _cells[r, c];
            }

            foreach (BoardCell slot in Slots)
            {
                if (slot.Is(r, c))
                {
                    return slot;
                }
            }

            return null;
        }

        public char LetterAt(int r, int c)
        {
            BoardCell cell = GetCellOrNull(r, c);
            return cell?.Key != null ? cell.Key.Letter : '\0';
        }

        public IEnumerable<BoardCell> KeyCells()
        {
            foreach (BoardCell cell in _allCells)
            {
                if (cell.Key != null)
                {
                    yield return cell;
                }
            }
        }

        /// <summary>그 행·열의 칸(SHIFT 칸 포함, 슬롯 제외)</summary>
        public List<BoardCell> LineCells(LineAxis axis, int index)
        {
            var result = new List<BoardCell>();
            int length = axis == LineAxis.Row ? GeometryUtils.COLS : GeometryUtils.ROWS;
            for (int i = 0; i < length; i++)
            {
                BoardCell cell = axis == LineAxis.Row ? _cells[index, i] : _cells[i, index];
                if (cell != null)
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        public List<BoardCell> Area(int r0, int c0, int height, int width)
        {
            var result = new List<BoardCell>();
            for (int r = r0; r < r0 + height; r++)
            {
                for (int c = c0; c < c0 + width; c++)
                {
                    BoardCell cell = GetCellOrNull(r, c);
                    if (cell != null)
                    {
                        result.Add(cell);
                    }
                }
            }

            return result;
        }

        /// <summary>(r, c)에서 걸어서 가는 걸음 수. 칸 번호(GeometryUtils.CellKey) → 걸음 수.</summary>
        public Dictionary<int, int> DistFrom(int r, int c)
        {
            var dist = new Dictionary<int, int> { [GeometryUtils.CellKey(r, c)] = 0 };
            var queue = new Queue<BoardCell>();
            queue.Enqueue(GetCellOrNull(r, c) ?? new BoardCell(r, c, false, false));
            while (queue.Count > 0)
            {
                BoardCell current = queue.Dequeue();
                int currentDist = dist[GeometryUtils.CellKey(current.R, current.C)];
                foreach (MoveDir dir in AllDirs)
                {
                    Vector2Int delta = GeometryUtils.ToDelta(dir);
                    BoardCell next = GetCellOrNull(current.R + delta.y, current.C + delta.x);
                    if (next != null && !dist.ContainsKey(GeometryUtils.CellKey(next.R, next.C)))
                    {
                        dist[GeometryUtils.CellKey(next.R, next.C)] = currentDist + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            return dist;
        }

        public static readonly MoveDir[] AllDirs = { MoveDir.Up, MoveDir.Down, MoveDir.Left, MoveDir.Right };

        public void Update(float dt)
        {
            foreach (BoardCell cell in _allCells)
            {
                KeyTile key = cell.Key;
                if (key?.Slide != null)
                {
                    key.Slide.T += dt;
                    if (key.Slide.IsDone)
                    {
                        key.Slide = null;
                    }
                }

                if (cell.Block != null)
                {
                    cell.Block.Age += dt;
                    cell.Block.HitT = Mathf.Max(0f, cell.Block.HitT - dt);
                }
            }
        }

        private void BuildLines()
        {
            for (int r = 0; r < GeometryUtils.ROWS; r++)
            {
                AddLine(LineAxis.Row, r);
            }

            for (int c = 0; c < GeometryUtils.COLS; c++)
            {
                AddLine(LineAxis.Col, c);
            }
        }

        private void AddLine(LineAxis axis, int index)
        {
            var line = new BoardLine { Axis = axis, Index = index };
            foreach (BoardCell cell in LineCells(axis, index))
            {
                if (!cell.IsShift)
                {
                    line.Cells.Add(cell);
                }
            }

            if (line.Cells.Count >= 2)
            {
                Lines.Add(line);
            }
        }
    }
}
