using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 5x7 도트 글꼴(PixelFont A~Z, PixelSymbols 0~9와 기호)로 글자를 그린다. 소문자는 대문자로 그린다.
    /// WebGL에는 시스템 글꼴이 없어서 화면 글자는 모두 영어 대문자·숫자·기호로 쓴다.
    /// </summary>
    public class PixelTextDrawer
    {
        public const int GLYPH_W = 5;
        public const int GLYPH_H = 7;
        private const int ADVANCE = 6;
        private const int SPACE_ADVANCE = 4;
        private const string SYMBOLS = "0123456789+-!?:/%×→←↑↓⇧";

        private readonly PixelCanvas _canvas;
        private readonly ArtCatalog _catalog;

        public PixelTextDrawer(PixelCanvas canvas, ArtCatalog catalog)
        {
            _canvas = canvas;
            _catalog = catalog;
        }

        public float Measure(string text, float scale = 1f)
        {
            float width = 0f;
            foreach (char ch in text)
            {
                width += (ch == ' ' ? SPACE_ADVANCE : ADVANCE) * scale;
            }

            return Mathf.Max(0f, width - scale);
        }

        /// <summary>(x, y) = 글자 줄의 기준점(왼쪽·가운데·오른쪽) 위쪽. 그림자를 주면 1px 아래·오른쪽에 먼저 그린다.</summary>
        public void Draw(string text, float x, float y, Color color, TextAlign align = TextAlign.Left, float scale = 1f, Color? shadow = null)
        {
            if (string.IsNullOrEmpty(text) || color.a <= 0f)
            {
                return;
            }

            float width = Measure(text, scale);
            float startX = align == TextAlign.Center ? Mathf.Round(x - width * 0.5f) : align == TextAlign.Right ? x - width : x;
            if (shadow.HasValue)
            {
                DrawRun(text, startX + scale, y + scale, shadow.Value, scale);
            }

            DrawRun(text, startX, y, color, scale);
        }

        public bool TryGetGlyph(char ch, out ArtFrame frame)
        {
            char upper = char.ToUpperInvariant(ch);
            if (upper >= 'A' && upper <= 'Z')
            {
                frame = _catalog.GetFrame("PixelFont", null, upper - 'A');
                return frame.IsValid;
            }

            int symbol = SYMBOLS.IndexOf(ch);
            if (symbol >= 0)
            {
                frame = _catalog.GetFrame("PixelSymbols", null, symbol);
                return frame.IsValid;
            }

            frame = default;
            return false;
        }

        private void DrawRun(string text, float x, float y, Color color, float scale)
        {
            float cursor = x;
            foreach (char ch in text)
            {
                if (TryGetGlyph(ch, out ArtFrame frame))
                {
                    _canvas.DrawFrame(frame, cursor, y, color, scale);
                }

                cursor += (ch == ' ' ? SPACE_ADVANCE : ADVANCE) * scale;
            }
        }
    }
}
