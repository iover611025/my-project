using UnityEngine;
using UnityEngine.UI;

namespace X
{
    public class RequireHeldItemToOpen : MonoBehaviour
    {
        public int requiredItemId; // 需要握持的道具ID
        public InventoryUI inventoryUI; // 拖入InventoryUI
        public ToggleUIObject doorToggle; // 門的開關腳本

        public void TryOpenDoor()
        {
            if (inventoryUI == null || doorToggle == null)
                return;

            // 取得目前握持的icon
            Sprite heldIcon = inventoryUI.heldItemImage != null ? inventoryUI.heldItemImage.sprite : null;
            if (heldIcon == null)
            {
                Debug.Log("請先握持正確的道具！");
                return;
            }

            // 檢查握持的icon是否對應到正確的道具
            foreach (var slot in inventoryUI.slots)
            {
                if (slot.iconImage.sprite == heldIcon)
                {
                    // 取得該slot的道具ID
                    int slotIndex = inventoryUI.slots.IndexOf(slot);
                    // 你可以根據你的設計，將ItemData存在slot裡，這裡假設icon唯一對應
                    var itemData = FindItemDataByIcon(heldIcon);
                    if (itemData != null && itemData.id == requiredItemId)
                    {
                        doorToggle.OnClick(); // 開門
                        Debug.Log("門已開啟！");
                        return;
                    }
                }
            }

            Debug.Log("握持的不是正確的道具，無法開門！");
        }

        // 根據icon尋找ItemData（需有ItemDatabase參考）
        public ItemDatabase itemDatabase;
        private ItemData FindItemDataByIcon(Sprite icon)
        {
            if (itemDatabase == null) return null;
            return itemDatabase.items.Find(x => x.icon == icon);
        }
    }
}