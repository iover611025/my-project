using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace X
{
    /// <summary>
    /// 條件接收器。監聽 GameStateManager 的廣播，
    /// 當所需條件全數滿足時，執行相應的 UnityEvent。
    /// </summary>
    public class ConditionReceiver : MonoBehaviour
    {
        [Header("解謎需求狀態")]
        public List<string> requiredStates;

        [Header("條件達成時的事件")]
        public UnityEvent onConditionsMet;

        private bool _isTriggered = false;

        private void OnEnable()
        {
            // 每次房間開啟、物件啟動時，先檢查一次。
            // 這能確保玩家離開房間再回來時，如果條件已滿足，視覺能正確更新！
            CheckConditions();

            // 訂閱 Manager 的廣播事件
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateAdded += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            // 物件隱藏(房間關閉)時，取消訂閱，防止內存洩漏 (Memory Leak)
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateAdded -= OnStateChanged;
            }
        }

        // 當有任何新狀態加入時，觸發此方法
        private void OnStateChanged(string newState)
        {
            if (_isTriggered) return;
            CheckConditions();
        }

        private void CheckConditions()
        {
            if (_isTriggered || GameStateManager.Instance == null) return;

            // 呼叫 Manager 驗證是否所有字串條件都已滿足
            if (GameStateManager.Instance.HasAllStates(requiredStates))
            {
                _isTriggered = true;
                onConditionsMet?.Invoke();
            }
        }
    }
}