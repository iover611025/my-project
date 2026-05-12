using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace X
{
    public class InventoryTooltip : MonoBehaviour
    {
        public static InventoryTooltip Instance;

        [Header("UI 元件")]
        public GameObject tooltipPanel;
        public TextMeshProUGUI contentText;

        private void Awake()
        {
            Instance = this;
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
        }

        /// <summary>
        /// 顯示物品提示
        /// </summary>
        /// <param name="itemName">物品名稱</param>
        /// <param name="description">物品描述</param>
        public void Show(string itemName, string description)
        {
            if (tooltipPanel == null || contentText == null) return;

            contentText.text = $"<b>{itemName}</b>\n{description}";
            tooltipPanel.SetActive(true);

            // 確保提示框永遠顯示在最上層
            tooltipPanel.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
        }

        // 進階：讓提示框跟隨滑鼠位置（可選）
        private void Update()
        {
            if (tooltipPanel != null && tooltipPanel.activeSelf)
            {
                // 這裡可以加入跟隨滑鼠的邏輯，或者固定在特定位置
            }
        }
    }
}