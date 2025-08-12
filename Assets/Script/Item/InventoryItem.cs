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
        public int id;           // 道具ID
        public string itemName;  // 道具名稱
        public Sprite icon;      // 道具圖示
        // 你可以擴充其他欄位
    }
}