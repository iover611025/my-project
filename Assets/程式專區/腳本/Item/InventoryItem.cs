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
    }
}