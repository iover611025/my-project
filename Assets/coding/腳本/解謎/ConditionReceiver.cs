using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace X
{
    /// <summary>
    /// 監聽來自 GameStateManager 狀態，
    /// 當需求狀態符合、且指定物件全部為 activeInHierarchy 時，才觸發 UnityEvent。
    /// 即使物件晚於狀態滿足後才開啟，仍會正確觸發。
    /// </summary>
    public class ConditionReceiver : MonoBehaviour
    {
        [Header("需求狀態")]
        public List<string> requiredStates;

        [Header("需要開啟的物件（全部 active 才觸發）")]
        [Tooltip("若此清單不為空，條件符合時還需確保所有指定物件均為 activeInHierarchy，才會觸發事件。\n即使物件晚於狀態滿足後才開啟，也會自動偵測。")]
        public List<GameObject> requiredActiveObjects;

        [Header("符合時的事件")]
        public UnityEvent onConditionsMet;

        [Header("等待物件開啟的輪詢間隔（秒）")]
        [Tooltip("狀態已滿足但物件尚未全部開啟時，每隔此秒數重新檢查一次物件狀態。")]
        public float checkInterval = 0.2f;

        private bool _isTriggered = false;
        /// <summary>所有 requiredStates 已滿足，正在等待物件全部開啟</summary>
        private bool _statesMet = false;

        private void OnEnable()
        {
            CheckConditions();

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateAdded += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateAdded -= OnStateChanged;
            }

            // 停用時取消輪詢
            CancelInvoke(nameof(PollActiveObjects));
        }

        private void OnStateChanged(string newState)
        {
            if (_isTriggered) return;
            CheckConditions();
        }

        private void CheckConditions()
        {
            if (_isTriggered || GameStateManager.Instance == null) return;

            // 狀態尚未全部滿足 → 直接離開
            if (!GameStateManager.Instance.HasAllStates(requiredStates)) return;

            // 狀態已滿足
            _statesMet = true;

            // 嘗試立刻完成（物件可能已是開啟的）
            if (TryTrigger()) return;

            // 物件尚未全部開啟 → 開始輪詢等待
            if (!IsInvoking(nameof(PollActiveObjects)))
            {
                InvokeRepeating(nameof(PollActiveObjects), checkInterval, checkInterval);
            }
        }

        /// <summary>輪詢：狀態已滿足後，等待所有物件變為 active</summary>
        private void PollActiveObjects()
        {
            if (_isTriggered)
            {
                CancelInvoke(nameof(PollActiveObjects));
                return;
            }

            TryTrigger();
        }

        /// <summary>
        /// 嘗試觸發事件。
        /// 若所有物件已 active 則觸發並停止輪詢，回傳 true；否則回傳 false。
        /// </summary>
        private bool TryTrigger()
        {
            if (!AreAllRequiredObjectsActive()) return false;

            CancelInvoke(nameof(PollActiveObjects));
            _isTriggered = true;
            onConditionsMet?.Invoke();
            return true;
        }

        private bool AreAllRequiredObjectsActive()
        {
            if (requiredActiveObjects == null || requiredActiveObjects.Count == 0) return true;

            foreach (var obj in requiredActiveObjects)
            {
                if (obj == null || !obj.activeInHierarchy) return false;
            }
            return true;
        }
    }
}
