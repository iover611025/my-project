using UnityEngine;
using UnityEngine.UI;

namespace X
{
    public class InventorySlot : MonoBehaviour
    {
        public Image iconImage;

        public void SetIcon(Sprite icon)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
    }
}