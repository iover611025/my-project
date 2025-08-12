using UnityEngine;
using UnityEngine.UI;

namespace X
{
    public class ToggleUIObject : MonoBehaviour
    {
        public Sprite closedSprite;
        public Sprite openSprite;
        public bool isOpen = false;

        private Image img;

        void Awake()
        {
            img = GetComponent<Image>();
            UpdateVisual();
        }

        public void OnClick()
        {
            isOpen = !isOpen;
            UpdateVisual();
            // 這裡可加音效、動畫、事件等
        }

        void UpdateVisual()
        {
            if (img != null)
                img.sprite = isOpen ? openSprite : closedSprite;
        }
    }
}