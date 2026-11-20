using System.Collections.Generic;

namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 목표 단어. 한 줄(행은 왼→오, 열은 위→아래)의 연속한 칸에 맞추면 완성. 양쪽 키보드 어디든 된다.
    /// 페이즈 단어를 목록 순서대로(쉬운 것부터) 돌려 쓰고, 마지막 페이즈에서 손 HP가 낮아지면 마무리 단어로 바뀐다.
    /// </summary>
    public class WordSystem
    {
        private readonly GameSession _game;
        private int _order;
        private int _lastCount;

        public string Current { get; private set; }
        public string Next { get; private set; }

        public WordSystem(GameSession game)
        {
            _game = game;
        }

        public void Reset()
        {
            _order = 0;
            Current = null;
            Next = null;
            Current = Pick();
            Next = Pick();
            OnNewWord();
        }

        /// <summary>두 판 중 가장 많이 맞춰진 자리(같으면 플레이어가 있는 판)</summary>
        public AlignResult BestOrNull()
        {
            if (Current == null)
            {
                return null;
            }

            AlignResult best = null;
            foreach (BoardModel board in _game.Boards)
            {
                AlignResult align = board.Align(Current);
                if (best == null || align.Count > best.Count || (align.Count == best.Count && board.Index == _game.Player.Board))
                {
                    best = align;
                }
            }

            return best;
        }

        /// <summary>판이 바뀔 때마다(플레이어·손의 줄 돌리기) 부른다</summary>
        public void Check()
        {
            AlignResult align = BestOrNull();
            if (align == null)
            {
                return;
            }

            if (align.Count == Current.Length)
            {
                Complete(align);
                return;
            }

            // 제자리 글자가 늘었을 때만 그 칸들을 잠깐 초록으로 번쩍여 알려 준다(위치를 계속 보여 주지는 않는다)
            if (align.Count > _lastCount)
            {
                BoardModel board = _game.Boards[align.Board];
                var matched = new List<BoardCell>();
                for (int i = 0; i < align.Cells.Count; i++)
                {
                    if (board.LetterAt(align.Cells[i].R, align.Cells[i].C) == Current[i])
                    {
                        matched.Add(align.Cells[i]);
                    }
                }

                _game.Fx.Add(new FxEvent { Kind = FxKind.Match, Board = align.Board, Cells = FxEvent.ToCellList(matched), T = 0.5f, Duration = 0.6f });
            }

            _lastCount = align.Count;
        }

        public bool IsFinisherReady => _game.IsLastPhase && _game.Hand.Hp <= _game.Config.FinisherHp;

        /// <summary>마지막 페이즈에서 손 HP가 낮아지면 목표를 바로 마무리 단어로 바꾼다</summary>
        public void Refresh()
        {
            string finisher = _game.Data.FinisherWord;
            if (!IsFinisherReady || Current == finisher)
            {
                return;
            }

            // 슬롯에 남은 블록은 마무리 구간에서 쓸모가 없으므로(손 HP를 1 아래로 못 내림) 여기서 사라진다
            foreach (BoardModel board in _game.Boards)
            {
                foreach (BoardCell slot in board.Slots)
                {
                    if (slot.Block != null)
                    {
                        _game.Fx.Add(FxEvent.AtCell(FxKind.Fizzle, board.Index, slot.R, slot.C, 0.5f));
                        slot.Block = null;
                    }
                }
            }

            Current = finisher;
            Next = null;
            OnNewWord();
            _game.ShowToast("FINISH IT: LINE UP SHIFT!");
        }

        private void Complete(AlignResult align)
        {
            string done = Current;
            Current = Next;
            Next = Pick();
            OnNewWord();
            _game.OnWordComplete(done, align);
        }

        // 새 목표 단어: 위치 힌트 점멸을 처음부터 다시(바로 한 번 깜빡인다)
        private void OnNewWord()
        {
            _lastCount = 0;
            _game.HintT0 = _game.Time;
        }

        // 지금 목표와 같거나 이미 맞춰져 있는 단어는 건너뛴다
        private string Pick()
        {
            List<string> words = _game.CurrentPhase.Words;
            for (int i = 0; i < words.Count; i++)
            {
                string word = words[_order++ % words.Count];
                if (word != Current && IsUnsolvedOnBothBoards(word))
                {
                    return word;
                }
            }

            return words[0];
        }

        private bool IsUnsolvedOnBothBoards(string word)
        {
            foreach (BoardModel board in _game.Boards)
            {
                if (board.Align(word).Count >= word.Length)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
