using ShiftMagicSpell.Core;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>SHIFT 칸(ShiftKey 프리팹): 파란 키 + 순간이동 쿨다운 동안 위에서부터 덮이는 어두운 덮개.</summary>
    public class ShiftKeyView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("쿨다운 덮개(흰 점을 늘려서 씀). 위쪽 기준으로 높이가 줄어든다")]
        [SerializeField] private SpriteRenderer _cooldown;
        [Tooltip("덮개 최대 높이(px) = 키 윗면")]
        [SerializeField] private float _cooldownHeightPx = 16f;
        [Tooltip("키 그림 높이(px). 덮개는 이 높이의 위쪽부터 덮는다")]
        [SerializeField] private float _keyHeightPx = 20f;
        [SerializeField] private int _sortingOrder = RenderOrderUtils.BOARD_BASE;

        /// <param name="cooldownRatio">0이면 덮개 없음, 1이면 가득</param>
        public void Render(float cooldownRatio)
        {
            _body.sortingOrder = _sortingOrder;
            _cooldown.sortingOrder = _sortingOrder + 1;
            _cooldown.enabled = cooldownRatio > 0f;
            _cooldown.color = ViewAssets.Instance.Colors.CooldownShade;
            float height = Mathf.Round(_cooldownHeightPx * cooldownRatio);
            Vector3 scale = _cooldown.transform.localScale;
            _cooldown.transform.localScale = new Vector3(scale.x, height, 1f);

            // 덮개는 피벗이 왼쪽 아래라서, 키 위쪽 끝에서 아래로 덮이게 높이만큼 내린다
            Vector3 position = _cooldown.transform.localPosition;
            _cooldown.transform.localPosition = new Vector3(position.x, (_keyHeightPx - height) / ViewUtils.PPU, position.z);
        }
    }
}
