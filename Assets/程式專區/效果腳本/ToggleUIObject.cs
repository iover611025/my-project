using UnityEngine;
using UnityEngine.UI;

namespace X
{
    public class ToggleUIObject : MonoBehaviour
    {
        public Sprite closedSprite;
        public Sprite openSprite;
        public bool isOpen = false;

        // 若為 true，門開啟後會禁用後續點擊以避免圖片被切回
        public bool disableClickWhenOpen = true;

        private Image img;

        void Awake()
        {
            img = GetComponent<Image>();
            if (img == null)
                img = GetComponentInChildren<Image>();

            UpdateVisual();
        }
                
        public void OnClick()
        {
            // 若已開啟且設定禁用，再次點擊不做任何事（避免切回圖片）
            if (disableClickWhenOpen && isOpen)
                return;

            isOpen = !isOpen;
            UpdateVisual();
        }

        // 外部可透過程式設定狀態（避免直接改 isOpen）
        public void SetOpen(bool open)
        {
            isOpen = open;
            UpdateVisual();
        }

        void UpdateVisual()
        {
            if (img != null)
                img.sprite = isOpen ? openSprite : closedSprite;
        }
    }
}