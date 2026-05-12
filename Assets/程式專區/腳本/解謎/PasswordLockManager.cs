using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using X;

namespace X
{

    public class PasswordLockManager : MonoBehaviour
    {
        [Header("密碼設定")]
        public int[] correctPassword;      // 例如 [1, 2, 3, 4]
        public List<PasswordDigit> digits; // 拖入場景中的數字位

        [Header("成功事件")]
        public UnityEvent OnUnlock;        // 解鎖後執行的動作
        public bool isLocked = true;

        [Header("音效回饋")]
        public AudioSource audioSource;
        public AudioClip clickClip;
        public AudioClip unlockClip;

        void Start()
        {
            // 初始化所有數字位
            foreach (var digit in digits)
            {
                digit.Init(this);
            }
        }

        public void CheckPassword()
        {
            if (!isLocked) return;

            bool isCorrect = true;
            for (int i = 0; i < correctPassword.Length; i++)
            {
                if (digits[i].currentValue != correctPassword[i])
                {
                    isCorrect = false;
                    break;
                }
            }

            if (isCorrect)
            {
                Unlock();
            }
        }

        private void Unlock()
        {
            isLocked = false;
            Debug.Log("密碼正確！箱子已開啟。");

            if (audioSource && unlockClip) audioSource.PlayOneShot(unlockClip);

            // 執行成功事件 (例如呼叫之前寫的 PanelActivator 開啟新畫面)
            OnUnlock?.Invoke();
        }

        public void PlayClickSound()
        {
            if (audioSource && clickClip) audioSource.PlayOneShot(clickClip);
        }
    }
}