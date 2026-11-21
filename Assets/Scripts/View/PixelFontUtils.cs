using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 5x7 도트 글꼴(PixelFont A~Z, PixelSymbols 0~9와 기호). 소문자는 대문자로 그린다. 마침표는 글꼴에 없어서 점 하나로 그린다.
    /// WebGL에는 시스템 글꼴이 없어서 화면 글자는 모두 영어 대문자·숫자·기호로 쓴다.
    /// </summary>
    public static class PixelFontUtils
    {
        public const int GLYPH_W = 5;
        public const int GLYPH_H = 7;
        private const int ADVANCE = 6;
        private const int SPACE_ADVANCE = 4;
        private const int DOT_ADVANCE = 2;
        private const string SYMBOLS = "0123456789+-!?:/%×→←↑↓⇧";

        public static bool TryGetGlyph(ArtCatalog catalog, char ch, out ArtFrame frame)
        {
            char upper = char.ToUpperInvariant(ch);
            if (upper >= 'A' && upper <= 'Z')
            {
                frame = catalog.GetFrame("PixelFont", null, upper - 'A');
                return frame.IsValid;
            }

            int symbol = SYMBOLS.IndexOf(ch);
            if (symbol >= 0)
            {
                frame = catalog.GetFrame("PixelSymbols", null, symbol);
                return frame.IsValid;
            }

            frame = default;
            return false;
        }

        public static int GetAdvance(char ch)
        {
            switch (ch)
            {
                case ' ':
                    return SPACE_ADVANCE;
                case '.':
                    return DOT_ADVANCE;
                default:
                    return ADVANCE;
            }
        }

        /// <summary>글자 줄 너비(px, 배율 적용)</summary>
        public static float Measure(string text, float scale = 1f)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            float width = 0f;
            foreach (char ch in text)
            {
                width += GetAdvance(ch) * scale;
            }

            return Mathf.Max(0f, width - scale);
        }

        /// <summary>정렬에 맞춘 첫 글자 왼쪽 x(px)</summary>
        public static float GetStartX(float anchorX, float width, TextAlign align)
        {
            switch (align)
            {
                case TextAlign.Center:
                    return Mathf.Round(anchorX - width * 0.5f);
                case TextAlign.Right:
                    return anchorX - width;
                default:
                    return anchorX;
            }
        }
    }
}
