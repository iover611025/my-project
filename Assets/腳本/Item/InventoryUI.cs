using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace X
{
    public class InventoryUI : MonoBehaviour
    {
        public List<InventorySlot> slots;
        public Image heldItemImage; // 握持區
        private Sprite defaultHeldItemSprite;

        void Awake()
        {
            if (heldItemImage != null)
                defaultHeldItemSprite = heldItemImage.sprite;
        }

        public void AddItemToSlot(ItemData item)
        {
            foreach (var slot in slots)
            {
                if (slot.iconImage.sprite == null)
                {
                    slot.SetIcon(item.icon);
                    break;
                }
            }
        }

        public void SetHeldItem(Sprite icon)
        {
            if (heldItemImage != null)
            {
                heldItemImage.sprite = icon;
                heldItemImage.enabled = true;
                // 若使用透明或特殊 placeholder，需要確保 alpha 或 enabled 表示「有物品」
            }
        }

        public void ClearHeldItem()
        {
            if (heldItemImage != null)
            {
                // 把握持區視為「空手」：移除 sprite 並關閉 Image，讓 IsEmptyHand 能正確判斷
                heldItemImage.sprite = null;
                heldItemImage.enabled = false;
            }
        }
    }
}