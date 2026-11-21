using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 슬롯 위에 서 있으면 화면 아래에 뜨는 발동 안내: Space 키 그림 + "CAST FIRE"(콤보가 가능한 시간이면 "COMBO!" 붙음).
    /// 오브젝트 위치 = 안내 줄의 위쪽 가운데.
    /// </summary>
    public class SlotPromptView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spaceKey;
        [Tooltip("{0} = 단어")]
        [SerializeField] private PixelText _label;
        [SerializeField] private string _comboSuffix = "  COMBO!";
        [Tooltip("Space 키 그림과 글자 사이(px)")]
        [SerializeField] private float _gap = 4f;
        [SerializeField] private float _labelTop = 4f;
        [Tooltip("Space 키 그림이 눌렸다 떼어지는 빠르기(초당)")]
        [SerializeField] private float _pressRate = 2.5f;

        private string _format;

        public void Render(GameSession game)
        {
            BoardCell here = game.Player.Here;
            bool isVisible = game.State == GameState.Play && here != null && here.IsSlot && here.Block != null;
            gameObject.SetActive(isVisible);
            if (!isVisible)
            {
                return;
            }

            _format ??= _label.Text;
            string text = string.Format(_format, here.Block.Word) + (game.IsComboOpen(game.Player.Board) ? _comboSuffix : string.Empty);
            _label.SetDisplay(text);
            ArtCatalog catalog = ViewAssets.Instance.Catalog;
            bool isPressedFrame = Mathf.FloorToInt(game.Time * _pressRate) % 2 == 0;
            ArtFrame key = catalog.GetFrame("SpaceKey", isPressedFrame ? "Pressed" : "Base", 0);
            _spaceKey.sprite = key.Sprite;
            float width = key.CanvasSize.x + _gap + PixelFontUtils.Measure(text);
            float left = -Mathf.Round(width * 0.5f);
            _spaceKey.transform.localPosition = new Vector3(left, -key.CanvasSize.y, 0f) / ViewUtils.PPU;
            _label.transform.localPosition = new Vector3(left + key.CanvasSize.x + _gap, -_labelTop, 0f) / ViewUtils.PPU;
        }
    }
}
