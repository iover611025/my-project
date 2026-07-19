using UnityEngine;
using System.Collections;
using UnityEngine.Events; // 引入 UnityEvent 命名空間

namespace X
{
    public class AutoDisableTimer : MonoBehaviour
    {
        [Header("計時設定")]
        public float duration = 2f;
        public bool isRepeating = false;

        [Header("目標設定")]
        public GameObject targetObject;

        [Tooltip("計時結束時，要同時被開啟（SetActive true）的物件清單。留空則不額外開啟任何物件。")]
        public GameObject[] objectsToEnableOnEnd;

        [Header("計時期間暫停設定")]
        [Tooltip("計時期間要被隱藏且停止互動的 CanvasGroup（例如：圖片返回 Prefab 上的 CanvasGroup）")]
        public CanvasGroup pausedDuringTimer;

        [Header("自訂事件擴充")]
        [Tooltip("當計時器啟動時，想要同時觸發什麼事件？(可拖曳 ShakeFeedback 或播放音效)")]
        public UnityEvent onTimerStart; 

        private WaitForSeconds _cachedWait;

        private void Awake()
        {
            if (targetObject == null) targetObject = gameObject;
            _cachedWait = new WaitForSeconds(duration);
        }

        private void OnEnable()
        {
            // 計時開始：隱藏並鎖定指定的 CanvasGroup
            SetCanvasGroupPaused(true);

            // 觸發在 Inspector 中設定的所有事件 (例如：呼叫 ShakeFeedback.PlayShake)
            onTimerStart?.Invoke();
            
            StartCoroutine(TimerRoutine());
        }

        private void OnDisable()
        {
            // 若計時器所在物件被關閉，恢復 CanvasGroup（防止殘留狀態）
            SetCanvasGroupPaused(false);
        }

        private IEnumerator TimerRoutine()
        {
            while (true)
            {
                yield return _cachedWait;

                if (isRepeating)
                {
                    targetObject.SetActive(!targetObject.activeSelf);
                }
                else
                {
                    // 計時結束：先恢復 CanvasGroup，再關閉目標物件
                    SetCanvasGroupPaused(false);
                    targetObject.SetActive(false);

                    // 開啟所有指定的物件
                    if (objectsToEnableOnEnd != null)
                    {
                        foreach (var obj in objectsToEnableOnEnd)
                        {
                            if (obj != null) obj.SetActive(true);
                        }
                    }

                    yield break; 
                }
            }
        }

        /// <summary>
        /// 設定 CanvasGroup 的暫停狀態。
        /// paused=true  → 不顯示、不可互動、不擋射線
        /// paused=false → 恢復正常顯示與互動
        /// </summary>
        private void SetCanvasGroupPaused(bool paused)
        {
            if (pausedDuringTimer == null) return;

            pausedDuringTimer.alpha          = paused ? 0f : 1f;
            pausedDuringTimer.interactable   = !paused;
            pausedDuringTimer.blocksRaycasts = !paused;
        }
    }
}