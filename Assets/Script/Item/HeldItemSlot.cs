using UnityEngine;
using UnityEngine.EventSystems;

namespace X
{
    public class HeldItemSlot : MonoBehaviour, IPointerClickHandler
    {
        public InventoryUI inventoryUI;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (inventoryUI != null)
            {
                inventoryUI.ClearHeldItem();
            }
        }
    }
}