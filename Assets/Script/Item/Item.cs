

namespace abc
{
    using UnityEngine;

    [System.Serializable]
    public class Item
    {
        public string id;            // 物品唯一識別碼
        public string itemName;      // 物品名稱
        public Sprite icon;          // 物品圖片
        public int quantity = 1;     // 數量
        public bool canStack = false;// 是否可堆疊
        public string description;   // 物品描述
    }
}