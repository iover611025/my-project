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

            var clueBook = Object.FindFirstObjectByType<ClueBookManager>();
            if (clueBook != null)
                // 將 RegisterClue 改為 UnlockSection，因為 ClueBookManager 只有 UnlockSection 方法
                clueBook.UnlockSection("家", "發現了神秘鑰匙");
        }
    }
}