using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace X
{
    public class PickupNotificationUI : MonoBehaviour
    {
        public static PickupNotificationUI Instance;

        [Header("UI 元件")]
        public GameObject notificationPanel; // 通知面板本體
        public Image itemIconImage;          // 顯示道具圖示的 Image
        public TextMeshProUGUI itemNameText; // 顯示道具名稱的 TextMeshPro
        public TextMeshProUGUI descriptionText; // 新增：用於顯示自訂描述的元件

        void Awake()
        {
            Instance = this;
            if (notificationPanel != null)
                notificationPanel.SetActive(false); // 初始隱藏
        }

        // 供外部呼叫的顯示函式
        public void ShowNotification(ItemData data)
        {
            if (data == null || notificationPanel == null) return;

            // 更新內容
            if (itemNameText != null) itemNameText.text = "獲得了 " + data.itemName;
            if (descriptionText != null)
            {
                if (!string.IsNullOrEmpty(data.pickupDescription))
                {
                    descriptionText.text = data.pickupDescription;
                    descriptionText.gameObject.SetActive(true);
                }
                else
                {
                    descriptionText.gameObject.SetActive(false); // 若沒填則隱藏描述框
                }
            }
            if (itemIconImage != null)
            {
                itemIconImage.sprite = data.icon;
                itemIconImage.enabled = data.icon != null;
            }
            // 顯示面板
            notificationPanel.SetActive(true);

            // 確保通知面板在最上層
            notificationPanel.transform.SetAsLastSibling();
        }

        // 供按鈕（例如面板上的「確定」按鈕）呼叫的關閉函式
        public void CloseNotification()
        {
            if (notificationPanel != null)
                notificationPanel.SetActive(false);
        }
    }
}