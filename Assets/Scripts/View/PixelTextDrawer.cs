using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>즉시 그리기 캔버스(PixelCanvas)에 도트 글꼴로 글자를 쓴다(떠오르는 숫자처럼 매번 바뀌는 글자용).</summary>
    public class PixelTextDrawer
    {
        private readonly ArtCatalog _catalog;
        private PixelCanvas _canvas;

        public PixelTextDrawer(PixelCanvas canvas, ArtCatalog catalog)
        {
            _canvas = canvas;
            _catalog = catalog;
        }

        /// <summary>그릴 캔버스를 바꾼다(앞뒤 순서가 다른 캔버스에 같은 글꼴로 쓸 때)</summary>
        public PixelCanvas Canvas
        {
            get => _canvas;
            set => _canvas = value;
        }

        public float Measure(string text, float scale = 1f)
        {
            return PixelFontUtils.Measure(text, scale);
        }

        public bool TryGetGlyph(char ch, out ArtFrame frame)
        {
            return PixelFontUtils.TryGetGlyph(_catalog, ch, out frame);
        }

        /// <summary>(x, y) = 글자 줄의 기준점(왼쪽·가운데·오른쪽) 위쪽. 그림자를 주면 1px 아래·오른쪽에 먼저 그린다.</summary>
        public void Draw(string text, float x, float y, Color color, TextAlign align = TextAlign.Left, float scale = 1f, Color? shadow = null)
        {
            if (string.IsNullOrEmpty(text) || color.a <= 0f)
            {
                return;
            }

            float startX = PixelFontUtils.GetStartX(x, Measure(text, scale), align);
            if (shadow.HasValue)
            {
                DrawRun(text, startX + scale, y + scale, shadow.Value, scale);
            }

            DrawRun(text, startX, y, color, scale);
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
                else if (ch == '.')
                {
                    _canvas.DrawRect(cursor, y + (PixelFontUtils.GLYPH_H - 1) * scale, scale, scale, color);
                }

                cursor += PixelFontUtils.GetAdvance(ch) * scale;
            }
        }
    }
}
