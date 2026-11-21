using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ShiftMagicSpell.Audio;
using ShiftMagicSpell.Core;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ShiftMagicSpell.Tests
{
    /// <summary>Audio.prefab을 띄우고 GameSession에 이벤트를 일으켜, SoundManager가 어떤 소리를 요청하는지 본다(실제 소리는 듣지 않는다).</summary>
    public class SoundManagerTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Audio.prefab";
        private const float DT = 1f / 60f;

        private readonly List<SoundKey> _played = new List<SoundKey>();
        private GameObject _audio;
        private SoundManager _manager;
        private GameSession _game;

        [SetUp]
        public void SetUp()
        {
            // 앞선 씬 테스트가 남긴 Main 씬에도 Audio가 있어서, 싱글톤 자리를 차지하고 있으면 테스트용 프리팹이 스스로 지워진다
            if (SoundManager.Instance != null)
            {
                Object.DestroyImmediate(SoundManager.Instance.gameObject);
            }

#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            Assert.IsNotNull(prefab, PREFAB_PATH);
            _audio = Object.Instantiate(prefab);
#endif
            _manager = _audio.GetComponent<SoundManager>();
            _manager.SetMuted(false);
            _manager.OnSoundRequested += _played.Add;
            _game = TestDataUtils.CreateSession();
            // 앞선 씬 테스트가 남긴 GameRunner를 찾아 붙기 전에 바로 테스트용 판에 묶는다
            _manager.Bind(_game);
        }

        [TearDown]
        public void TearDown()
        {
            _played.Clear();
            // 다음 테스트의 프리팹이 싱글톤 중복으로 지워지지 않게 바로 없앤다
            Object.DestroyImmediate(_audio);
            AudioListener.volume = 1f;
        }

        [Test]
        public void Library_HasClipForEveryKey()
        {
            Assert.IsNotNull(_manager.Library);
            Assert.IsNotNull(_manager.Library.Music, "배경음");
            foreach (SoundKey key in Enum.GetValues(typeof(SoundKey)))
            {
                Assert.IsTrue(_manager.Library.TryGetEntry(key, out _), key.ToString());
            }
        }

        [UnityTest]
        public IEnumerator StartClick_PlaysStart_AndStartsMusic_WithoutStepSound()
        {
            _manager.Bind(_game);
            yield return null;
            Assert.IsFalse(_manager.HasMusicStarted, "첫 화면(Ready)에서는 배경음을 틀지 않는다(WebGL 자동 재생 막힘)");

            _game.StartNewRun();
            yield return null;
            CollectionAssert.Contains(_played, SoundKey.Start);
            CollectionAssert.DoesNotContain(_played, SoundKey.Move, "페이즈 시작으로 칸이 바뀐 것은 걸음이 아니다");
            Assert.IsTrue(_manager.HasMusicStarted);
        }

        [UnityTest]
        public IEnumerator MoveRotateDeny_AreHeard()
        {
            yield return BindAtPlay();
            PlayerController player = _game.Player;
            player.Board = 0;
            player.R = 1;
            player.C = 2;
            yield return Settle();

            player.IsShiftHeld = true;
            Assert.IsTrue(player.Rotate(MoveDir.Right));
            yield return null;
            // 돌린 결과 제자리 글자가 늘면 LetterMatch도 같이 날 수 있다
            CollectionAssert.Contains(_played, SoundKey.Rotate);
            CollectionAssert.DoesNotContain(_played, SoundKey.Move, "줄 돌리기는 플레이어가 움직이지 않는다");

            _played.Clear();
            player.IsShiftHeld = false;
            Assert.IsTrue(player.TryMove(MoveDir.Down));
            yield return null;
            CollectionAssert.AreEqual(new[] { SoundKey.Move }, _played);

            BoardCell shift = _game.Boards[0].ShiftCell;
            player.R = shift.R;
            player.C = shift.C;
            yield return Settle();
            player.IsShiftHeld = true;
            Assert.IsFalse(player.Rotate(MoveDir.Left));
            yield return null;
            CollectionAssert.AreEqual(new[] { SoundKey.Deny }, _played);
        }

        [UnityTest]
        public IEnumerator Teleport_PlaysWarpOnce_NotStep()
        {
            yield return BindAtPlay();
            PlayerController player = _game.Player;
            BoardCell shift = _game.Boards[0].ShiftCell;
            player.Board = 0;
            player.R = shift.R;
            player.C = shift.C + 1;
            yield return Settle();

            Assert.IsTrue(player.TryMove(MoveDir.Left));
            yield return null;
            Assert.AreEqual(1, player.Board, "반대편 판으로 순간이동");
            CollectionAssert.AreEqual(new[] { SoundKey.Warp }, _played);
        }

        [UnityTest]
        public IEnumerator WordComplete_PlaysWordAndStore_ThenFireCastsSpellAndImpact()
        {
            yield return BindAtPlay();
            string word = _game.Words.Current;
            TestDataUtils.PlaceWord(_game, 0, word);
            yield return null;
            CollectionAssert.Contains(_played, SoundKey.WordComplete);
            CollectionAssert.Contains(_played, SoundKey.Store);
            CollectionAssert.DoesNotContain(_played, SoundKey.LetterMatch, "완성 프레임의 Match 연출은 글자 맞춤 소리가 아니다");

            BoardCell slot = _game.Boards[1].Slots[0];
            PlayerController player = _game.Player;
            player.Board = 1;
            player.R = slot.R;
            player.C = slot.C;
            yield return Settle();

            Assert.IsTrue(player.Fire());
            yield return null;
            SoundKey spellKey = SoundEventWatcher.GetSpellKey(_game.Data.GetSpellOrNull(word).Fx);
            CollectionAssert.AreEqual(new[] { spellKey }, _played);

            _played.Clear();
            yield return RunSession(_game.Config.SpellTime);
            CollectionAssert.Contains(_played, SoundKey.SpellImpact, "마법이 손에 맞는 순간");
        }

        [UnityTest]
        public IEnumerator SecondFireInWindow_PlaysCombo()
        {
            yield return BindAtPlay();
            BoardModel right = _game.Boards[1];
            right.Slots[0].Block = new AttackBlock { Word = "FIRE", Hp = 2 };
            right.Slots[1].Block = new AttackBlock { Word = "BOLT", Hp = 2 };
            PlayerController player = _game.Player;
            player.Board = 1;
            player.R = right.Slots[0].R;
            player.C = right.Slots[0].C;
            yield return Settle();

            player.Fire();
            yield return RunSession(20 * DT);
            _played.Clear();
            player.TryMove(MoveDir.Down);
            player.Fire();
            yield return null;
            CollectionAssert.Contains(_played, SoundKey.Combo);
            CollectionAssert.Contains(_played, SoundKey.SpellBolt);
        }

        [UnityTest]
        public IEnumerator Hurt_ThenLastHp_PlaysHurtAndDeath()
        {
            yield return BindAtPlay();
            PlayerController player = _game.Player;
            Assert.IsTrue(player.Hurt("test"));
            yield return null;
            CollectionAssert.AreEqual(new[] { SoundKey.PlayerHurt }, _played);

            _played.Clear();
            player.Invuln = 0f;
            player.Hp = 1;
            Assert.IsTrue(player.Hurt("test"));
            yield return null;
            CollectionAssert.Contains(_played, SoundKey.PlayerHurt);
            CollectionAssert.Contains(_played, SoundKey.Death);
        }

        [UnityTest]
        public IEnumerator HandWarn_AndClick_AreHeard()
        {
            yield return BindAtPlay();
            _game.Hand.State = HandState.Warn;
            yield return null;
            CollectionAssert.AreEqual(new[] { SoundKey.HandWarn }, _played);

            _played.Clear();
            yield return null;
            CollectionAssert.IsEmpty(_played, "예고는 Warn이 된 순간 한 번만");

            _game.Fx.Add(new FxEvent { Kind = FxKind.Click, T = 0.25f, Duration = 0.25f });
            _game.Fx.Add(new FxEvent { Kind = FxKind.Fizzle, T = 0.5f, Duration = 0.5f });
            yield return null;
            CollectionAssert.AreEquivalent(new[] { SoundKey.HandClick, SoundKey.BlockBreak }, _played);
        }

        [UnityTest]
        public IEnumerator Finisher_PlaysShift_ThenWinFanfareOnImpact()
        {
            _game.StartPhase(_game.Data.Phases.Count - 1);
            _manager.Bind(_game);
            yield return Settle();

            _game.CastSpell(_game.Data.FinisherWord, _game.Hand.Pos, 1f);
            yield return null;
            Assert.AreEqual(GameState.Win, _game.State);
            CollectionAssert.AreEqual(new[] { SoundKey.SpellShift }, _played, "팡파르는 시전 순간이 아니라 맞는 순간");

            _played.Clear();
            yield return RunSession(_game.Config.SpellTime);
            CollectionAssert.Contains(_played, SoundKey.SpellImpact);
            CollectionAssert.Contains(_played, SoundKey.Win);
        }

        [Test]
        public void Mute_SetsListenerVolume()
        {
            _manager.SetMuted(true);
            Assert.AreEqual(0f, AudioListener.volume);
            _manager.SetMuted(false);
            Assert.AreEqual(1f, AudioListener.volume);
        }

        private IEnumerator BindAtPlay()
        {
            _game.StartPhase(0);
            _manager.Bind(_game);
            yield return Settle();
        }

        // 테스트가 직접 바꾼 상태(플레이어 위치 등)를 한 프레임 읽히고 기록을 비운다
        private IEnumerator Settle()
        {
            yield return null;
            _played.Clear();
        }

        private IEnumerator RunSession(float seconds)
        {
            for (float t = 0f; t < seconds + DT; t += DT)
            {
                _game.Update(DT);
                yield return null;
            }
        }
    }
}
