using UnityEngine;
using UnityEngine.EventSystems;

namespace X
{
    public class PickupableItem : MonoBehaviour, IPointerClickHandler
    {
        public int itemID;               // 只需在Inspector填id
        public ItemDatabase itemDatabase; // 拖進資料表ScriptableObject

        public void OnPointerClick(PointerEventData eventData)
        {
            if (itemDatabase == null)
            {
                Debug.LogWarning("請先將ItemDatabase拖進PickupableItem腳本的itemDatabase欄位！");
                return;
            }
            var data = itemDatabase.items.Find(x => x.id == itemID);
            if (data != null)
            {
                var inventoryUI = Object.FindFirstObjectByType<InventoryUI>();
                if (inventoryUI != null)
                {
                    inventoryUI.AddItemToSlot(data);
                }
                Destroy(gameObject);
            }
        }
    }
}