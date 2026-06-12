using UnityEngine;
using Unity.Cinemachine; // Unity 6 (Cinemachine 3.x) 的專屬命名空間

namespace X
{
    // 強制要求掛載此腳本的物件也必須具備 CinemachineImpulseSource 元件
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShakeController : MonoBehaviour
    {
        private CinemachineImpulseSource _impulseSource;

        private void Awake()
        {
            // 快取元件，避免在執行期間使用 GetComponent 造成效能浪費
            _impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        /// <summary>
        /// 觸發預設強度的晃動 (適用於一般機關觸發)
        /// </summary>
        public void TriggerDefaultShake()
        {
            Debug.Log("[CameraShakeController] 觸發 TriggerDefaultShake");
            if (_impulseSource == null) Debug.LogWarning("[CameraShakeController] 缺少 CinemachineImpulseSource！");
            
            // GenerateImpulse 會直接讀取你在 Inspector 中設定的 Amplitude 與 Frequency
            _impulseSource?.GenerateImpulse();
        }

        /// <summary>
        /// 觸發自訂強度的晃動 (適用於動態事件，例如物件越重，震動越大)
        /// </summary>
        /// <param name="forceMultiplier">強度倍率 (例如 0.5 為減半，2.0 為兩倍)</param>
        public void TriggerCustomShake(float forceMultiplier)
        {
            Debug.Log($"[CameraShakeController] 觸發 TriggerCustomShake，強度倍率: {forceMultiplier}");
            if (_impulseSource == null) Debug.LogWarning("[CameraShakeController] 缺少 CinemachineImpulseSource！");
            
            // GenerateImpulseWithForce 可以在不修改預設設定的情況下，動態放大或縮小震波強度
            _impulseSource?.GenerateImpulseWithForce(forceMultiplier);
        }
        
        /// <summary>
        /// 觸發帶有方向性的晃動 (例如：角色撞到右邊的牆壁)
        /// </summary>
        public void TriggerDirectionalShake(Vector3 direction)
        {
            Debug.Log($"[CameraShakeController] 觸發 TriggerDirectionalShake，方向: {direction}");
            if (_impulseSource == null) Debug.LogWarning("[CameraShakeController] 缺少 CinemachineImpulseSource！");
            
            // 將震動波賦予特定的物理方向向量
            _impulseSource?.GenerateImpulse(direction);
        }
    }
}