using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 키 하나(Key 프리팹). 겉모양·글자 위치·겹쳐 그리는 그림은 프리팹에서 고친다.
    /// 프리팹 루트 위치 = 키 그림(16x20)의 왼쪽 아래. 위치와 글자는 BoardView가 매 프레임 정한다.
    /// </summary>
    public class KeyView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("돌릴 수 있는 줄 / 움직이는 키 하늘색 테(KeyRowBand)")]
        [SerializeField] private SpriteRenderer _band;
        [Tooltip("단어 글자 힌트(노란 바탕). 흰 점을 늘려서 씀")]
        [SerializeField] private SpriteRenderer _hint;
        [Tooltip("제자리에 맞은 글자 초록 테(KeyMatched)")]
        [SerializeField] private SpriteRenderer _matched;
        [SerializeField] private SpriteRenderer _letter;
        [SerializeField] private int _sortingOrder = RenderOrderUtils.KEY;
        [Tooltip("움직이는 키는 다른 키 위에 그린다")]
        [SerializeField] private int _movingSortingOffset = 10;
        [SerializeField, Range(0f, 1f)] private float _hintFillAlpha = 0.22f;

        public void Render(char letter, KeyLook look)
        {
            ArtCatalog catalog = ViewAssets.Instance.Catalog;
            ViewColors colors = ViewAssets.Instance.Colors;
            bool hasLetter = letter != '\0';
            int order = _sortingOrder + (look.IsMoving ? _movingSortingOffset : 0);
            var tint = new Color(1f, 1f, 1f, look.Alpha);

            _body.sprite = catalog.GetFrame("Keycap", hasLetter ? "Base" : "Blank", 0).Sprite;
            _body.color = tint;
            _body.sortingOrder = order;

            _band.enabled = look.IsMoving || look.IsOnBand;
            _band.sprite = catalog.GetFrame("KeyRowBand", "Band", look.IsMoving ? 1 : 0).Sprite;
            _band.color = tint;
            _band.sortingOrder = order + 1;

            bool isHinted = look.Hint > 0f && (look.IsOk || look.IsInWord);
            _hint.enabled = isHinted && !look.IsOk;
            _hint.color = colors.Glow.WithAlpha(_hintFillAlpha * look.Hint * look.Alpha);
            _hint.sortingOrder = order + 2;

            _matched.enabled = isHinted && look.IsOk;
            _matched.color = tint.WithAlpha(look.Alpha * look.Hint);
            _matched.sortingOrder = order + 3;

            ArtFrame glyph = default;
            bool hasGlyph = hasLetter && PixelFontUtils.TryGetGlyph(catalog, letter, out glyph);
            _letter.enabled = hasGlyph;
            if (hasGlyph)
            {
                _letter.sprite = glyph.Sprite;
                Color letterColor = colors.Text;
                if (isHinted)
                {
                    letterColor = Color.Lerp(colors.Text, look.IsOk ? colors.Ok : colors.Glow, look.Hint);
                }

                _letter.color = letterColor.WithAlpha(look.Alpha);
                _letter.sortingOrder = order + 4;
            }
        }
    }
}
