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
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class PlaySoundOnClick : MonoBehaviour, IPointerClickHandler
    {
        [Header("音效")]
        [Tooltip("點擊時播放的音效。")]
        [SerializeField] private AudioClip clip;

        [Tooltip("音量（0 ~ 1）")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [Header("冷卻（選填）")]
        [Tooltip("兩次播音之間最短間隔。設為 0 則無限制。")]
        [SerializeField, Min(0f)] private float cooldown = 0f;

        private float _lastPlayTime = -999f;

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

            // 冷卻檢查
            if (cooldown > 0f && Time.time - _lastPlayTime < cooldown) return;
            _lastPlayTime = Time.time;

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