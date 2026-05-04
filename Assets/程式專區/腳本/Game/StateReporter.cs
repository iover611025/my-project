using UnityEngine;

namespace X
{
    /// <summary>
    /// 狀態回傳器。可以掛載在蝴蝶、開花的花盆等物件上。
    /// 當此物件被啟動 (OnEnable) 或被外部事件觸發時，向 Manager 註冊狀態。
    /// </summary>
    public class StateReporter : MonoBehaviour
    {
        [Header("要註冊的狀態名稱")]
        public string stateToReport;

        [Header("觸發時機")]
        [Tooltip("如果勾選，當這個 GameObject 被 SetActive(true) 時自動回傳")]
        public bool reportOnEnable = true;

        private void OnEnable()
        {
            // 房間切換時，如果此物件跟著房間被開啟，它會重新回傳。
            // 但因為 HashSet 的特性，重複加入不會產生副作用。
            if (reportOnEnable && GameStateManager.Instance != null)
            {
                ReportState();
            }
        }

        /// <summary>
        /// 供按鈕或自訂腳本 (如 HeldItemTrigger) 在完成操作後手動呼叫
        /// </summary>
        public void ReportState()
        {
            if (!string.IsNullOrEmpty(stateToReport))
            {
                GameStateManager.Instance.AddState(stateToReport);
            }
        }
    }
}