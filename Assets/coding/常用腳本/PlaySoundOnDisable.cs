using UnityEngine;

namespace X
{
    /// <summary>
    /// 掛在「想在被關閉時播放音效」的物件上。
    ///
    /// 播放交由 SFXManager 處理，不需要自帶 AudioSource。
    /// 若場景中沒有 SFXManager，則自動退回 AudioSource.PlayClipAtPoint。
    ///
    /// 使用方式：
    ///   1. 將此腳本掛在目標物件上。
    ///   2. 在 Inspector 的 Close Clip 拖入音效檔即可。
    ///   3. 選填：Open Clip 在物件被啟用時播放。
    /// </summary>
    public class PlaySoundOnDisable : MonoBehaviour
    {
        [Header("音效")]
        [Tooltip("物件被關閉（SetActive false）時播放。")]
        [SerializeField] private AudioClip closeClip;

        [Tooltip("物件被開啟（OnEnable）時播放。")]
        [SerializeField] private AudioClip openClip;

        [Tooltip("音量（0 ~ 1）")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        // 避免第一次 Awake/OnEnable 時誤播開啟音效
        private bool _isFirstEnable = true;

        private void OnEnable()
        {
            if (_isFirstEnable)
            {
                _isFirstEnable = false;
                return;
            }

            PlayClip(openClip);
        }

        private void OnDisable()
        {
            // 場景卸載或應用程式結束時不播
            if (!gameObject.scene.isLoaded) return;

            PlayClip(closeClip);
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null) return;

            // 優先使用 SFXManager
            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.Play(clip, volume);
                return;
            }

            // 備援：PlayClipAtPoint（自建臨時物件）
            Vector3 pos = Camera.main != null
                ? Camera.main.transform.position
                : Vector3.zero;
            AudioSource.PlayClipAtPoint(clip, pos, volume);
        }
    }
}