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
            if (data != null )
            {
                var inventoryUI = Object.FindFirstObjectByType<InventoryUI>();
                if (inventoryUI != null && inventoryUI.IsHeldEmpty())
                {
                    Debug.Log($"[Pickup] Found InventoryUI '{inventoryUI.gameObject.name}'. heldEmpty={inventoryUI.IsHeldEmpty()} slotsCount={inventoryUI.slots?.Count ?? 0}");
                    // 如果玩家目前握持著物品，禁止再次撿取
                    if (!inventoryUI.IsHeldEmpty())
                    {
                        Debug.Log("[Pickup] 目前握持著物品，無法撿取新的物品，請先放置手上物品到物品欄。");
                        return;
                    }

                    bool accepted = inventoryUI.AddItemToSlot(data);
                    if (accepted)
                    {
                        // --- 新增：顯示拾取通知 ---
                        if (PickupNotificationUI.Instance != null)
                        {
                            PickupNotificationUI.Instance.ShowNotification(data);
                        }

                        // 只有在 Inventory 接受（或排程）時才移除場上物件
                        Destroy(gameObject);
                    }
                    else
                    {
                        Debug.LogWarning("[Pickup] Inventory 未接受此道具，保留場上物件。");
                    }
                }
                else
                {
                    Debug.LogWarning("[Pickup] 沒有找到 InventoryUI，無法加入物品欄。");
                }
            }
        }

    }
}