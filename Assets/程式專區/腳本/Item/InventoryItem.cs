using UnityEngine;
using System.Collections.Generic;

namespace X
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemData> items = new List<ItemData>();
    }

    [System.Serializable]
    public class ItemData
    {
        public int id;
        public string itemName;
        public Sprite icon;

        [TextArea(3, 10)] // 讓 Inspector 顯示較大的文字輸入框
        public string pickupDescription; // 新增：自訂的拾取提示文本
    }
}