using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 화면 위 목표 단어 판: 마법 아이콘 + 단어(가장 많이 맞춘 줄에서 제자리에 온 글자는 초록) + 진행도 + 다음 단어.
    /// 어느 줄인지는 알려 주지 않는다. 오브젝트 위치 = 판 위쪽 가운데. 단어 길이에 맞춰 판 너비가 바뀐다.
    /// </summary>
    public class WordPanelView : MonoBehaviour
    {
        [SerializeField] private NineSliceSprite _panel;
        [SerializeField] private SpriteRenderer _icon;
        [SerializeField] private PixelText _word;
        [SerializeField] private PixelText _progress;
        [Tooltip("진행도 문구. {0} = 제자리 글자 수, {1} = 단어 길이")]
        [SerializeField] private string _progressFormat = "{0}/{1} LETTERS LINED UP";
        [Tooltip("{0} = 다음 단어")]
        [SerializeField] private PixelText _next;
        [SerializeField] private string _finisherText = "FINISHER";

        [Header("판 안 여백(px)")]
        [SerializeField] private float _paddingLeft = 4f;
        [SerializeField] private float _iconGap = 4f;
        [SerializeField] private float _progressGap = 6f;
        [SerializeField] private float _paddingRight = 5f;
        [SerializeField] private float _height = 16f;
        [SerializeField] private float _textTop = 5f;

        public void Render(GameSession game)
        {
            string word = game.Words.Current;
            bool isVisible = !string.IsNullOrEmpty(word);
            gameObject.SetActive(isVisible);
            if (!isVisible)
            {
                return;
            }

            ViewColors colors = ViewAssets.Instance.Colors;
            ArtCatalog catalog = ViewAssets.Instance.Catalog;
            AttackPatternsData data = game.Data;
            AlignResult best = game.Words.BestOrNull();
            BoardModel bestBoard = best != null ? game.Boards[best.Board] : null;
            var charColors = new Color[word.Length];
            for (int i = 0; i < word.Length; i++)
            {
                bool isOk = bestBoard != null && i < best.Cells.Count && bestBoard.LetterAt(best.Cells[i].R, best.Cells[i].C) == word[i];
                charColors[i] = isOk ? colors.Ok : colors.Text;
            }

            _word.SetDisplay(word);
            _word.SetCharColors(charColors);
            string progress = string.Format(_progressFormat, best != null ? best.Count : 0, word.Length);
            _progress.SetDisplay(progress);
            _progress.Color = game.HintAlpha() > 0f ? colors.Glow : colors.Dim;

            float iconWidth = catalog.GetFrame("SpellIcon", null, 0).CanvasSize.x;
            float width = _paddingLeft + iconWidth + _iconGap + PixelFontUtils.Measure(word) + _progressGap + PixelFontUtils.Measure(progress) + _paddingRight;
            float left = -Mathf.Round(width * 0.5f);
            _panel.transform.localPosition = new Vector3(left, 0f, 0f) / ViewUtils.PPU;
            _panel.SizePx = new Vector2(width, _height);
            SpellDef spell = data.GetSpellOrNull(word);
            _icon.sprite = catalog.GetFrame("SpellIcon", spell?.Icon, 0).Sprite;
            _icon.transform.localPosition = new Vector3(left + _paddingLeft, -2f - iconWidth, 0f) / ViewUtils.PPU;
            float wordX = left + _paddingLeft + iconWidth + _iconGap;
            _word.transform.localPosition = new Vector3(wordX, -_textTop, 0f) / ViewUtils.PPU;
            _progress.transform.localPosition = new Vector3(wordX + PixelFontUtils.Measure(word) + _progressGap, -_textTop, 0f) / ViewUtils.PPU;

            if (game.Words.Next != null)
            {
                _next.SetArgs(game.Words.Next);
                _next.Color = colors.Dim;
            }
            else
            {
                _next.SetDisplay(_finisherText);
                _next.Color = colors.Glow;
            }
        }
    }
}
