using System.Collections;
using NUnit.Framework;
using ShiftMagicSpell.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ShiftMagicSpell.Tests
{
    /// <summary>실제 Main 씬에서 1→2, 2→3 페이즈 전환과 SHIFT 마무리 전환을 시간 순서대로 PNG로 남긴다(Logs/Screens/transition-*).</summary>
    public class TransitionCaptureTests
    {
        private GameSession _game;

        [UnityTest]
        public IEnumerator CapturePhaseTransitions()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            _game = Object.FindFirstObjectByType<GameRunner>().Session;
            _game.StartNewRun();
            _game.IsInvincible = true;
            yield return Wait(2f);

            for (int phase = 0; phase < 2; phase++)
            {
                _game.Hand.Hp = 3;
                _game.CastSpell("FIRE", GeometryUtils.CellCenter(0, 2, 3), 1f);
                float clear = _game.Config.PhaseClearTime;
                float intro = _game.Config.PhaseIntroTime;
                string prefix = $"transition-{phase + 1}to{phase + 2}";
                yield return CaptureAt(prefix + "-a-hit", 0.6f);
                yield return CaptureAt(prefix + "-b-retreat", clear * 0.85f - 0.6f);
                Assert.AreEqual(GameState.PhaseClear, _game.State);
                yield return CaptureAt(prefix + "-c-banner-flash", clear * 0.15f + 0.08f);
                Assert.AreEqual(GameState.PhaseIntro, _game.State);
                yield return CaptureAt(prefix + "-d-banner", intro * 0.6f);
                yield return CaptureAt(prefix + "-e-play", intro * 0.4f + 0.2f);
                Assert.AreEqual(GameState.Play, _game.State);
                Assert.AreEqual(phase + 1, _game.Phase);
                yield return Wait(1.5f);
            }

            // 마지막 페이즈: SHIFT 마무리로 바뀌는 순간
            _game.Hand.Hp = 7;
            _game.CastSpell("FIRE", GeometryUtils.CellCenter(0, 2, 3), 1f);
            Assert.AreEqual("SHIFT", _game.Words.Current);
            yield return CaptureAt("transition-final-a-flash", 0.08f);
            yield return CaptureAt("transition-final-b-banner", 0.8f);
            yield return CaptureAt("transition-final-c-after", _game.Config.FinisherIntroTime);
        }

        private IEnumerator CaptureAt(string fileName, float afterSeconds)
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
