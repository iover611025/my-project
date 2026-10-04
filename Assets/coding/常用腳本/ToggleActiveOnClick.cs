using UnityEngine;
using UnityEngine.UI;

namespace X
{
    /// <summary>
    /// 通用開關（Toggle）腳本：
    /// 可用來控制目標 GameObject（如線索本面板）的顯示與隱藏。
    /// 
    /// 使用方式：
    /// 1. 將此腳本掛在「線索本按鈕」上。
    /// 2. 在 Target Object 欄位拖入「線索本 Panel」物件。
    /// 3. 在 Button 的 OnClick() 事件中加入此腳本的 Toggle() 方法
    ///    （若直接掛在 Button 上，Awake 也會自動註冊監聽）。
    /// </summary>
    public class ToggleActiveOnClick : MonoBehaviour
    {
        [Header("目標物件")]
        [Tooltip("要開關的目標面板（例如線索本 GameObject）。若留空，預設控制自己。")]
        [SerializeField] private GameObject targetObject;

        [Header("可選：按鈕圖示切換")]
        [Tooltip("點擊後是否連同按鈕自身的圖片一起切換？")]
        [SerializeField] private bool changeButtonSprite = false;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Sprite openedSprite;
        [SerializeField] private Sprite closedSprite;

        private void Awake()
        {
            // 如果沒指定目標，預設為自己
            if (targetObject == null)
            {
                targetObject = gameObject;
            }

            // 自動抓取按鈕 Image 元件
            if (changeButtonSprite && buttonImage == null)
            {
                buttonImage = GetComponent<Image>();
                if (buttonImage != null && closedSprite == null)
                {
                    closedSprite = buttonImage.sprite;
                }
            }

            UpdateVisual();
        }

        /// <summary>
        /// 切換目標的開關狀態（若開則關，若關則開）。
        /// 供 Button 的 OnClick() 調用。
        /// </summary>
        public void Toggle()
        {
            if (targetObject == null) return;

            bool nextState = !targetObject.activeSelf;
            targetObject.SetActive(nextState);

            UpdateVisual();
        }

        /// <summary>
        /// 強制開啟目標物件。
        /// </summary>
        public void Open()
        {
            if (targetObject == null) return;
            targetObject.SetActive(true);
            UpdateVisual();
        }

        /// <summary>
        /// 強制關閉目標物件。
        /// </summary>
        public void Close()
        {
            if (targetObject == null) return;
            targetObject.SetActive(false);
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (!changeButtonSprite || buttonImage == null || targetObject == null) return;

            bool isCurrentlyOpen = targetObject.activeSelf;
            if (isCurrentlyOpen && openedSprite != null)
            {
                buttonImage.sprite = openedSprite;
            }
            else if (!isCurrentlyOpen && closedSprite != null)
            {
                buttonImage.sprite = closedSprite;
            }
        }
    }
}
