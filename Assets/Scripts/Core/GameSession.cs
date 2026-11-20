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
        public readonly HandController Hand;
        public readonly WordSystem Words;
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

        // 단어 위치 힌트 점멸 기준 시각(새 단어가 나오면 다시 센다)
        public float HintT0;

        // 마지막으로 슬롯을 발동한 판·시각·콤보 단계(같은 판을 comboWindow초 안에 연달아 쏘면 콤보)
        public int LastFireBoard = -1;
        public float LastFireTime;
        public int LastFireCombo;

        // 마법이 손에 닿는 순간까지 남은 시간(닿으면 손이 움찔하고 화면이 흔들림)
        private float _spellHitT;

        public GameSession(AttackPatternsData data, BoardLayoutData layout, int seed)
        {
            Data = data;
            Random = new GameRandom(seed);
            Boards = new[] { new BoardModel(0, layout), new BoardModel(1, layout) };
            Player = new PlayerController(this);
            Hand = new HandController(this);
            Words = new WordSystem(this);
        }

        public ConfigValues Config => Data.Config;
        public PhaseDef CurrentPhase => Data.Phases[Phase];
        public bool IsLastPhase => Phase == Data.Phases.Count - 1;

        /// <summary>0~1: 단어 위치 힌트 세기. hintInterval초마다 hintDuration초 동안 두 번 깜빡이고, 나머지 시간에는 0.</summary>
        public float HintAlpha()
        {
            if (Words.Current == null)
            {
                return 0f;
            }

            float local = Mathf.Repeat(Time - HintT0, Config.HintInterval);
            if (local > Config.HintDuration)
            {
                return 0f;
            }

            return Mathf.Sin(Mathf.Repeat(local / Config.HintDuration * 2f, 1f) * Mathf.PI);
        }

        /// <summary>페이즈 시작(판도 시작 배치로). handHp를 주면 손 HP를 그 값으로 시작한다(사망 후 재시작).</summary>
        public void StartPhase(int phase, int? handHp = null)
        {
            Phase = phase;
            foreach (BoardModel board in Boards)
            {
                board.Reset();
            }

            Player.Reset();
            Hand.Reset();
            if (handHp.HasValue)
            {
                Hand.Hp = handHp.Value;
            }

            Words.Reset();
            Words.Refresh();
            Fx.Clear();
            LastFireBoard = -1;
            _spellHitT = 0f;
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

        /// <summary>판이 바뀔 때마다(플레이어·손의 줄 돌리기) 부른다. 손이 섞다가 우연히 맞춰진 단어도 완성으로 친다.</summary>
        public void OnBoardChanged()
        {
            Words.Check();
        }

        /// <summary>
        /// 단어 완성. 보통 단어는 바로 쏘지 않고 공격 블록이 되어 반대편 판의 빈 슬롯에 들어간다(양쪽 판을 오가게 만드는 장치).
        /// 반대편 슬롯이 꽉 찼으면 바로 발동하되 피해는 overflowMul. 마무리 단어는 슬롯을 거치지 않고 바로 발동.
        /// </summary>
        public void OnWordComplete(string word, AlignResult align)
        {
            Stats.WordsDone++;
            Vector2 from = CenterOf(align.Board, align.Cells);
            Fx.Add(new FxEvent { Kind = FxKind.Match, Board = align.Board, Cells = FxEvent.ToCellList(align.Cells), T = 0.6f, Duration = 0.6f });
            if (word == Data.FinisherWord)
            {
                CastSpell(word, from, 1f);
                return;
            }

            BoardModel target = Boards[1 - align.Board];
            BoardCell slot = target.EmptySlotOrNull();
            if (slot == null)
            {
                ShowToast("SLOTS FULL: CAST NOW (80%)");
                CastSpell(word, from, Config.OverflowMul);
                return;
            }

            slot.Block = new AttackBlock { Word = word, Hp = Config.BlockHp };
            Stats.Stored++;
            SpellDef spell = Data.GetSpellOrNull(word);
            Fx.Add(new FxEvent
            {
                Kind = FxKind.Store,
                Word = word,
                Color = spell != null ? spell.Color : PaletteUtils.Glow,
                From = from,
                To = GeometryUtils.CellCenter(target.Index, slot.R, slot.C),
                T = Config.StoreTime,
                Duration = Config.StoreTime,
            });
            ShowToast($"{word} → {(target.Index == 0 ? "LEFT" : "RIGHT")} SLOT");
        }

        /// <summary>슬롯에서 Space: 블록 발동. 같은 판 슬롯을 comboWindow초 안에 연달아 쏘면 콤보 보너스.</summary>
        public bool FireSlot(BoardModel board, BoardCell slot)
        {
            AttackBlock block = slot.Block;
            if (block == null || State != GameState.Play)
            {
                return false;
            }

            slot.Block = null;
            int combo = IsComboOpen(board.Index) ? LastFireCombo + 1 : 0;
            LastFireBoard = board.Index;
            LastFireTime = Time;
            LastFireCombo = combo;
            if (combo > 0)
            {
                ShowToast($"COMBO ×{combo + 1}! +{Mathf.RoundToInt(Config.ComboBonus * combo * 100f)}%");
                Stats.Combos++;
            }

            CastSpell(block.Word, GeometryUtils.CellCenter(board.Index, slot.R, slot.C), 1f + Config.ComboBonus * combo);
            return true;
        }

        /// <summary>그 판 슬롯을 지금 쏘면 콤보가 되는가</summary>
        public bool IsComboOpen(int board)
        {
            return LastFireBoard == board && Time - LastFireTime <= Config.ComboWindow;
        }

        /// <summary>마법 발동: 피해 = 글자 수 × 배율(반올림, 최소 1). 손은 맞은 뒤 잠깐 멈춘다. 준 피해를 돌려준다.</summary>
        public int CastSpell(string word, Vector2 from, float multiplier)
        {
            bool isFinisher = word == Data.FinisherWord;
            SpellDef spell = Data.GetSpellOrNull(word);
            int damage = Mathf.Max(1, Mathf.RoundToInt(word.Length * multiplier));

            // 마지막 페이즈에서는 마무리 단어로만 쓰러진다: 다른 단어는 손 HP를 1 아래로 못 내린다
            if (IsLastPhase && !isFinisher)
            {
                damage = Mathf.Min(damage, Mathf.Max(0, Hand.Hp - 1));
            }

            Hand.Hp = isFinisher ? 0 : Mathf.Max(0, Hand.Hp - damage);
            Stats.Casts++;
            float duration = Config.SpellTime;
            Color color = spell != null ? spell.Color : PaletteUtils.Glow;
            Fx.Add(new FxEvent
            {
                Kind = FxKind.Spell,
                Style = spell != null ? spell.Fx : "beam",
                Word = word,
                Color = color,
                Count = spell != null ? Mathf.Max(1, spell.Count) : 1,
                Size = spell != null ? spell.Size : 10f,
                From = from,
                T = duration,
                Duration = duration,
                Seed = Random.Range(0f, 1000f),
            });
            Fx.Add(new FxEvent
            {
                Kind = FxKind.Float,
                From = Hand.Pos - new Vector2(0f, 1f),
                Text = isFinisher ? "SHIFT!" : $"{word} -{damage}",
                Color = color,
                T = 1.3f + duration * 0.55f,
                Duration = 1.3f,
                Delay = duration * 0.55f,
            });
            _spellHitT = duration * 0.55f;
            if (Hand.Hp <= 0)
            {
                State = IsLastPhase ? GameState.Win : GameState.PhaseClear;
                StateT = IsLastPhase ? 0f : 2f;
                return damage;
            }

            Hand.Stun(Config.SpellStun + duration * 0.5f);
            Words.Refresh();
            return damage;
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

            if (_spellHitT > 0f && (_spellHitT -= dt) <= 0f)
            {
                Shake = Mathf.Max(Shake, 0.25f);
                Hand.SetAnim(HandController.ANIM_HURT);
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
                    UpdatePlay(dt);
                    break;
                default:
                    break;
            }
        }

        private void UpdatePlay(float dt)
        {
            foreach (BoardModel board in Boards)
            {
                board.Update(dt);
            }

            Player.Update(dt);
            if (State == GameState.Play)
            {
                Hand.Update(dt);
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

        private static Vector2 CenterOf(int board, List<BoardCell> cells)
        {
            Vector2 sum = Vector2.zero;
            foreach (BoardCell cell in cells)
            {
                sum += GeometryUtils.CellCenter(board, cell.R, cell.C);
            }

            return cells.Count > 0 ? sum / cells.Count : sum;
        }
    }
}
