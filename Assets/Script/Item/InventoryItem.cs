using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace X
{

    // 單個物品資料結構
    [System.Serializable]
    public class InventoryItem : MonoBehaviour
    {
        public string itemName;
        public Sprite icon;
        public bool isPicked; // 狀態
                              // 可擴充：數量、描述、ID等
    }

    public class InventorySystemUI : MonoBehaviour
    {
        [Header("物品欄設定")]
        public List<InventoryItem> allItems;         // 全部可撿物品資料（可在 Inspector 編輯）
        public Transform inventoryGridParent;        // 物品欄格子父物件（Grid Layout Group）
        public GameObject slotPrefab;                // 空格 Prefab（內有Image）

        [Header("測試用撿物品按鈕")]
        public Button pickItemButton;
        public int pickItemIndex = 0;                // 測試要撿第幾個物品

        private List<InventoryItem> ownedItems = new List<InventoryItem>();
        private List<Image> slots = new List<Image>();

        void Start()
        {
            // 初始化格子
            for (int i = 0; i < inventoryGridParent.childCount; i++)
            {
                slots.Add(inventoryGridParent.GetChild(i).GetComponent<Image>());
                slots[i].sprite = null;
                slots[i].color = new Color(1, 1, 1, 0); // 隱藏
            }
            // 可用 Instantiate(slotPrefab) 動態生成

            // 綁定測試撿物品按鈕
            if (pickItemButton != null)
                pickItemButton.onClick.AddListener(() => PickItem(pickItemIndex));
        }

        // 撿到物品
        public void PickItem(int index)
        {
            if (index < 0 || index >= allItems.Count)
                return;

            InventoryItem item = allItems[index];
            if (item.isPicked) return; // 已撿過不重複撿

            item.isPicked = true;      // 狀態改變
            ownedItems.Add(item);      // 存進背包
            UpdateInventoryUI();       // 更新顯示
        }

        // 更新物品欄圖示
        void UpdateInventoryUI()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (i < ownedItems.Count)
                {
                    slots[i].sprite = ownedItems[i].icon;
                    slots[i].color = Color.white;
                }
                else
                {
                    slots[i].sprite = null;
                    slots[i].color = new Color(1, 1, 1, 0);
                }
            }
        }
    }
}