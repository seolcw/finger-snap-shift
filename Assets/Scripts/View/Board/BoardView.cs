using System.Collections.Generic;
using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 키보드 한 판(Board_Left / Board_Right). 칸 오브젝트(빈 홈, SHIFT 칸, 슬롯, 키)는 씬에 실제로 있고,
    /// 마스크가 바뀌었을 때만 인스펙터의 "Rebuild Cells"(오른쪽 위 ⋮ 메뉴)로 다시 만든다.
    /// 위치는 ViewAssets의 판 위치·칸 크기로 매 프레임 맞춘다. 에디터에서는 시작 배치 글자를 미리 보여 준다.
    /// </summary>
    [ExecuteAlways]
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private int _boardIndex;
        [SerializeField] private BoardLayoutData _layoutData;

        [Header("프리팹 (Rebuild Cells 때 사용)")]
        [SerializeField] private KeyView _keyPrefab;
        [SerializeField] private GameObject _homePrefab;
        [SerializeField] private ShiftKeyView _shiftKeyPrefab;
        [SerializeField] private SlotView _slotPrefab;

        [Header("만들어진 칸 (자동)")]
        [SerializeField] private List<KeyView> _keys = new List<KeyView>();
        [SerializeField] private List<GameObject> _homes = new List<GameObject>();
        [SerializeField] private ShiftKeyView _shiftKey;
        [SerializeField] private List<SlotView> _slots = new List<SlotView>();

        private static readonly (float Lag, float Alpha)[] GhostTrail = { (0.07f, 0.28f), (0.035f, 0.45f) };
        private BoardModel _previewModel;

        public int BoardIndex => _boardIndex;

        private void Update()
        {
            if (Application.isPlaying || _layoutData == null || ViewAssets.Instance == null)
            {
                return;
            }

            // 에디터 미리보기: 시작 배치
            _previewModel ??= new BoardModel(_boardIndex, _layoutData);
            RenderModel(_previewModel, null);
        }

        /// <summary>플레이 중 매 프레임. 잔상·점선은 under 캔버스에 그린다.</summary>
        public void Render(GameSession game, PixelCanvas underCanvas)
        {
            RenderModel(game.Boards[_boardIndex], game);
            if (underCanvas != null)
            {
                DrawGhostsAndGuide(game, underCanvas);
            }
        }

        private void RenderModel(BoardModel board, GameSession game)
        {
            int homeIndex = 0;
            foreach (BoardCell cell in board.AllCells)
            {
                Vector3 position = ViewUtils.TopLeftToWorld(ViewUtils.CellTopLeft(_boardIndex, cell.R, cell.C), ViewUtils.CellH);
                if (cell.IsSlot)
                {
                    continue;
                }

                if (cell.IsShift)
                {
                    if (_shiftKey != null)
                    {
                        _shiftKey.transform.localPosition = position;
                        float ratio = game != null && game.Player.ShiftCd > 0f ? game.Player.ShiftCd / game.Config.ShiftCooldown : 0f;
                        _shiftKey.Render(ratio);
                    }

                    continue;
                }

                if (homeIndex < _homes.Count && _homes[homeIndex] != null)
                {
                    _homes[homeIndex].transform.localPosition = position;
                }

                homeIndex++;
            }

            for (int i = 0; i < board.Slots.Count && i < _slots.Count; i++)
            {
                BoardCell slot = board.Slots[i];
                if (_slots[i] == null)
                {
                    continue;
                }

                _slots[i].transform.localPosition = ViewUtils.TopLeftToWorld(ViewUtils.CellTopLeft(_boardIndex, slot.R, slot.C), ViewUtils.CellH);
                bool isActive = game != null && game.Player.Board == _boardIndex && slot.Is(game.Player.R, game.Player.C);
                _slots[i].Render(slot, isActive, game != null ? game.Time : 0f, game?.Data);
            }

            RenderKeys(board, game);
        }

        private void RenderKeys(BoardModel board, GameSession game)
        {
            string word = game?.Words.Current ?? string.Empty;
            float hint = game != null && game.State == GameState.Play ? game.HintAlpha() : 0f;
            HashSet<BoardCell> okCells = GetOkCells(board, game, word);
            PlayerController player = game?.Player;
            bool isShowingBand = player != null && player.IsShiftHeld && player.Board == _boardIndex && game.State == GameState.Play
                && player.Here != null && !player.Here.IsShift && !player.Here.IsSlot;

            foreach (BoardCell cell in board.KeyCells())
            {
                KeyTile key = cell.Key;
                if (key.Id >= _keys.Count || _keys[key.Id] == null)
                {
                    continue;
                }

                Vector3 offset = SlideAnim.GetOffset(key.Slide);
                Vector2 topLeft = ViewUtils.CellTopLeft(_boardIndex, cell.R, cell.C) + ViewUtils.ToPixelDelta(offset);
                KeyView view = _keys[key.Id];
                view.transform.localPosition = ViewUtils.TopLeftToWorld(topLeft, ViewUtils.CellH);
                view.Render(key.Letter, new KeyLook
                {
                    IsMoving = offset.x != 0f || offset.y != 0f,
                    IsOnBand = isShowingBand && (cell.R == player.R || cell.C == player.C),
                    IsOk = okCells.Contains(cell),
                    IsInWord = key.HasLetter && word.IndexOf(key.Letter) >= 0,
                    Hint = hint,
                    Alpha = offset.z,
                });
            }
        }

        private HashSet<BoardCell> GetOkCells(BoardModel board, GameSession game, string word)
        {
            var okCells = new HashSet<BoardCell>();
            AlignResult best = game?.Words.BestOrNull();
            if (best == null || best.Board != _boardIndex || best.Count == 0)
            {
                return okCells;
            }

            for (int i = 0; i < best.Cells.Count; i++)
            {
                if (board.LetterAt(best.Cells[i].R, best.Cells[i].C) == word[i])
                {
                    okCells.Add(best.Cells[i]);
                }
            }

            return okCells;
        }

        // 줄 돌리기 잔상(키 뒤 옅은 두 장)과 단어 자리 노란 점선(점멸할 때만)
        private void DrawGhostsAndGuide(GameSession game, PixelCanvas canvas)
        {
            BoardModel board = game.Boards[_boardIndex];
            ViewColors colors = ViewAssets.Instance.Colors;
            foreach (BoardCell cell in board.KeyCells())
            {
                SlideAnim slide = cell.Key.Slide;
                if (slide == null || slide.T < slide.Delay)
                {
                    continue;
                }

                Vector2 home = ViewUtils.CellTopLeft(_boardIndex, cell.R, cell.C);
                foreach ((float lag, float alpha) in GhostTrail)
                {
                    Vector3 ghost = slide.GetOffsetAt(slide.T - lag);
                    Vector2 position = home + ViewUtils.ToPixelDelta(ghost);
                    canvas.DrawRect(position.x + 3f, position.y + 3f, ViewUtils.CellW - 6f, ViewUtils.CellW - 6f, colors.ShiftHi.WithAlpha(alpha * ghost.z));
                }
            }

            float hint = game.State == GameState.Play ? game.HintAlpha() : 0f;
            AlignResult best = game.Words.BestOrNull();
            if (hint <= 0f || best == null || best.Board != _boardIndex || best.Count == 0)
            {
                return;
            }

            foreach (BoardCell cell in best.Cells)
            {
                Vector2 topLeft = ViewUtils.CellTopLeft(_boardIndex, cell.R, cell.C);
                canvas.DrawDashedRect(topLeft.x - 1f, topLeft.y - 1f, ViewUtils.CellW + 2f, ViewUtils.CellH + 1f, 2f, colors.Glow.WithAlpha(0.85f * hint));
            }
        }

#if UNITY_EDITOR
        /// <summary>마스크(BoardLayoutData)대로 칸 오브젝트를 다시 만든다. 프리팹 연결은 유지한다.</summary>
        [ContextMenu("Rebuild Cells")]
        public void RebuildCells()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            _keys.Clear();
            _homes.Clear();
            _slots.Clear();
            _shiftKey = null;
            var model = new BoardModel(_boardIndex, _layoutData);
            var keyCells = new List<BoardCell>(model.KeyCells());
            foreach (BoardCell cell in model.AllCells)
            {
                if (cell.IsSlot)
                {
                    _slots.Add(Spawn(_slotPrefab, $"Slot_{cell.R}_{cell.C}"));
                }
                else if (cell.IsShift)
                {
                    _shiftKey = Spawn(_shiftKeyPrefab, "ShiftKey");
                }
                else
                {
                    _homes.Add(Spawn(_homePrefab.transform, $"Home_{cell.R}_{cell.C}").gameObject);
                }
            }

            keyCells.Sort((a, b) => a.Key.Id.CompareTo(b.Key.Id));
            foreach (BoardCell cell in keyCells)
            {
                _keys.Add(Spawn(_keyPrefab, $"Key_{cell.Key.Id:00}"));
            }

            _previewModel = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private T Spawn<T>(T prefab, string objectName) where T : Component
        {
            var instance = (T)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
            instance.name = objectName;
            return instance;
        }

        private void OnValidate()
        {
            _previewModel = null;
        }
#endif
    }
}
