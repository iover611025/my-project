using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace X
{
    /// <summary>
    /// 點擊後透過 SFXManager 播放音效。
    /// 只需掛上腳本並拖入音效，不需手動綁定 OnClick()。
    ///
    /// 設計原則：
    ///   只使用 IPointerClickHandler，不訂閱 Button.onClick。
    ///   因此不受 DisableButtonClick（會清空 onClick）的影響。
    ///   Button 若被設為 interactable = false，點擊事件才會真正停止。
    ///
    /// 播放模式（Inspector 切換）：
    ///   Overlap  — 允許重疊播放（PlayOneShot 疊加）。
    ///   NoRepeat — 播放期間忽略新的觸發，clip 播完後才能再次播放。
    ///   Cooldown — 兩次播放之間需等待固定冷卻時間（秒）。
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class PlaySoundOnClick : MonoBehaviour, IPointerClickHandler
    {
        // ────────────────────────────────────────
        // 播放模式
        // ────────────────────────────────────────

        public enum PlayMode
        {
            /// <summary>允許重疊播放（每次點擊都播）。</summary>
            Overlap,
            /// <summary>播放期間忽略新觸發，clip 播完後才解鎖。</summary>
            NoRepeat,
            /// <summary>兩次播放之間需等待固定冷卻時間。</summary>
            Cooldown
        }

        [Header("播放模式")]
        [Tooltip("Overlap：允許重疊播放。\nNoRepeat：播放期間忽略新觸發。\nCooldown：兩次播放需間隔指定秒數。")]
        [SerializeField] private PlayMode playMode = PlayMode.NoRepeat;

        [Tooltip("【Cooldown 模式】兩次播音之間的最短間隔（秒）。")]
        [SerializeField, Min(0f)] private float cooldown = 0.5f;

        // ────────────────────────────────────────
        // 音效設定
        // ────────────────────────────────────────

        [Header("音效")]
        [Tooltip("點擊時播放的音效。")]
        [SerializeField] private AudioClip clip;

        [Tooltip("音量（0 ~ 1）")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        // ────────────────────────────────────────
        // 內部狀態
        // ────────────────────────────────────────

        /// <summary>NoRepeat 模式：clip 預計播放結束的 Time.time。</summary>
        private float _playEndTime = -1f;

        /// <summary>Cooldown 模式：上次成功播放的 Time.time。</summary>
        private float _lastPlayTime = -999f;

        // ────────────────────────────────────────
        // Unity
        // ────────────────────────────────────────

        private void Awake()
        {
            // 確保 Graphic 可以接收射線（raycast）才能觸發點擊
            var graphic = GetComponent<Graphic>();
            if (graphic != null)
                graphic.raycastTarget = true;
        }

        // ────────────────────────────────────────
        // IPointerClickHandler
        // 注意：Button 的 interactable=false 時此事件不會觸發
        // ────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            PlaySound();
        }

        // ────────────────────────────────────────
        // 公開 API（供其他腳本呼叫）
        // ────────────────────────────────────────

        /// <summary>播放預設音效。</summary>
        public void PlaySound()
        {
            Play(clip);
        }

        /// <summary>播放指定音效。</summary>
        public void PlaySound(AudioClip overrideClip)
        {
            Play(overrideClip);
        }

        // ────────────────────────────────────────
        // 內部
        // ────────────────────────────────────────

        private void Play(AudioClip target)
        {
            if (target == null) return;

            switch (playMode)
            {
                case PlayMode.NoRepeat:
                    // 播放保護：clip 尚未播完則忽略
                    if (Time.time < _playEndTime) return;
                    _playEndTime = Time.time + target.length;
                    break;

                case PlayMode.Cooldown:
                    // 冷卻檢查：距上次播放未超過冷卻時間則忽略
                    if (Time.time - _lastPlayTime < cooldown) return;
                    _lastPlayTime = Time.time;
                    break;

                // PlayMode.Overlap：無限制，直接播
            }

            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.Play(target, volume);
            }
            else
            {
                Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
                AudioSource.PlayClipAtPoint(target, pos, volume);
            }
        }
    }
}
