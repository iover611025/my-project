using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace X
{
    public class InventorySlot : MonoBehaviour, IPointerClickHandler
    {
        public Image iconImage;

        public void SetIcon(Sprite icon)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var inventoryUI = GetComponentInParent<InventoryUI>();
            if (inventoryUI != null)
            {
                if (inventoryUI.heldItemImage != null && inventoryUI.heldItemImage.sprite == iconImage.sprite && iconImage.sprite != null)
                {
                    inventoryUI.ClearHeldItem();
                }
                else
                {
                    inventoryUI.SetHeldItem(iconImage.sprite);
                }
            }
        }
    }
}