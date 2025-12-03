using UnityEngine;
using UnityEngine.UI;

namespace X
{
    public class CluePopupUI : MonoBehaviour
    {
        public Text popupText;
        public Image popupImage;
        public Button unlockButton;
        public Button closeButton;

        public void Show(string text, Sprite image, System.Action unlockAction)
        {
            if (popupText != null)
                popupText.text = text;
            if (popupImage != null)
            {
                popupImage.sprite = image;
                popupImage.gameObject.SetActive(image != null);
            }
            if (unlockButton != null)
            {
                unlockButton.gameObject.SetActive(unlockAction != null);
                unlockButton.onClick.RemoveAllListeners();
                if (unlockAction != null)
                    unlockButton.onClick.AddListener(() => { unlockAction(); Destroy(gameObject); });
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => Destroy(gameObject));
            }
        }
    }
}