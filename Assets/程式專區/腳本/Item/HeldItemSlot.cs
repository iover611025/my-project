using UnityEngine;
using UnityEngine.EventSystems;

namespace X
{
    // 繼承 Enter 和 Exit 介面來偵測滑鼠懸停
    public class HeldItemSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public InventoryUI inventoryUI;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (inventoryUI != null)
            {
                inventoryUI.OnHeldAreaClick();
            }
        }

        // 當滑鼠移入握持區
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (inventoryUI == null) return;

            // 取得目前握持的資料
            ItemData heldData = inventoryUI.GetHeldItemData();

            // 如果手上真的有東西，且不是在拖拽中，就顯示提示
            if (heldData != null && heldData.id != 0 && !eventData.dragging)
            {
                if (InventoryTooltip.Instance != null)
                {
                    InventoryTooltip.Instance.Show(heldData.itemName, heldData.pickupDescription);
                }
            }
        }

        // 當滑鼠移出握持區
        public void OnPointerExit(PointerEventData eventData)
        {
            if (InventoryTooltip.Instance != null)
            {
                InventoryTooltip.Instance.Hide();
            }
        }
    }
}