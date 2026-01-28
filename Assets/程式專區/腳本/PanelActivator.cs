using UnityEngine;
using UnityEngine.EventSystems;

namespace X
{
    /// <summary>
    /// 點擊某個可互動物件時啟用指定 panel（可選同時關閉另一個 panel）。
    /// 在 Inspector 指派 panelToOpen（以及可選的 panelToClose）。
    /// </summary>
    public class PanelActivator : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("要啟用的 Panel（GameObject）")]
        public GameObject panelToOpen;

        [Tooltip("可選：要關閉的 Panel（啟用時會被關閉）")]
        public GameObject panelToClose;

        [Tooltip("點擊後是否只在啟用 panelToOpen 時呼叫 SetActive(true)（預設 true）")]
        public bool openOnly = true;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (panelToOpen != null)
            {
                panelToOpen.SetActive(true);
            }

            if (panelToClose != null)
            {
                panelToClose.SetActive(false);
            }
        }
    }
}