using System.Linq;
using NUnit.Framework;
using ShiftMagicSpell.Core;
using UnityEngine;

namespace ShiftMagicSpell.Tests
{
    /// <summary>
    /// 씬 없이 로직만 돌리는 봇 시뮬레이션(웹 simtest.js·verify-rules.js 5번). 사람이 직접 한 플레이가 아니다.
    /// 봇은 행동 간격마다 한 번 움직이고, 처음부터 마무리 SHIFT 승리까지 가야 한다.
    /// </summary>
    public class BotSimulationTests
    {
        private const float MAX_GAME_SECONDS = 1500f;
        private const float DT = 1f / 60f;

        [TestCase(11, 0.22f, 1f)]
        [TestCase(22, 0.22f, 1f)]
        [TestCase(33, 0.6f, 1f)]
        [TestCase(44, 0.22f, 0.7f)]
        public void Bot_WinsFromStartToFinisher(int seed, float actionInterval, float dodgeChance)
        {
            GameSession game = TestDataUtils.CreateSession(seed);
            var bot = new BotPlayer();
            var random = new System.Random(seed);
            game.StartNewRun();
            string baseLetters = LettersOf(game);
            bool hasLettersChanged = false;
            float time = 0f;
            float actionT = 0f;
            while (time < MAX_GAME_SECONDS && game.State != GameState.Win)
            {
                time += DT;
                actionT -= DT;
                if (game.State == GameState.Play && actionT <= 0f)
                {
                    actionT = actionInterval;
                    BotPlayer.Apply(game, bot.Decide(game, random.NextDouble() < dodgeChance));
                }

                game.Update(DT);
                if (game.State == GameState.Play && LettersOf(game) != baseLetters)
                {
                    hasLettersChanged = true;
                }
            }

            GameStats stats = game.Stats;
            Debug.Log($"[INFO] BotSimulationTests::Bot_WinsFromStartToFinisher() - Run finished. Details: [Seed={seed}, Interval={actionInterval}, Dodge={dodgeChance}, State={game.State}, Time={time:0}s, Words={stats.WordsDone}, Casts={stats.Casts}, Combos={stats.Combos}, BlocksLost={stats.BlocksLost}, Rotations={stats.Rotations}, Hits={stats.Hits.Values.Sum()}, Deaths={stats.Deaths}, Teleports={stats.Teleports}, Attacks={string.Join(" ", stats.Attacks.Select(pair => pair.Key + "=" + pair.Value))}]");
            Assert.IsFalse(hasLettersChanged, "손 공격과 줄 돌리기 중에 판의 글자 구성(26자 + 빈 키 2)은 그대로여야 함");
            Assert.AreEqual(GameState.Win, game.State, $"{MAX_GAME_SECONDS}초 안에 승리해야 함");
        }

        private static string LettersOf(GameSession game)
        {
            return string.Join("|", game.Boards.Select(board => new string(board.KeyCells().Select(cell => cell.Key.HasLetter ? cell.Key.Letter : '_').OrderBy(ch => ch).ToArray())));
        }
    }
}
