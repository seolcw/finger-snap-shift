using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ShiftMagicSpell.Core;

namespace ShiftMagicSpell.Tests
{
    /// <summary>웹 verify-rules.js를 옮긴 규칙 검사(씬 없이 GameSession만 돌린다).</summary>
    public class RulesTests
    {
        private GameSession _game;

        [SetUp]
        public void SetUp()
        {
            _game = TestDataUtils.CreateSession();
        }

        private IEnumerable<string> AllWords()
        {
            return _game.Data.Phases.SelectMany(phase => phase.Words).Append(_game.Data.FinisherWord);
        }

        [Test]
        public void WordList_HasNoRepeatedLetters_AndFitsLongestLine()
        {
            List<string> errors = LayoutUtils.CheckWordList(AllWords(), LayoutUtils.LongestLine(_game.Boards[0].Lines));
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void StartLayout_PassesChecks()
        {
            List<string> left = LayoutUtils.CheckStartLayout(_game.Boards[0], _game.Boards[1], AllWords(), out _);
            List<string> right = LayoutUtils.CheckStartLayout(_game.Boards[1], _game.Boards[0], AllWords(), out _);
            Assert.IsEmpty(left.Concat(right), string.Join("\n", left.Concat(right)));
        }

        [Test]
        public void EveryWord_HasSpell()
        {
            foreach (string word in AllWords())
            {
                Assert.IsNotNull(_game.Data.GetSpellOrNull(word), word);
            }
        }

        [Test]
        public void Rotate_CyclesRowAndColumn_PlayerStays()
        {
            _game.StartPhase(0);
            PlayerController player = _game.Player;
            BoardModel board = _game.Boards[0];
            player.Board = 0;
            player.R = 1;
            player.C = 2;
            player.IsShiftHeld = true;

            string before = TestDataUtils.RowString(board, 1);
            Assert.IsTrue(player.Rotate(MoveDir.Right));
            string after = TestDataUtils.RowString(board, 1);
            Assert.AreEqual(before.Substring(before.Length - 1) + before.Substring(0, before.Length - 1), after);
            Assert.AreEqual((1, 2), (player.R, player.C));

            string colBefore = TestDataUtils.ColString(board, 2);
            Assert.IsTrue(player.Rotate(MoveDir.Up));
            Assert.AreEqual(colBefore.Substring(1) + colBefore[0], TestDataUtils.ColString(board, 2));

            player.R = 2;
            player.C = 6;
            int version = board.Version;
            Assert.IsFalse(player.Rotate(MoveDir.Left), "SHIFT 칸 위에서는 줄 돌리기 안 됨");
            Assert.AreEqual(version, board.Version);
        }

        [Test]
        public void CompletedWord_GoesToOppositeSlot_AndSpaceCastsIt()
        {
            _game.StartPhase(0);
            BoardModel right = _game.Boards[1];
            string word = _game.Words.Current;
            int handHp = _game.Hand.Hp;
            TestDataUtils.PlaceWord(_game, 0, word);

            Assert.AreEqual(handHp, _game.Hand.Hp, "블록이 들어갈 때는 손 HP 그대로");
            Assert.AreEqual(word, right.Slots[0].Block?.Word);
            Assert.AreEqual(0, _game.Boards[0].BlockCount());
            Assert.AreNotEqual(word, _game.Words.Current);

            PlayerController player = _game.Player;
            player.Board = 1;
            player.R = right.Slots[0].R;
            player.C = right.Slots[0].C;
            player.IsShiftHeld = true;
            Assert.IsFalse(player.Rotate(MoveDir.Up), "슬롯 칸 위에서는 줄 돌리기 안 됨");
            Assert.IsTrue(player.Fire());
            Assert.AreEqual(handHp - word.Length, _game.Hand.Hp);
            Assert.IsNull(right.Slots[0].Block);
            Assert.AreEqual(HandState.Stun, _game.Hand.State);
        }

        [Test]
        public void SecondCastWithinComboWindow_GetsBonus()
        {
            _game.StartPhase(0);
            BoardModel right = _game.Boards[1];
            _game.Hand.Hp = 30;
            int handHp = _game.Hand.Hp;
            right.Slots[0].Block = new AttackBlock { Word = "FIRE", Hp = 2 };
            right.Slots[1].Block = new AttackBlock { Word = "BOLT", Hp = 2 };
            PlayerController player = _game.Player;
            player.Board = 1;
            player.R = right.Slots[0].R;
            player.C = right.Slots[0].C;
            player.Fire();
            for (int i = 0; i < 20; i++)
            {
                _game.Update(1f / 60f);
            }

            player.TryMove(MoveDir.Down);
            player.Fire();
            int comboDamage = UnityEngine.Mathf.RoundToInt(4 * (1f + _game.Config.ComboBonus));
            Assert.AreEqual(handHp - 4 - comboDamage, _game.Hand.Hp);
        }

        [Test]
        public void FullSlots_CastImmediately_AtOverflowDamage()
        {
            _game.StartPhase(0);
            BoardModel right = _game.Boards[1];
            int handHp = _game.Hand.Hp;
            string word = _game.Words.Current;
            foreach (BoardCell slot in right.Slots)
            {
                slot.Block = new AttackBlock { Word = "NOVA", Hp = 2 };
            }

            TestDataUtils.PlaceWord(_game, 0, word);
            Assert.AreEqual(handHp - UnityEngine.Mathf.RoundToInt(word.Length * _game.Config.OverflowMul), _game.Hand.Hp);
            Assert.AreEqual(2, right.BlockCount());
        }

        [Test]
        public void HintBlinks_OnlyBriefly_AndRestartsForNewWord()
        {
            _game.StartPhase(0);
            float At(float seconds)
            {
                _game.Time = _game.HintT0 + seconds;
                return _game.HintAlpha();
            }

            Assert.Greater(At(0.25f), 0.9f);
            Assert.AreEqual(0f, At(_game.Config.HintDuration + 0.5f));
            Assert.Greater(At(_game.Config.HintInterval + 0.25f), 0.9f);
        }

        [Test]
        public void Finisher_ClearsBlocks_AndShiftWinsWithoutSlot()
        {
            _game.StartPhase(2);
            _game.Boards[0].Slots[0].Block = new AttackBlock { Word = "COMET", Hp = 2 };
            _game.Hand.Hp = 5;
            _game.Words.Refresh();
            Assert.AreEqual("SHIFT", _game.Words.Current);
            Assert.IsTrue(_game.Boards.All(board => board.BlockCount() == 0));

            TestDataUtils.PlaceWord(_game, 0, "SHIFT");
            Assert.AreEqual(GameState.Win, _game.State);
            Assert.AreEqual(0, _game.Boards[1].BlockCount());
        }

        [Test]
        public void LastPhase_NonFinisherCannotDropHandBelowOne()
        {
            _game.StartPhase(2);
            _game.Hand.Hp = 7;
            BoardModel right = _game.Boards[1];
            right.Slots[0].Block = new AttackBlock { Word = "DRAGON", Hp = 2 };
            _game.Player.Board = 1;
            _game.Player.R = right.Slots[0].R;
            _game.Player.C = right.Slots[0].C;
            _game.Player.Fire();
            Assert.AreEqual(1, _game.Hand.Hp);
            Assert.AreEqual("SHIFT", _game.Words.Current);
        }

        [Test]
        public void SlotBreak_CracksOnFirstHit_RemovesOnSecond()
        {
            _game.StartPhase(0);
            BoardCell slot = _game.Boards[1].Slots[0];
            slot.Block = new AttackBlock { Word = "FIRE", Hp = 2 };
            _game.Player.Board = 0;
            var attack = new AttackPlan { Type = AttackType.Slot, Name = "slot", Board = 1, Slot = slot, Cells = new List<BoardCell> { slot } };
            _game.Hand.ForceFire(attack);
            Assert.AreEqual(1, slot.Block?.Hp);
            _game.Hand.ForceFire(attack);
            Assert.IsNull(slot.Block);
        }

        [Test]
        public void Hand_UsesAllowedAttacks_AndNeverChangesLetters()
        {
            // 무적 플레이어를 가만히 두고 페이즈마다 90초씩 돌린다: 예외 없이 공격이 나오고, 판의 글자 구성은 그대로
            for (int phase = 0; phase < _game.Data.Phases.Count; phase++)
            {
                _game.StartPhase(phase);
                _game.IsInvincible = true;
                string before = LettersOf(_game);
                for (int frame = 0; frame < 90 * 60; frame++)
                {
                    _game.Update(1f / 60f);
                }

                Assert.AreEqual(before, LettersOf(_game), $"phase {phase + 1}");
                Assert.AreEqual(GameState.Play, _game.State);
                UnityEngine.Debug.Log($"[INFO] RulesTests::Hand_UsesAllowedAttacks_AndNeverChangesLetters() - Phase done. Details: [Phase={phase + 1}, Attacks={string.Join(", ", _game.Stats.Attacks.Select(pair => pair.Key + "=" + pair.Value))}]");
            }

            int total = _game.Stats.Attacks.Values.Sum();
            UnityEngine.Debug.Log($"[INFO] RulesTests::Hand_UsesAllowedAttacks_AndNeverChangesLetters() - Attack counts. Details: [{string.Join(", ", _game.Stats.Attacks.Select(pair => pair.Key + "=" + pair.Value))}]");
            Assert.Greater(total, 60);
            Assert.Greater(_game.Stats.Attacks.Count, 4);
        }

        private static string LettersOf(GameSession game)
        {
            return string.Join("|", game.Boards.Select(board => new string(board.KeyCells().Select(cell => cell.Key.HasLetter ? cell.Key.Letter : '_').OrderBy(ch => ch).ToArray())));
        }

        [Test]
        public void Death_RestartsPhase_HandRecoversHalfOfLostHp()
        {
            _game.StartPhase(0);
            _game.Hand.Hp = 4;
            _game.Boards[1].Slots[0].Block = new AttackBlock { Word = "BOLT", Hp = 1 };
            _game.Player.Hp = 1;
            _game.Player.Invuln = 0f;
            _game.Player.Hurt("test");
            Assert.AreEqual(GameState.Dead, _game.State);
            for (int i = 0; i < 120; i++)
            {
                _game.Update(1f / 60f);
            }

            Assert.AreEqual(GameState.Play, _game.State);
            Assert.AreEqual(UnityEngine.Mathf.CeilToInt(4 + (_game.Hand.MaxHp - 4) * 0.5f), _game.Hand.Hp);
            Assert.AreEqual("BOLT", _game.Boards[1].Slots[0].Block?.Word, "쓰러져도 슬롯 블록은 남는다(금 간 상태 그대로)");
            Assert.AreEqual(1, _game.Boards[1].Slots[0].Block.Hp);
            Assert.AreEqual("test", _game.LastHitSource, "사망 원인");
            Assert.AreEqual(_game.Config.PlayerHp, _game.Player.Hp);
            Assert.AreEqual(0, _game.Phase);
        }

        [Test]
        public void PhaseClear_MovesToNextPhase()
        {
            _game.StartPhase(0);
            _game.Hand.Hp = 3;
            _game.CastSpell("FIRE", UnityEngine.Vector2.zero, 1f);
            Assert.AreEqual(GameState.PhaseClear, _game.State);
            Step(_game.Config.PhaseClearTime + _game.Config.PhaseIntroTime + 0.1f);

            Assert.AreEqual(GameState.Play, _game.State);
            Assert.AreEqual(1, _game.Phase);
            Assert.AreEqual(_game.Data.Phases[1].HandHp, _game.Hand.Hp);
        }

        [Test]
        public void QuickDoubleTap_SecondMoveWaitsForRest_AndIsNotLost()
        {
            _game.StartPhase(0);
            _game.IsInvincible = true;
            PlayerController player = _game.Player;
            player.Board = 0;
            player.R = 2;
            player.C = 1;
            player.Press(MoveDir.Right);
            player.Release(MoveDir.Right);
            Step(0.02f);
            player.Press(MoveDir.Right);
            player.Release(MoveDir.Right);
            Assert.AreEqual(2, player.C, "쉬는 시간 안에 누른 두 번째 입력은 바로 실행되지 않음");
            Step(_game.Config.MoveDelay);
            Assert.AreEqual(3, player.C, "쉬는 시간이 끝나면 예약된 입력이 실행됨");
        }

        [Test]
        public void QuickDoubleRotate_SecondWaitsForRotateDelay()
        {
            _game.StartPhase(0);
            _game.IsInvincible = true;
            PlayerController player = _game.Player;
            BoardModel board = _game.Boards[0];
            player.Board = 0;
            player.R = 1;
            player.C = 2;
            player.IsShiftHeld = true;
            int version = board.Version;
            player.Press(MoveDir.Right);
            player.Release(MoveDir.Right);
            Step(0.05f);
            player.Press(MoveDir.Right);
            player.Release(MoveDir.Right);
            player.IsShiftHeld = false;
            Assert.AreEqual(version + 1, board.Version);
            Step(_game.Config.RotateDelay);
            Assert.AreEqual(version + 2, board.Version, "Shift를 뗀 뒤라도 예약 당시 줄 돌리기로 실행");
            Assert.AreEqual((1, 2), (player.R, player.C));
        }

        private void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 60f)
            {
                _game.Update(1f / 60f);
            }
        }

        [Test]
        public void NewPhase_ClearsBlocks()
        {
            _game.StartPhase(0);
            _game.Boards[1].Slots[0].Block = new AttackBlock { Word = "FIRE", Hp = 2 };
            _game.StartPhase(1);
            Assert.IsTrue(_game.Boards.All(board => board.BlockCount() == 0));
        }
    }
}
