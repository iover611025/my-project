using UnityEngine;

namespace X
{
    public class PickupNotificationUI : MonoBehaviour
    {
        public static PickupNotificationUI Instance;

        [Header("設定")]
        public GameObject entryPrefab; // 提示條的預製物件 (Prefab)
        public Transform container;   // 放置提示條的父物件 (通常掛載 Vertical Layout Group)

        void Awake()
        {
            Instance = this;
            // 由於改為動態生成，原本通知面板初始隱藏的邏輯可移除[cite: 8]
        }

        /// <summary>
        /// 顯示物品提示（支援複數生成疊加）
        /// </summary>
        public void ShowNotification(ItemData data, int qty = 1)
        {
            if (data == null || entryPrefab == null || container == null)
            {
                Debug.LogWarning("[PickupNotificationUI] 遺失設定，無法生成提示。");
                return;
            }

            // 1. 在容器中生成新的提示條
            GameObject newEntryGO = Instantiate(entryPrefab, container);

            // 2. 取得提示條腳本並初始化（傳入數量）
            PickupNotificationEntry entry = newEntryGO.GetComponent<PickupNotificationEntry>();
            if (entry != null)
            {
                entry.Setup(data, qty);
            }

            // 3. 確保容器始終顯示在 UI 最前方[cite: 8]
            container.SetAsLastSibling();
        }

        // 原本的 CloseNotification() 可刪除，因為現在由各別 Entry 自行 Destroy[cite: 8]
    }
}