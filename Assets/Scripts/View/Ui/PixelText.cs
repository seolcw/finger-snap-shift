using System.Collections.Generic;
using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 도트 글꼴 글자 한 줄. 에디터에서도 바로 보이고, 문구·색·정렬·크기는 인스펙터에서 고친다.
    /// 오브젝트 위치 = 글자 줄의 위쪽 기준점(왼쪽·가운데·오른쪽 정렬에 따라). 문구에 {0} 같은 자리가 있으면 코드가 값을 채운다.
    /// 글자 스프라이트는 숨은 자식으로 만들고 씬에 저장하지 않는다.
    /// </summary>
    [ExecuteAlways]
    public class PixelText : MonoBehaviour
    {
        [SerializeField, TextArea(1, 3)] private string _text = "TEXT";
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private TextAlign _align = TextAlign.Left;
        [SerializeField, Min(1)] private int _scale = 1;
        [SerializeField] private bool _hasShadow;
        [SerializeField] private Color _shadowColor = Color.black;
        [SerializeField] private int _sortingOrder = RenderOrderUtils.HUD;

        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        private string _display;
        private Color[] _charColors;
        private float _alpha = 1f;
        private bool _isDirty = true;

        /// <summary>인스펙터의 문구(값 자리 {0} 포함)</summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value || _display != null)
                {
                    _text = value;
                    _display = null;
                    _isDirty = true;
                }
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    _isDirty = true;
                }
            }
        }

        public float Alpha
        {
            get => _alpha;
            set
            {
                if (!Mathf.Approximately(_alpha, value))
                {
                    _alpha = value;
                    _isDirty = true;
                }
            }
        }

        public float Width => PixelFontUtils.Measure(DisplayText, _scale);

        private string DisplayText => _display ?? _text;

        /// <summary>문구의 {0}, {1}… 자리에 값을 넣어 보여 준다</summary>
        public void SetArgs(params object[] args)
        {
            string formatted = string.Format(_text, args);
            if (formatted != _display)
            {
                _display = formatted;
                _isDirty = true;
            }
        }

        /// <summary>인스펙터 문구 대신 이 글자를 보여 준다(문구 자체는 바꾸지 않음)</summary>
        public void SetDisplay(string text)
        {
            if (text != _display)
            {
                _display = text;
                _isDirty = true;
            }
        }

        /// <summary>글자마다 색(null이면 전체 색)</summary>
        public void SetCharColors(Color[] colors)
        {
            _charColors = colors;
            _isDirty = true;
        }

        private void OnEnable()
        {
            _isDirty = true;
        }

        private void OnDisable()
        {
            ClearRenderers();
        }

        private void OnValidate()
        {
            _display = null;
            _isDirty = true;
        }

        private void LateUpdate()
        {
            if (_isDirty)
            {
                Rebuild();
            }
        }

        private void Rebuild()
        {
            _isDirty = false;
            ViewAssets assets = ViewAssets.Instance;
            if (assets == null || assets.Catalog == null)
            {
                return;
            }

            string text = DisplayText ?? string.Empty;
            float width = PixelFontUtils.Measure(text, _scale);
            float cursor = PixelFontUtils.GetStartX(0f, width, _align);
            int used = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                Color baseColor = _charColors != null && i < _charColors.Length ? _charColors[i] : _color;
                Color color = baseColor.WithAlpha(baseColor.a * _alpha);
                Sprite sprite = null;
                Vector2 size = Vector2.one * _scale;
                float bottom = -PixelFontUtils.GLYPH_H * _scale;
                if (PixelFontUtils.TryGetGlyph(assets.Catalog, ch, out ArtFrame glyph))
                {
                    sprite = glyph.Sprite;
                }
                else if (ch == '.')
                {
                    sprite = assets.WhitePixel;
                }

                if (sprite != null)
                {
                    if (_hasShadow)
                    {
                        Place(used++, sprite, new Vector2(cursor + _scale, bottom - _scale), size, _shadowColor.WithAlpha(_shadowColor.a * _alpha), _sortingOrder);
                    }

                    Place(used++, sprite, new Vector2(cursor, bottom), size, color, _sortingOrder + 1);
                }

                cursor += PixelFontUtils.GetAdvance(ch) * _scale;
            }

            for (int i = used; i < _renderers.Count; i++)
            {
                _renderers[i].enabled = false;
            }
        }

        // positionPx: 기준점에서 글자 왼쪽 아래까지(px, y 위가 +)
        private void Place(int index, Sprite sprite, Vector2 positionPx, Vector2 scale, Color color, int order)
        {
            while (_renderers.Count <= index)
            {
                var child = new GameObject("Glyph") { hideFlags = HideFlags.HideAndDontSave };
                child.transform.SetParent(transform, false);
                _renderers.Add(child.AddComponent<SpriteRenderer>());
            }

            SpriteRenderer renderer = _renderers[index];
            renderer.enabled = true;
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            renderer.transform.localPosition = new Vector3(positionPx.x, positionPx.y, 0f) / ViewUtils.PPU;
            renderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        private void ClearRenderers()
        {
            foreach (SpriteRenderer renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(renderer.gameObject);
                }
                else
                {
                    DestroyImmediate(renderer.gameObject);
                }
            }

            _renderers.Clear();
        }
    }
}
