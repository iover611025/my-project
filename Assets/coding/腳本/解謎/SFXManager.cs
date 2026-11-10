using UnityEngine;

namespace X
{
    /// <summary>
    /// 全場景共用的音效管理器（Singleton）。
    ///
    /// 使用方式：
    ///   1. 在場景中任意 GameObject 掛上此腳本（建議命名為「SFXManager」）。
    ///   2. 其他腳本呼叫：
    ///        SFXManager.Instance.Play(clip);
    ///        SFXManager.Instance.Play(clip, volume);
    ///
    /// 注意：
    ///   此 Manager 只管 SFX（音效）。
    ///   背景音樂請另外處理。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SFXManager : MonoBehaviour
    {
        // ────────────────────────────────────────
        // Singleton
        // ────────────────────────────────────────

        private static SFXManager _instance;

        public static SFXManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<SFXManager>();
                return _instance;
            }
        }

        // ────────────────────────────────────────
        // 內部
        // ────────────────────────────────────────

        private AudioSource _audioSource;

        /// <summary>
        /// 點擊音效
        /// </summary>
        public AudioSource _clickSFX;

        public AudioClip _clickSFX2;

        private void Awake()
        {
            // 防止重複
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _audioSource = GetComponent<AudioSource>();
        }

        // ────────────────────────────────────────
        // 公開 API
        // ────────────────────────────────────────

        /// <summary>
        /// 播放一次音效（不打斷當前音效）。
        /// </summary>
        public void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            _audioSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// 停止所有正在播放的音效。
        /// </summary>
        public void Stop()
        {
            _audioSource.Stop();
        }

        public void PlayClickSFX()
        {
            _clickSFX.Play();
        }
    }
}