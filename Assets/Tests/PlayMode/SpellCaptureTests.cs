using System.Collections;
using NUnit.Framework;
using ShiftMagicSpell.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ShiftMagicSpell.Tests
{
    /// <summary>단어마다 마법을 쏘고 날아가는 중·터지는 중을 PNG로 남긴다(Logs/Screens/spell-*). 예외 없이 그려지는지 확인.</summary>
    public class SpellCaptureTests
    {
        [UnityTest]
        public IEnumerator CaptureEverySpell()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            GameSession game = Object.FindFirstObjectByType<GameRunner>().Session;
            game.StartPhase(0);
            game.IsInvincible = true;
            string[] words = { "FIRE", "BOLT", "WIND", "BEAM", "NOVA", "MAGIC", "FROST", "LASER", "STORM", "BLADE", "ROCKET", "VORTEX", "DRAGON", "SHIFT" };
            foreach (string word in words)
            {
                game.StartPhase(0);
                game.IsInvincible = true;
                game.Hand.Hp = 999;
                game.CastSpell(word == "SHIFT" ? "FIRE" : word, GeometryUtils.CellCenter(0, 2, 3), 1f);
                if (word == "SHIFT")
                {
                    // 마무리 연출만 보려고 손 HP를 건드리지 않고 연출 기록의 모양만 바꾼다
                    FxEvent spell = game.Fx.Find(fx => fx.Kind == FxKind.Spell);
                    spell.Style = "shift";
                    spell.Color = game.Data.GetSpellOrNull("SHIFT").Color;
                }

                float duration = game.Config.SpellTime;
                yield return WaitGameTime(game, duration * 0.35f);
                ScreenCaptureUtils.Capture($"spell-{word}-1fly");
                yield return WaitGameTime(game, duration * 0.35f);
                ScreenCaptureUtils.Capture($"spell-{word}-2hit");
            }

            Assert.AreEqual(GameState.Play, game.State);
        }

        private static IEnumerator WaitGameTime(GameSession game, float seconds)
        {
            float end = game.Time + seconds;
            while (game.Time < end)
            {
                yield return null;
            }
        }
    }
}
