using System.Collections;
using System.Linq;
using NUnit.Framework;
using ShiftMagicSpell.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ShiftMagicSpell.Tests
{
    /// <summary>
    /// 실제 Main 씬에서 연습 구간 → 페이즈 1 → 사망 후 재시작(블록 유지) → 페이즈 전환 → 승리까지 장면을 PNG로 남긴다(Logs/Screens/flow-*).
    /// 조작은 로직을 직접 움직여 만든다(사람 플레이가 아님).
    /// </summary>
    public class FlowCaptureTests
    {
        private GameSession _game;

        [UnityTest]
        public IEnumerator CaptureFirstPlayFlow()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            _game = Object.FindFirstObjectByType<GameRunner>().Session;
            _game.StartNewRun();
            PlayerController player = _game.Player;
            yield return Capture("flow-01-practice-move", 1f);

            player.Press(MoveDir.Right);
            player.Release(MoveDir.Right);
            yield return Capture("flow-02-practice-rotate", 0.5f);

            player.IsShiftHeld = true;
            player.Press(MoveDir.Up);
            player.Release(MoveDir.Up);
            player.IsShiftHeld = false;
            yield return Capture("flow-03-practice-line-up", 0.6f);

            TestDataUtils.PlaceWord(_game, 0, _game.Words.Current);
            yield return Capture("flow-04-practice-go-to-slot", 1f);
            Assert.AreEqual(3, _game.TutorialStep);

            BoardCell slot = _game.Boards[1].Slots.First(s => s.Block != null);
            player.Board = 1;
            player.R = slot.R;
            player.C = slot.C;
            yield return Capture("flow-05-practice-on-slot", 0.3f);
            Assert.IsTrue(player.Fire());
            yield return Capture("flow-06-practice-over", 0.5f);
            Assert.IsFalse(_game.IsTutorial);

            // 페이즈 1: 손이 공격하기 시작
            player.Board = 0;
            player.R = 2;
            player.C = 2;
            yield return Wait(4f);
            yield return Capture("flow-07-phase1-attacks", 0f);

            // 블록 하나를 모아 둔 채 쓰러짐 → 원인 표시 → 다시 시작해도 블록 유지
            _game.Boards[1].Slots[1].Block = new AttackBlock { Word = "BOLT", Hp = 2 };
            player.Hp = 1;
            player.Invuln = 0f;
            player.Hurt(AttackType.Click.ToDisplayName());
            yield return Capture("flow-08-dead-cause", 0.5f);
            Assert.AreEqual(GameState.Dead, _game.State);
            yield return Wait(1.5f);
            yield return Capture("flow-09-restart-block-kept", 0.2f);
            Assert.AreEqual("BOLT", _game.Boards[1].Slots[1].Block?.Word);

            // 페이즈 전환 → 마지막 페이즈 → SHIFT 승리
            _game.IsInvincible = true;
            for (int phase = 0; phase < 2; phase++)
            {
                _game.Hand.Hp = 2;
                _game.CastSpell("FIRE", GeometryUtils.CellCenter(0, 2, 3), 1f);
                yield return Wait(_game.Config.PhaseClearTime + 0.3f);
                if (phase == 0)
                {
                    yield return Capture("flow-10-phase2-banner", 0.5f);
                }

                yield return Wait(_game.Config.PhaseIntroTime);
            }

            Assert.AreEqual(2, _game.Phase);
            _game.Hand.Hp = 5;
            _game.Words.Refresh();
            yield return Capture("flow-11-final-shift", 0.4f);
            TestDataUtils.PlaceWord(_game, 0, "SHIFT");
            yield return Capture("flow-12-victory", 2f);
            Assert.AreEqual(GameState.Win, _game.State);
        }

        private IEnumerator Capture(string fileName, float afterSeconds)
        {
            yield return Wait(afterSeconds);
            ScreenCaptureUtils.Capture(fileName);
        }

        private IEnumerator Wait(float seconds)
        {
            float end = _game.Time + seconds;
            while (_game.Time < end)
            {
                yield return null;
            }
        }
    }
}
