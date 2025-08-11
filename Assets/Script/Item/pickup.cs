using UnityEngine;
using UnityEngine.EventSystems;
using X;

namespace x
{
    public class PickupableItem : MonoBehaviour, IPointerClickHandler
    {
        public int itemIndex;


        public void OnPointerClick(PointerEventData eventData)
        {
            // 取得場景中的 InventorySystemUI 物件
            InventorySystemUI inventory = Object.FindFirstObjectByType<InventorySystemUI>();
            if (inventory != null)
            {
                inventory.PickItem(itemIndex); // 呼叫背包管理腳本的方法
                Destroy(gameObject);           // 撿起後讓物品消失
            }
        }
    }
}