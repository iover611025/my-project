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
            if (data != null)
            {
                Debug.Log($"你撿到：【{data.itemName}】");
                // 這裡可以呼叫你的背包系統，把 data 加入背包
                Destroy(gameObject); // 撿起後物品消失
            }
            else
            {
                Debug.LogWarning($"找不到ID為{itemID}的道具！");
            }
        }
    }
}