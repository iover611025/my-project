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
            // 觸發在 Inspector 中設定的所有事件 (例如：呼叫 ShakeFeedback.PlayShake)
            onTimerStart?.Invoke();
            
            StartCoroutine(TimerRoutine());
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
                    targetObject.SetActive(false);
                    yield break; 
                }
            }
        }
    }
}