using System.Collections;
using NUnit.Framework;
using ShiftMagicSpell.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ShiftMagicSpell.Tests
{
    /// <summary>입력 회귀 테스트: Main 씬에 가상 키보드 이벤트를 넣는다.</summary>
    public class InputRegressionTests : InputTestFixture
    {
        // WebGL 빌드에서 발견: 방향키를 짧게 쳐서 누름·뗌이 한 프레임에 들어오면 계속 눌린 것으로 남아 끝까지 미끄러졌다
        [UnityTest]
        public IEnumerator TapWithinOneFrame_MovesExactlyOneCell()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            GameSession game = Object.FindFirstObjectByType<GameRunner>().Session;
            game.StartNewRun();
            game.IsInvincible = true;
            int startC = game.Player.C;

            // Input System은 키를 처음 조회한 다음 업데이트부터 한 프레임 안의 누름·뗌을 둘 다 기록한다(실제 플레이와 같은 조건)
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            Press(keyboard.dKey, queueEventOnly: true);
            Release(keyboard.dKey, queueEventOnly: true);
            for (int i = 0; i < 40; i++)
            {
                yield return null;
            }

            Assert.AreEqual(startC + 1, game.Player.C, "한 번 친 키는 한 칸만 움직여야 함");
        }

        // 검사에서 발견: 페이즈 클리어 직후 R을 누르면 방금 깬 페이즈가 손 HP 가득으로 다시 시작됐다
        [UnityTest]
        public IEnumerator RestartKey_IsIgnoredDuringPhaseClear()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            GameSession game = Object.FindFirstObjectByType<GameRunner>().Session;
            game.StartPhase(0);
            game.IsInvincible = true;
            yield return null;
            game.Hand.Hp = 2;
            game.CastSpell("FIRE", Vector2.zero, 1f);
            Assert.AreEqual(GameState.PhaseClear, game.State);
            Press(keyboard.rKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.rKey, queueEventOnly: true);
            yield return null;
            Assert.AreEqual(GameState.PhaseClear, game.State, "전환 중에는 R을 받지 않는다");
            Assert.AreEqual(0, game.Hand.Hp);
        }

        // 플레이 평가에서 발견: 쓰러짐 화면에서 P를 누르면 PAUSED가 겹쳤다
        [UnityTest]
        public IEnumerator PauseKey_IsIgnoredWhileDead()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            GameSession game = Object.FindFirstObjectByType<GameRunner>().Session;
            game.StartPhase(0);
            yield return null;
            game.Player.Hp = 1;
            game.Player.Invuln = 0f;
            game.Player.Hurt("TEST");
            Assert.AreEqual(GameState.Dead, game.State);
            Press(keyboard.pKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.pKey, queueEventOnly: true);
            yield return null;
            Assert.IsFalse(game.IsPaused, "쓰러짐 화면에서는 일시정지하지 않는다");
        }
    }
}
