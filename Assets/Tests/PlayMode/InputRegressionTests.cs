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
    }
}
