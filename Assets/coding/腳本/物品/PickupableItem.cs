using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

        /// <summary>
        /// 單筆拾取項目：指定物品 ID 與數量。
        /// 同一個 PickupableItem 可設定多筆不同種類的物品。
        /// </summary>
        [System.Serializable]
        public class PickupEntry
        {
            public int itemID;
            [Min(1), Tooltip("拾取此種物品的數量，最少為 1")]
            public int amount = 1;
        }

        [Header("基礎設定")]
        public ItemDatabase itemDatabase;
        public PickupRequirement pickupRequirement = PickupRequirement.Default;

        [Header("拾取物品清單（可新增多種不同物品）")]
        public List<PickupEntry> pickupItems = new List<PickupEntry>();

        [Header("可選：需握持指定道具才能撿取")]
        public bool requireHeldItem = false;
        public int requiredHeldItemId = 0;
        public bool consumeHeldItemOnPickup = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (itemDatabase == null)
            {
                Debug.LogWarning("[Pickup] 請先將 ItemDatabase 拖進 PickupableItem 腳本的 itemDatabase 欄位！");
                return;
            }

            if (pickupItems == null || pickupItems.Count == 0)
            {
                Debug.LogWarning("[Pickup] pickupItems 清單是空的，請至少新增一筆物品。");
                return;
            }

            var inventoryUI = Object.FindFirstObjectByType<InventoryUI>();
            if (inventoryUI == null) return;

            // --- 檢查「必須空手」的邏輯 ---
            if (pickupRequirement == PickupRequirement.MustBeEmpty)
            {
                if (!inventoryUI.IsHeldEmpty())
                {
                    Debug.Log($"[Pickup] {gameObject.name} 太重或太特殊，你必須先放下手上的東西才能撿起它。");
                    return;
                }
            }

            // --- 需握持特定道具 ---
            if (requireHeldItem)
            {
                var held = inventoryUI.GetHeldItemData();
                if (held == null || held.id != requiredHeldItemId)
                {
                    Debug.Log("[Pickup] 握持的不是正確的道具，無法撿取！");
                    return;
                }
            }
            // --- 預設限制：手上有物品時不給撿 ---
            else if (pickupRequirement == PickupRequirement.Default)
            {
                if (!inventoryUI.IsHeldEmpty())
                {
                    Debug.Log("[Pickup] 目前握持著物品，請先將手上物品放回物品欄。");
                    return;
                }
            }

            ExecutePickup(inventoryUI);
        }

        private void ExecutePickup(InventoryUI inventoryUI)
        {
            // 1. 收集所有有效的拾取資料
            var validEntries = new List<(ItemData data, int qty)>();
            foreach (var entry in pickupItems)
            {
                if (entry == null) continue;
                var data = itemDatabase.items.Find(x => x.id == entry.itemID);
                if (data == null)
                {
                    Debug.LogWarning($"[Pickup] 找不到 itemID={entry.itemID} 的資料，跳過此筆。");
                    continue;
                }
                validEntries.Add((data, Mathf.Max(1, entry.amount)));
            }

            if (validEntries.Count == 0) return;

            // 2. 預先確認背包有足夠空格（全部能放才執行，避免只撿部分）
            if (!inventoryUI.HasEnoughSpace(validEntries.Count))
            {
                Debug.Log("[Pickup] 背包空間不足，無法拾取所有物品。");
                return;
            }

            // 3. 逐一放入背包並顯示通知
            foreach (var (data, qty) in validEntries)
            {
                inventoryUI.AddItemToSlot(data, qty);

                if (PickupNotificationUI.Instance != null)
                    PickupNotificationUI.Instance.ShowNotification(data, qty);
            }

            // 4. 消耗握持道具（若有設定）
            if (consumeHeldItemOnPickup && !inventoryUI.IsHeldEmpty())
                inventoryUI.ClearHeldItem();

            Destroy(gameObject);
        }
    }
}