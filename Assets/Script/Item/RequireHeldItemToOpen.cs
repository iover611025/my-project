using UnityEngine;
using UnityEngine.UI;

namespace X
{
    public class RequireHeldItemToOpen : MonoBehaviour
    {
        public int requiredItemId; // 需要握持的道具ID
        public InventoryUI inventoryUI; // 拖入InventoryUI
        public ToggleUIObject doorToggle; // 門的開關腳本
        public ItemDatabase itemDatabase;

        public void TryOpenDoor()
        {
            if (inventoryUI == null || doorToggle == null || itemDatabase == null)
                return;

            // 取得目前握持的icon
            Sprite heldIcon = inventoryUI.heldItemImage != null ? inventoryUI.heldItemImage.sprite : null;
            if (heldIcon == null)
            {
                Debug.Log("請先握持正確的道具！");
                return;
            }

            var itemData = itemDatabase.items.Find(x => x.icon == heldIcon);
            if (itemData != null && itemData.id == requiredItemId)
            {
                doorToggle.OnClick(); // 開門
                Debug.Log("門已開啟！");
            }
            else
            {
                Debug.Log("握持的不是正確的道具，無法開門！");
            }
        }
    }
}