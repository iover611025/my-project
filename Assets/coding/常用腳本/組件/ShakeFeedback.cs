using UnityEngine;
using Unity.Cinemachine;

namespace X
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class ShakeFeedback : MonoBehaviour
    {
        [Header("晃動設定")]
        [Tooltip("預設的晃動強度倍率")]
        public float defaultForce = 1.0f;

        [Tooltip("是否在物件啟用 (OnEnable) 時自動觸發一次？")]
        public bool playOnEnable = false;

        private CinemachineImpulseSource _impulseSource;

        private void Awake()
        {
            // 快取震波發射器元件
            _impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        private void OnEnable()
        {
            // 如果勾選了啟用時自動播放，則立刻觸發
            if (playOnEnable)
            {
                PlayShake();
            }
        }

        /// <summary>
        /// 提供給外部事件 (如 UnityEvent 或其他腳本) 呼叫的方法。
        /// 依照 Inspector 中設定的預設強度播放晃動。
        /// </summary>
        public void PlayShake()
        {
            if (_impulseSource == null) _impulseSource = GetComponent<CinemachineImpulseSource>();

            Debug.Log($"[ShakeFeedback] 觸發 PlayShake，預設強度: {defaultForce}");
            if (_impulseSource != null)
            {
                _impulseSource.GenerateImpulseWithForce(defaultForce);
            }
            else
            {
                Debug.LogError("[ShakeFeedback] 嚴重警告：此物件上完全沒有掛載 CinemachineImpulseSource 元件！請在 Inspector 中手動加入它！");
            }
        }

        /// <summary>
        /// 提供給需要動態改變強度的進階呼叫。
        /// </summary>
        /// <param name="customForce">自訂的強度倍率</param>
        public void PlayShakeWithForce(float customForce)
        {
            if (_impulseSource == null) _impulseSource = GetComponent<CinemachineImpulseSource>();

            Debug.Log($"[ShakeFeedback] 觸發 PlayShakeWithForce，自訂強度: {customForce}");
            if (_impulseSource != null)
            {
                _impulseSource.GenerateImpulseWithForce(customForce);
            }
            else
            {
                Debug.LogError("[ShakeFeedback] 嚴重警告：此物件上完全沒有掛載 CinemachineImpulseSource 元件！請在 Inspector 中手動加入它！");
            }
        }
    }
}