using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 같은 칸을 늘어놓은 줄(플레이어 HP 칸, 손 HP 막대). 칸 수는 데이터(최대 HP)에 따라 플레이 중에 바뀐다.
    /// 오브젝트 위치 = 왼쪽 정렬이면 첫 칸 왼쪽 위, 오른쪽 정렬이면 마지막 칸 오른쪽 위.
    /// </summary>
    [ExecuteAlways]
    public class CountRowView : MonoBehaviour
    {
        [SerializeField] private string _sheet = "HpCell";
        [SerializeField] private string _fullTag = "Full";
        [SerializeField] private string _emptyTag = "Empty";
        [Tooltip("칸 사이 간격(px, 칸 왼쪽 끝 사이)")]
        [SerializeField] private float _spacingPx = 9f;
        [SerializeField] private bool _isRightAligned;
        [SerializeField] private int _sortingOrder = RenderOrderUtils.HUD;
        [Tooltip("에디터 미리보기용 칸 수")]
        [SerializeField] private int _previewCount = 5;

        private readonly HiddenRendererPool _pool = new HiddenRendererPool();
        private int _count = -1;
        private int _value = -1;

        public void Render(int value, int count)
        {
            if (value == _value && count == _count)
            {
                return;
            }

            _value = value;
            _count = count;
            Rebuild();
        }

        private void OnEnable()
        {
            _count = -1;
        }

        private void OnDisable()
        {
            _pool.Clear();
        }

        private void OnValidate()
        {
            _count = -1;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying && _count != _previewCount)
            {
                _count = _previewCount;
                _value = _previewCount;
                Rebuild();
            }
        }

        private void Rebuild()
        {
            ArtCatalog catalog = ViewAssets.Instance != null ? ViewAssets.Instance.Catalog : null;
            if (catalog == null)
            {
                return;
            }

            ArtFrame full = catalog.GetFrame(_sheet, _fullTag, 0);
            ArtFrame empty = catalog.GetFrame(_sheet, _emptyTag, 0);
            int width = full.CanvasSize.x;
            for (int i = 0; i < _count; i++)
            {
                ArtFrame frame = i < _value ? full : empty;
                SpriteRenderer renderer = _pool.Get(transform, i);
                renderer.sprite = frame.Sprite;
                renderer.sortingOrder = _sortingOrder;
                float x = _isRightAligned ? -((_count - 1 - i) * _spacingPx + width) : i * _spacingPx;
                renderer.transform.localPosition = new Vector3(x, -frame.CanvasSize.y, 0f) / ViewUtils.PPU;
            }

            _pool.HideFrom(Mathf.Max(0, _count));
        }
    }
}
