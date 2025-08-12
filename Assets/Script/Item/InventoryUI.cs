using UnityEngine;
using System.Collections.Generic;

namespace X
{
    public class InventoryUI : MonoBehaviour
    {
        public List<InventorySlot> slots;

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
    }
}