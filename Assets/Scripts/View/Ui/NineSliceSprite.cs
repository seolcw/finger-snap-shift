using System.Collections.Generic;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 9-slice 판(Ui/Panel 등): 테두리는 그대로 두고 가운데만 늘려 Size(px)를 채운다. 에디터에서도 보인다.
    /// 오브젝트 위치 = 판의 왼쪽 위.
    /// </summary>
    [ExecuteAlways]
    public class NineSliceSprite : MonoBehaviour
    {
        private static readonly Dictionary<(Sprite, int), Sprite> SliceCache = new Dictionary<(Sprite, int), Sprite>();

        [SerializeField] private string _sheet = "Panel";
        [SerializeField] private string _tag = "Panel";
        [SerializeField] private Vector2 _sizePx = new Vector2(48f, 16f);
        [SerializeField, Min(0)] private int _borderPx = 3;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private int _sortingOrder = RenderOrderUtils.HUD;

        private readonly HiddenRendererPool _pool = new HiddenRendererPool();
        private bool _isDirty = true;

        public Vector2 SizePx
        {
            get => _sizePx;
            set
            {
                if (_sizePx != value)
                {
                    _sizePx = value;
                    _isDirty = true;
                }
            }
        }

        private void OnEnable()
        {
            _isDirty = true;
        }

        private void OnDisable()
        {
            _pool.Clear();
        }

        private void OnValidate()
        {
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
            ArtCatalog catalog = ViewAssets.Instance != null ? ViewAssets.Instance.Catalog : null;
            ArtFrame frame = catalog != null ? catalog.GetFrame(_sheet, _tag, 0) : default;
            if (!frame.IsValid)
            {
                _pool.HideFrom(0);
                return;
            }

            Rect source = frame.Sprite.textureRect;
            float border = _borderPx;
            float[] sourceX = { 0f, border, source.width - border, source.width };
            float[] sourceY = { 0f, border, source.height - border, source.height };   // 위에서부터
            float[] targetX = { 0f, border, _sizePx.x - border, _sizePx.x };
            float[] targetY = { 0f, border, _sizePx.y - border, _sizePx.y };
            int used = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    float width = targetX[col + 1] - targetX[col];
                    float height = targetY[row + 1] - targetY[row];
                    if (width <= 0f || height <= 0f)
                    {
                        continue;
                    }

                    Sprite slice = GetSlice(frame.Sprite, row * 3 + col, new Rect(source.x + sourceX[col], source.y + source.height - sourceY[row + 1], sourceX[col + 1] - sourceX[col], sourceY[row + 1] - sourceY[row]));
                    SpriteRenderer renderer = _pool.Get(transform, used++);
                    renderer.sprite = slice;
                    renderer.color = _color;
                    renderer.sortingOrder = _sortingOrder;
                    renderer.transform.localPosition = new Vector3(targetX[col], -targetY[row + 1], 0f) / ViewUtils.PPU;
                    renderer.transform.localScale = new Vector3(width / slice.rect.width, height / slice.rect.height, 1f);
                }
            }

            _pool.HideFrom(used);
        }

        private static Sprite GetSlice(Sprite source, int sliceIndex, Rect rect)
        {
            if (!SliceCache.TryGetValue((source, sliceIndex), out Sprite slice) || slice == null)
            {
                slice = Sprite.Create(source.texture, rect, Vector2.zero, ViewUtils.PPU, 0, SpriteMeshType.FullRect);
                slice.hideFlags = HideFlags.HideAndDontSave;
                SliceCache[(source, sliceIndex)] = slice;
            }

            return slice;
        }
    }
}
