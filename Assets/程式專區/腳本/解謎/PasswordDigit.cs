using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace X
{
    public class PasswordDigit : MonoBehaviour
    {
        public int currentValue = 0;
        public TextMeshProUGUI digitText;
        public Button upButton;
        public Button downButton;

        private PasswordLockManager _manager;

        public void Init(PasswordLockManager manager)
        {
            _manager = manager;
            UpdateUI();

            // 綁定按鈕事件
            upButton.onClick.AddListener(() => ChangeValue(1));
            downButton.onClick.AddListener(() => ChangeValue(-1));
        }

        private void ChangeValue(int step)
        {
            // 0-7 循環邏輯
            currentValue = (currentValue + step + 8) % 8;
            UpdateUI();

            // 每次變動都通知管理器檢查一次
            _manager.CheckPassword();

            // 播放微小的點擊音效 (建議加入)
            _manager.PlayClickSound();
        }

        private void UpdateUI()
        {
            digitText.text = currentValue.ToString();
        }
    }
}