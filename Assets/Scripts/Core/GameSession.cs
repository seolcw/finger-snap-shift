using System.Collections.Generic;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 게임 한 판의 상태와 진행(웹의 Game 클래스에서 렌더를 뺀 부분). MonoBehaviour 없이 Update(dt)로 돌아간다.
    /// </summary>
    public class GameSession
    {
        public readonly AttackPatternsData Data;
        public readonly BoardModel[] Boards;
        public readonly PlayerController Player;
        public readonly GameRandom Random;
        public readonly List<FxEvent> Fx = new List<FxEvent>();

        public GameStats Stats = new GameStats();
        public int Phase;
        public GameState State = GameState.Ready;
        public float Time;
        public float StateT;
        public float Shake;
        public TimedText Toast;
        public TimedText Banner;
        public bool IsInvincible;
        public bool IsPaused;

        public GameSession(AttackPatternsData data, BoardLayoutData layout, int seed)
        {
            Data = data;
            Random = new GameRandom(seed);
            Boards = new[] { new BoardModel(0, layout), new BoardModel(1, layout) };
            Player = new PlayerController(this);
        }

        public ConfigValues Config => Data.Config;
        public PhaseDef CurrentPhase => Data.Phases[Phase];
        public bool IsLastPhase => Phase == Data.Phases.Count - 1;

        /// <summary>페이즈 시작(판도 시작 배치로).</summary>
        public void StartPhase(int phase)
        {
            Phase = phase;
            foreach (BoardModel board in Boards)
            {
                board.Reset();
            }

            Player.Reset();
            Fx.Clear();
            State = GameState.Play;
            IsPaused = false;
            Banner = new TimedText($"PHASE {phase + 1}", 1.4f);
        }

        /// <summary>R: 현재 페이즈를 처음부터. 승리 화면에서는 1페이즈부터.</summary>
        public void RestartPhase()
        {
            StartPhase(State == GameState.Win ? 0 : Phase);
        }

        public void ShowToast(string text)
        {
            Toast = new TimedText(text, 1.8f);
        }

        public void OnPlayerHurt(string source)
        {
            GameStats.Increment(Stats.Hits, source);
            Shake = 0.3f;
            Vector2 center = GeometryUtils.CellCenter(Player.Board, Player.R, Player.C);
            Fx.Add(new FxEvent { Kind = FxKind.Float, From = center - new Vector2(0f, 0.4f), Text = "-1", Color = PaletteUtils.Hp, T = 0.9f, Duration = 0.9f });
            if (Player.Hp <= 0)
            {
                State = GameState.Dead;
                StateT = 1.3f;
                Stats.Deaths++;
            }
        }

        public void Update(float dt)
        {
            if (IsPaused)
            {
                return;
            }

            Time += dt;
            UpdateFx(dt);
            Shake = Mathf.Max(0f, Shake - dt);
            if (Toast != null && (Toast.T -= dt) <= 0f)
            {
                Toast = null;
            }

            if (Banner != null && (Banner.T -= dt) <= 0f)
            {
                Banner = null;
            }

            switch (State)
            {
                case GameState.Win:
                    StateT += dt;
                    break;
                case GameState.Dead:
                    StateT -= dt;
                    if (StateT <= 0f)
                    {
                        StartPhase(Phase);
                    }

                    break;
                case GameState.Play:
                    foreach (BoardModel board in Boards)
                    {
                        board.Update(dt);
                    }

                    Player.Update(dt);
                    break;
                default:
                    break;
            }
        }

        private void UpdateFx(float dt)
        {
            for (int i = Fx.Count - 1; i >= 0; i--)
            {
                FxEvent fx = Fx[i];
                fx.T -= dt;
                fx.Age += dt;
                if (fx.T <= 0f)
                {
                    Fx.RemoveAt(i);
                }
            }
        }
    }
}
