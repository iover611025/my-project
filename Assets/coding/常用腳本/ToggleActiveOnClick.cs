using UnityEngine;
using UnityEngine.UI;

namespace X
{
    /// <summary>
    /// 通過點擊 (Toggle) 開關目標物件。
    /// 音效播放優先透過 SFXManager；若場景中無 SFXManager 則備援至自身 AudioSource
    /// 或 AudioSource.PlayClipAtPoint。
    ///
    /// 使用方式：
    ///   1. 將此腳本掛在「按鈕」上。
    ///   2. 在 Target Object 填入目標 Panel。
    ///   3. 在 Button 的 OnClick() 加入 Toggle() 方法。
    /// </summary>
    public class ToggleActiveOnClick : MonoBehaviour
    {
        [Header("目標物件")]
        [Tooltip("要控制的目標物件。若空白，預設自己。")]
        [SerializeField] private GameObject targetObject;

        [Header("切換圖示顯示")]
        [Tooltip("點擊時是否同步切換自身圖示？")]
        [SerializeField] private bool changeButtonSprite = false;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Sprite openedSprite;
        [SerializeField] private Sprite closedSprite;

        [Header("音效")]
        [Tooltip("開啟時播放的音效（可自行拖入音效檔）。")]
        [SerializeField] private AudioClip openAudio;
        [Tooltip("關閉時播放的音效（可自行拖入音效檔）。")]
        [SerializeField] private AudioClip closeAudio;
        [Tooltip("備援用 AudioSource（選填）。優先使用 SFXManager；都沒有則用 PlayClipAtPoint。")]
        [SerializeField] private AudioSource audioSource;

        private void Awake()
        {
            if (targetObject == null)
                targetObject = gameObject;

            if (changeButtonSprite && buttonImage == null)
            {
                buttonImage = GetComponent<Image>();
                if (buttonImage != null && closedSprite == null)
                    closedSprite = buttonImage.sprite;
            }

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            UpdateVisual();
        }

        /// <summary>切換目標物件狀態（開則關，關則開）。</summary>
        public void Toggle()
        {
            if (targetObject == null) return;

            bool nextState = !targetObject.activeSelf;
            targetObject.SetActive(nextState);

            PlayAudio(nextState ? openAudio : closeAudio);
            UpdateVisual();
        }

        /// <summary>強制開啟目標物件。</summary>
        public void Open()
        {
            if (targetObject == null) return;
            targetObject.SetActive(true);
            PlayAudio(openAudio);
            UpdateVisual();
        }

        /// <summary>強制關閉目標物件。</summary>
        public void Close()
        {
            if (targetObject == null) return;
            targetObject.SetActive(false);
            PlayAudio(closeAudio);
            UpdateVisual();
        }

        private void PlayAudio(AudioClip clip)
        {
            if (clip == null) return;

            // 1. 優先：SFXManager
            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.Play(clip);
                return;
            }

            // 2. 備援：自身 AudioSource
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip);
                return;
            }

            // 3. 最終備援：PlayClipAtPoint
            Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(clip, pos);
        }

        private void UpdateVisual()
        {
            if (!changeButtonSprite || buttonImage == null || targetObject == null) return;

            bool isOpen = targetObject.activeSelf;
            if (isOpen && openedSprite != null)
                buttonImage.sprite = openedSprite;
            else if (!isOpen && closedSprite != null)
                buttonImage.sprite = closedSprite;
        }
    }
}