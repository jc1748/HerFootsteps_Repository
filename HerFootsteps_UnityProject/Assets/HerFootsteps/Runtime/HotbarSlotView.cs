using UnityEngine;
using UnityEngine.UI;

namespace HerFootsteps
{
    public sealed class HotbarSlotView : MonoBehaviour
    {
        [SerializeField] private Image background, border, icon;
        [SerializeField] private Text number, quantity, fallback;
        public void Bind(Image bg, Image edge, Image picture, Text key, Text count, Text placeholder)
        { background = bg; border = edge; icon = picture; number = key; quantity = count; fallback = placeholder; }
        public void Refresh(InventorySlot slot, int index, bool selected)
        {
            number.text = (index + 1).ToString();
            var item = slot.Item;
            icon.sprite = item != null ? item.Icon : null; icon.enabled = icon.sprite != null;
            fallback.text = item != null && item.Icon == null ? item.DisplayName.Substring(0, Mathf.Min(3, item.DisplayName.Length)).ToUpperInvariant() : "";
            quantity.text = item != null && item.MaximumStack > 1 ? slot.Quantity.ToString() : "";
            border.color = Color.Lerp(border.color, selected ? new Color(0.66f, 0.74f, 0.69f, 0.95f) : new Color(0.27f, 0.34f, 0.32f, 0.6f), Time.unscaledDeltaTime * 14);
            background.color = selected ? new Color(0.075f, 0.105f, 0.1f, 0.93f) : new Color(0.025f, 0.035f, 0.033f, 0.78f);
        }
    }
}
