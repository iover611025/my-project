using UnityEngine;
using UnityEngine.EventSystems;

namespace X
{
    public class PickupableItem : MonoBehaviour, IPointerClickHandler
    {
        public int itemID;               // 只需在Inspector填id
        public ItemDatabase itemDatabase; // 拖進資料表ScriptableObject

        // 新增：可選需求 — 玩家必須握持指定道具才能觸發撿取
        [Header("可選：需握持指定道具才能撿取")]
        [Tooltip("啟用後，玩家必須握持 requiredHeldItemId 指定的道具才能撿取此場景物件")]
        public bool requireHeldItem = false;
        [Tooltip("若 requireHeldItem=true，填入需要握持的道具 id")]
        public int requiredHeldItemId = 0;
        [Tooltip("當撿取成功時是否消耗玩家當前握持的道具（會呼叫 InventoryUI.ClearHeldItem）")]
        public bool consumeHeldItemOnPickup = false;

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
                if (inventoryUI == null)
                {
                    Debug.LogWarning("[Pickup] 沒有找到 InventoryUI，無法加入物品欄。");
                    return;
                }

                // 若設定需要握持指定道具，檢查當前握持是否符合
                if (requireHeldItem)
                {
                    var held = inventoryUI.GetHeldItemData();
                    if (held == null || held.id != requiredHeldItemId)
                    {
                        if (inventoryUI.IsHeldEmpty())
                            Debug.Log("[Pickup] 需要握持正確的道具才能撿取此物件！");
                        else
                            Debug.Log("[Pickup] 握持的不是正確的道具，無法撿取此物件！");
                        return;
                    }
                }
                else
                {
                    // 原行為：若玩家目前握持著物品，禁止再次撿取
                    if (!inventoryUI.IsHeldEmpty())
                    {
                        Debug.Log("[Pickup] 目前握持著物品，無法撿取新的物品，請先放置手上物品到物品欄。");
                        return;
                    }
                }

                bool accepted = inventoryUI.AddItemToSlot(data);
                if (accepted)
                {
                    // --- 新增：顯示拾取通知 ---
                    if (PickupNotificationUI.Instance != null)
                    {
                        PickupNotificationUI.Instance.ShowNotification(data);
                    }

                    // 如果設定要消耗握持道具，且玩家確實握著（requireHeldItem 模式下），則清除握持
                    if (consumeHeldItemOnPickup && inventoryUI.GetHeldItemData() != null && inventoryUI.GetHeldItemData().id != 0)
                    {
                        inventoryUI.ClearHeldItem();
                    }

                    // 只有在 Inventory 接受（或排程）時才移除場上物件
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogWarning("[Pickup] Inventory 未接受此道具，保留場上物件。");
                }
            }
        }

    }
}