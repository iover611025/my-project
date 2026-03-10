using UnityEngine;
using UnityEngine.EventSystems;

namespace X
{
    public class PanelActivator : MonoBehaviour, IPointerClickHandler
    {
        [Header("Panel 設定")]
        public GameObject panelToOpen;

        [Header("Return 設定 (由 Manager 統一生成)")]
        public GameObject returnPrefab;
        public Canvas returnParentCanvas;

        [Header("鏽湖式透明度設定")]
        public float revealRadius = 200f;
        [Range(0f, 1f)] public float minAlpha = 0f;
        [Range(0f, 1f)] public float maxAlpha = 1f;
        public AnimationCurve proximityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (panelToOpen == null) return;

            // 呼叫管理器處理堆疊
            UIPanelManager.Instance.PushPanel(panelToOpen, this);
        }
    }
}