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
            }
        }

        public void ClearHeldItem()
        {
            if (heldItemImage != null)
            {
                heldItemImage.sprite = defaultHeldItemSprite;
                heldItemImage.enabled = true;
            }
        }
    }
}