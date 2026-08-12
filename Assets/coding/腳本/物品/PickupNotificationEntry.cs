using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace X
{
    /// <summary>
    /// 控制單一物品拾取提示條的顯示與銷毀
    /// </summary>
    public class PickupNotificationEntry : MonoBehaviour
    {
        [Header("UI 元件")]
        public Image itemIconImage;          // 顯示道具圖示
        public TextMeshProUGUI itemNameText; // 顯示道具名稱
        public TextMeshProUGUI descriptionText; // 顯示自訂描述[cite: 8]
        public Button closeButton;           // 關閉按鈕

        /// <summary>
        /// 初始化提示內容
        /// </summary>
        public void Setup(ItemData data, int qty = 1)
        {
            if (data == null) return;

            // 設定名稱與描述（數量 > 1 時顯示 x N）
            if (itemNameText != null)
            {
                string qtyLabel = qty > 1 ? $" x{qty}" : "";
                itemNameText.text = $"獲得了 {data.itemName}{qtyLabel}";
            }
            if (descriptionText != null)
            {
                descriptionText.text = data.pickupDescription;
                descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(data.pickupDescription));
            }

            // 設定圖示[cite: 8]
            if (itemIconImage != null)
            {
                itemIconImage.sprite = data.icon;
                itemIconImage.enabled = data.icon != null;
            }

            // 綁定關閉按鈕事件：點擊後銷毀自己
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => {
                    Destroy(gameObject);
                });
            }
        }
    }
}