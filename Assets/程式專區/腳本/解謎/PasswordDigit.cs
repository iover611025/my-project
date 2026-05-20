using UnityEngine;
using UnityEngine.UI;

namespace X
{
    public class PasswordDigit : MonoBehaviour
    {
        public int currentValue = 1; // 預設從 1 開始
        public Image digitImage;
        public Sprite[] digitSprites; // 這裡存放 7 張圖片，對應數字 1-7

        public Button upButton;
        public Button downButton;

        private PasswordLockManager _manager;

        public void Init(PasswordLockManager manager)
        {
            _manager = manager;
            UpdateUI();

            upButton.onClick.AddListener(() => ChangeValue(1));
            downButton.onClick.AddListener(() => ChangeValue(-1));
        }

        private void ChangeValue(int step)
        {
            // --- 核心邏輯修改：1 到 7 的循環 ---
            // 先將值減 1 變回 0-6 範圍，進行循環運算後，再加 1 變回 1-7
            int index = currentValue - 1;
            index = (index + step + 7) % 7;
            currentValue = index + 1;
            // ----------------------------------

            UpdateUI();
            _manager.CheckPassword();
            _manager.PlayClickSound();
        }

        private void UpdateUI()
        {
            // 因為 currentValue 是 1-7，但陣列索引是 0-6
            int spriteIndex = currentValue - 1;

            if (digitSprites != null && spriteIndex >= 0 && spriteIndex < digitSprites.Length)
            {
                digitImage.sprite = digitSprites[spriteIndex];
            }
        }
    }
}