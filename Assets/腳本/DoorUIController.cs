using UnityEngine;
using UnityEngine.EventSystems;

namespace X
{
    public class DoorUIController : MonoBehaviour, IPointerClickHandler
    {
        public bool isOpen = false; // 門是否已開啟
        public GameObject currentPanel; // 目前顯示的UI Panel
        public GameObject nextPanel;    // 要顯示的UI Panel
        public InventoryUI inventoryUI; // 取得是否空手

        // 你可以用其他方式開門，這裡只示範點擊切換
        public void OpenDoor()
        {
            isOpen = true;
            // 其他開門動畫或邏輯
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isOpen) return;

            // 判斷是否空手
            bool isEmptyHand = inventoryUI == null || inventoryUI.heldItemImage == null || inventoryUI.heldItemImage.sprite == null || !inventoryUI.heldItemImage.enabled;

            if (isEmptyHand)
            {
                if (currentPanel != null)
                    currentPanel.SetActive(false);
                if (nextPanel != null)
                    nextPanel.SetActive(true);
            }
        }
    }
}