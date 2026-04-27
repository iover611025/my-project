using UnityEngine;
using UnityEngine.EventSystems;

namespace X
{
    public class PickupableItem : MonoBehaviour, IPointerClickHandler
    {
        // 定義拾取模式
        public enum PickupRequirement
        {
            Default,    // 預設：若有空位就進背包，沒空位就拿在手上
            MustBeEmpty // 必須空手：玩家手上不能握有任何東西才能拾取
        }

        [Header("基礎設定")]
        public int itemID;
        public ItemDatabase itemDatabase;
        public PickupRequirement pickupRequirement = PickupRequirement.Default;

        [Header("可選：需握持指定道具才能撿取")]
        public bool requireHeldItem = false;
        public int requiredHeldItemId = 0;
        public bool consumeHeldItemOnPickup = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (itemDatabase == null)
            {
                Debug.LogWarning("請先將ItemDatabase拖進PickupableItem腳本的itemDatabase欄位！");
                return;
            }

            var data = itemDatabase.items.Find(x => x.id == itemID);
            if (data == null) return;

            var inventoryUI = Object.FindFirstObjectByType<InventoryUI>();
            if (inventoryUI == null) return;

            // --- 新增：檢查「必須空手」的邏輯 ---
            if (pickupRequirement == PickupRequirement.MustBeEmpty)
            {
                if (!inventoryUI.IsHeldEmpty())
                {
                    Debug.Log($"[Pickup] {gameObject.name} 太重或太特殊，你必須先放下手上的東西才能撿起它。");
                    return;
                }
            }

            // 原有的「需握持特定道具」邏輯
            if (requireHeldItem)
            {
                var held = inventoryUI.GetHeldItemData();
                if (held == null || held.id != requiredHeldItemId)
                {
                    Debug.Log("[Pickup] 握持的不是正確的道具，無法撿取！");
                    return;
                }
            }
            // 原有的「預設限制」：如果不是要求特定道具，且目前手上拿著東西，預設不給撿新的
            // (這部分保留了你原有的邏輯，但與 MustBeEmpty 有所區隔)
            else if (pickupRequirement == PickupRequirement.Default)
            {
                if (!inventoryUI.IsHeldEmpty())
                {
                    Debug.Log("[Pickup] 目前握持著物品，請先將手上物品放回物品欄。");
                    return;
                }
            }

            // 執行拾取動作
            ExecutePickup(inventoryUI, data);
        }

        private void ExecutePickup(InventoryUI inventoryUI, ItemData data)
        {
            bool accepted = inventoryUI.AddItemToSlot(data);
            if (accepted)
            {
                if (PickupNotificationUI.Instance != null)
                    PickupNotificationUI.Instance.ShowNotification(data);

                if (consumeHeldItemOnPickup && !inventoryUI.IsHeldEmpty())
                    inventoryUI.ClearHeldItem();

                Destroy(gameObject);
            }
        }
    }
}