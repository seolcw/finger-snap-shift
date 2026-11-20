using System;
using System.Collections.Generic;

namespace ShiftMagicSpell.Core
{
    /// <summary>웹 layoutRules.js: 단어 정렬 찾기, 단어 목록 검사, 시작 배치 검사.</summary>
    public static class LayoutUtils
    {
        // 시작 배치에서 한 단어가 한 줄에 이만큼 넘게 맞춰져 있으면 안 된다(시작하자마자 거의 완성되는 것 방지)
        public const int MAX_START_ALIGN = 2;
        private const int MAX_SAME_POSITION = 4;

        /// <summary>한 줄 안의 연속한 칸에서 제자리에 맞는 글자 수가 가장 많은 자리</summary>
        public static AlignResult BestAlign(IReadOnlyList<BoardLine> lines, Func<int, int, char> letterAt, string word)
        {
            AlignResult best = null;
            foreach (BoardLine line in lines)
            {
                for (int start = 0; start + word.Length <= line.Cells.Count; start++)
                {
                    int count = 0;
                    for (int i = 0; i < word.Length; i++)
                    {
                        BoardCell cell = line.Cells[start + i];
                        if (letterAt(cell.R, cell.C) == word[i])
                        {
                            count++;
                        }
                    }

                    if (best == null || count > best.Count)
                    {
                        best = new AlignResult
                        {
                            Axis = line.Axis,
                            Index = line.Index,
                            Start = start,
                            Count = count,
                            Cells = line.Cells.GetRange(start, word.Length),
                        };
                    }
                }
            }

            return best ?? new AlignResult();
        }

        public static int LongestLine(IReadOnlyList<BoardLine> lines)
        {
            int longest = 0;
            foreach (BoardLine line in lines)
            {
                longest = Math.Max(longest, line.Cells.Count);
            }

            return longest;
        }

        /// <summary>판마다 글자가 하나씩뿐이라 한 단어에 같은 글자가 두 번 나오면 한 줄로 맞출 수 없다. 오류 목록(비면 통과).</summary>
        public static List<string> CheckWordList(IEnumerable<string> words, int maxLength)
        {
            var errors = new List<string>();
            foreach (string word in words)
            {
                var seen = new HashSet<char>();
                foreach (char ch in word)
                {
                    if (ch < 'A' || ch > 'Z')
                    {
                        errors.Add($"{word}: not A-Z");
                    }

                    if (!seen.Add(ch))
                    {
                        errors.Add($"{word}: {ch} appears twice");
                    }
                }

                if (word.Length > maxLength)
                {
                    errors.Add($"{word}: {word.Length} letters (longest line {maxLength})");
                }
            }

            return errors;
        }

        /// <summary>
        /// 시작 배치 검사(Reset 직후의 판): 글자 26개 + 빈 키 2개, 어떤 단어도 3글자 이상 미리 맞춰져 있지 않음,
        /// 좌우 배치가 충분히 다름(같은 칸·거울 칸 글자가 4개 이하). 오류 목록(비면 통과).
        /// </summary>
        public static List<string> CheckStartLayout(BoardModel board, BoardModel other, IEnumerable<string> words, out int worstAlign)
        {
            var errors = new List<string>();
            var positions = new Dictionary<char, BoardCell>();
            int blankCount = 0;
            foreach (BoardCell cell in board.KeyCells())
            {
                if (cell.Key.HasLetter)
                {
                    positions[cell.Key.Letter] = cell;
                }
                else
                {
                    blankCount++;
                }
            }

            if (positions.Count != 26)
            {
                errors.Add($"letters {positions.Count} (need 26)");
            }

            if (blankCount != 2)
            {
                errors.Add($"blank keys {blankCount} (need 2)");
            }

            worstAlign = 0;
            foreach (string word in words)
            {
                AlignResult align = BestAlign(board.Lines, board.LetterAt, word);
                worstAlign = Math.Max(worstAlign, align.Count);
                if (align.Count > MAX_START_ALIGN)
                {
                    errors.Add($"{word}: {align.Count} letters already lined up");
                }
            }

            int samePosition = 0;
            int mirrorPosition = 0;
            foreach (BoardCell cell in other.KeyCells())
            {
                if (!cell.Key.HasLetter || !positions.TryGetValue(cell.Key.Letter, out BoardCell mine))
                {
                    continue;
                }

                if (mine.R == cell.R && mine.C == cell.C)
                {
                    samePosition++;
                }

                if (mine.R == cell.R && mine.C == GeometryUtils.COLS - 1 - cell.C)
                {
                    mirrorPosition++;
                }
            }

            if (samePosition > MAX_SAME_POSITION)
            {
                errors.Add($"same position letters {samePosition}");
            }

            if (mirrorPosition > MAX_SAME_POSITION)
            {
                errors.Add($"mirrored position letters {mirrorPosition}");
            }

            return errors;
        }
    }
}
