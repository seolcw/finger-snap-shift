using System.Collections;
using System.Linq;
using NUnit.Framework;
using ShiftMagicSpell.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ShiftMagicSpell.Tests
{
    /// <summary>
    /// 실제 Main 씬(GameRunner·GameInput·렌더러)을 띄우고, 봇이 가상 키보드(Input System 테스트 장치)로 키를 눌러 플레이한다.
    /// 프레임마다 게임 시간을 1/60초씩 진행시켜(captureDeltaTime) 실제 시간보다 빨리 돈다. 사람이 직접 한 플레이가 아니다.
    /// </summary>
    public class BotScenePlayTests : InputTestFixture
    {
        private const float MAX_GAME_SECONDS = 900f;
        private const float ACTION_INTERVAL = 0.22f;

        private Keyboard _keyboard;
        private GameSession _game;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            Time.captureDeltaTime = 1f / 60f;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            base.TearDown();
        }

        private IEnumerator LoadAndStart()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            _game = Object.FindFirstObjectByType<GameRunner>().Session;
            Assert.AreEqual(GameState.Ready, _game.State, "첫 화면은 시작 대기");

            // 첫 화면: 아무 키(여기서는 Enter)로 시작
            yield return Tap(_keyboard.enterKey);
            Assert.AreEqual(GameState.Play, _game.State, "키를 누르면 시작");
        }

        // 테스트 장치는 이벤트마다 키보드 전체 상태를 복사해 보내므로, 같은 프레임에 두 키를 큐에 넣으면 뒤 이벤트가 앞 키를 덮어쓴다.
        // 사람처럼 Shift를 먼저 누르고 다음 프레임에 방향키를 누른 뒤, 한 프레임씩 나눠 뗀다.
        private IEnumerator Tap(ButtonControl key, ButtonControl modifier = null)
        {
            if (modifier != null)
            {
                Press(modifier, queueEventOnly: true);
                yield return null;
            }

            Press(key, queueEventOnly: true);
            yield return null;
            Release(key, queueEventOnly: true);
            yield return null;
            if (modifier != null)
            {
                Release(modifier, queueEventOnly: true);
                yield return null;
            }
        }

        private ButtonControl KeyFor(MoveDir dir)
        {
            switch (dir)
            {
                case MoveDir.Up:
                    return _keyboard.upArrowKey;
                case MoveDir.Down:
                    return _keyboard.downArrowKey;
                case MoveDir.Left:
                    return _keyboard.leftArrowKey;
                default:
                    return _keyboard.rightArrowKey;
            }
        }

        private IEnumerator PlayAction(BotAction action)
        {
            switch (action.Kind)
            {
                case BotActionKind.Move:
                    yield return Tap(KeyFor(action.Dir));
                    break;
                case BotActionKind.Rotate:
                    yield return Tap(KeyFor(action.Dir), _keyboard.leftShiftKey);
                    break;
                case BotActionKind.Fire:
                    yield return Tap(_keyboard.spaceKey);
                    break;
                default:
                    yield return null;
                    break;
            }
        }

        [UnityTest]
        public IEnumerator Bot_PlaysMainScene_WithKeyboard_UntilVictory()
        {
            yield return LoadAndStart();
            var bot = new BotPlayer();
            int lastPhase = 0;
            float nextAction = 0f;
            while (_game.State != GameState.Win && _game.Time < MAX_GAME_SECONDS)
            {
                if (_game.Phase != lastPhase)
                {
                    lastPhase = _game.Phase;
                    ScreenCaptureUtils.Capture($"bot-phase{lastPhase + 1}-start");
                }

                if (_game.State == GameState.Play && _game.Time >= nextAction)
                {
                    nextAction = _game.Time + ACTION_INTERVAL;
                    yield return PlayAction(bot.Decide(_game, true));
                }
                else
                {
                    yield return null;
                }
            }

            for (int i = 0; i < 90; i++)
            {
                yield return null;
            }

            ScreenCaptureUtils.Capture("bot-victory");
            GameStats stats = _game.Stats;
            Debug.Log($"[INFO] BotScenePlayTests::Bot_PlaysMainScene_WithKeyboard_UntilVictory() - Run finished. Details: [State={_game.State}, GameTime={_game.Time:0}s, Words={stats.WordsDone}, Casts={stats.Casts}, Combos={stats.Combos}, Rotations={stats.Rotations}, Hits={stats.Hits.Values.Sum()}, Deaths={stats.Deaths}, Teleports={stats.Teleports}]");
            Assert.AreEqual(GameState.Win, _game.State);
            Assert.Greater(stats.Rotations, 0, "Shift+방향키 입력으로 줄을 돌렸어야 함");
            Assert.Greater(stats.Casts, 0, "Space 입력으로 슬롯을 발동했어야 함");

            // 승리 화면에서 R → 1페이즈부터 새 판
            yield return Tap(_keyboard.rKey);
            Assert.AreEqual(GameState.Play, _game.State);
            Assert.AreEqual(0, _game.Phase);
        }

        [UnityTest]
        public IEnumerator Bot_DiesStandingStill_AndPhaseRestarts()
        {
            yield return LoadAndStart();

            // 봇이 마법을 한 번 맞혀 손 HP를 깎고 판을 돌려 둔 뒤, 키를 놓고 가만히 서서 쓰러질 때까지 기다린다
            var bot = new BotPlayer();
            float nextAction = 0f;
            int handHpBeforeDeath = _game.Hand.Hp;
            while (_game.State != GameState.Dead && _game.Time < MAX_GAME_SECONDS)
            {
                handHpBeforeDeath = _game.Hand.Hp;
                bool isIdle = _game.Hand.Hp < _game.Hand.MaxHp;
                if (!isIdle && _game.State == GameState.Play && _game.Time >= nextAction)
                {
                    nextAction = _game.Time + ACTION_INTERVAL;
                    yield return PlayAction(bot.Decide(_game, true));
                }
                else
                {
                    yield return null;
                }
            }

            Assert.AreEqual(GameState.Dead, _game.State, "가만히 서 있으면 언젠가 쓰러져야 함");
            int phase = _game.Phase;
            int maxHp = _game.Hand.MaxHp;
            int expectedHandHp = Mathf.CeilToInt(handHpBeforeDeath + (maxHp - handHpBeforeDeath) * _game.Config.DeathHandRecover);
            ScreenCaptureUtils.Capture("bot-dead");
            while (_game.State == GameState.Dead)
            {
                yield return null;
            }

            Assert.AreEqual(GameState.Play, _game.State);
            Assert.AreEqual(phase, _game.Phase, "같은 페이즈 처음부터");
            Assert.AreEqual(_game.Config.PlayerHp, _game.Player.Hp, "플레이어 HP 가득");
            Assert.AreEqual(expectedHandHp, _game.Hand.Hp, "손 HP는 잃은 만큼의 절반만 회복");
            Assert.IsTrue(_game.Boards.All(board => board.BlockCount() == 0), "슬롯 블록은 사라짐");
            var fresh = TestDataUtils.CreateSession();
            fresh.StartPhase(0);
            for (int b = 0; b < 2; b++)
            {
                for (int row = 0; row < GeometryUtils.ROWS; row++)
                {
                    Assert.AreEqual(TestDataUtils.RowString(fresh.Boards[b], row), TestDataUtils.RowString(_game.Boards[b], row), "판은 시작 배치로");
                }
            }

            Debug.Log($"[INFO] BotScenePlayTests::Bot_DiesStandingStill_AndPhaseRestarts() - Restarted. Details: [Phase={phase + 1}, HandHpBefore={handHpBeforeDeath}, HandHpAfter={_game.Hand.Hp}, GameTime={_game.Time:0}s]");
        }
    }
}
