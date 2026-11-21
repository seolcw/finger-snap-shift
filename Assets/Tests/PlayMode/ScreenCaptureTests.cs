using System.Collections;
using NUnit.Framework;
using ShiftMagicSpell.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ShiftMagicSpell.Tests
{
    /// <summary>실제 Main 씬을 띄워 몇 장면을 PNG로 남긴다(Logs/Screens). 화면이 그려지는지만 확인한다.</summary>
    public class ScreenCaptureTests
    {
        [UnityTest]
        public IEnumerator CaptureMainScreens()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            GameRunner runner = Object.FindFirstObjectByType<GameRunner>();
            Assert.IsNotNull(runner, "GameRunner가 씬에 있어야 함");
            yield return null;
            Debug.Log($"[INFO] ScreenCaptureTests::CaptureMainScreens() - Saved. Details: [Path={ScreenCaptureUtils.Capture("01-title")}]");

            GameSession game = runner.Session;
            game.StartPhase(0);
            for (int i = 0; i < 30; i++)
            {
                yield return null;
            }

            Debug.Log($"[INFO] ScreenCaptureTests::CaptureMainScreens() - Saved. Details: [Path={ScreenCaptureUtils.Capture("02-play")}]");
            Assert.AreEqual(GameState.Play, game.State);

            // 줄 돌리기 도중(도미노·잔상·화살표)
            game.Player.Board = 0;
            game.Player.R = 1;
            game.Player.C = 2;
            game.Player.IsShiftHeld = true;
            Assert.IsTrue(game.Player.Rotate(MoveDir.Right));
            for (int i = 0; i < 6; i++)
            {
                yield return null;
            }

            Debug.Log($"[INFO] ScreenCaptureTests::CaptureMainScreens() - Saved. Details: [Path={ScreenCaptureUtils.Capture("03-rotate")}]");

            // 단어 완성 → 반대편 슬롯 블록, 슬롯 위 안내, 위치 힌트 점멸
            for (int i = 0; i < 30; i++)
            {
                yield return null;
            }

            TestDataUtils.PlaceWord(game, 0, game.Words.Current);
            game.Boards[1].Slots[1].Block = new AttackBlock { Word = "BEAM", Hp = 1 };
            game.Player.IsShiftHeld = false;
            game.Player.Board = 1;
            game.Player.R = game.Boards[1].Slots[1].R;
            game.Player.C = game.Boards[1].Slots[1].C;
            for (int i = 0; i < 40; i++)
            {
                yield return null;
            }

            game.HintT0 = game.Time - 0.25f;
            yield return null;
            Debug.Log($"[INFO] ScreenCaptureTests::CaptureMainScreens() - Saved. Details: [Path={ScreenCaptureUtils.Capture("04-slot-hint")}]");

            // 손 예고: 공격을 고르고 예고 중인 장면을 몇 장
            game.IsInvincible = true;
            game.Player.Board = 0;
            game.Player.R = 2;
            game.Player.C = 2;
            int shots = 0;
            for (int frame = 0; frame < 60 * 40 && shots < 4; frame++)
            {
                yield return null;
                if (game.Hand.State == HandState.Warn && game.Hand.T < 0.2f)
                {
                    shots++;
                    Debug.Log($"[INFO] ScreenCaptureTests::CaptureMainScreens() - Saved. Details: [Path={ScreenCaptureUtils.Capture($"05-warn-{shots}-{game.Hand.Attack.Type}")}]");
                    for (int wait = 0; wait < 30; wait++)
                    {
                        yield return null;
                    }
                }
            }
        }
    }
}
