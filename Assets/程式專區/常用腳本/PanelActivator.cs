using UnityEngine;
using UnityEngine.EventSystems;

namespace X
{

    public class PanelActivator : MonoBehaviour, IPointerClickHandler
    {
        [Header("Panel 設定")]
        public GameObject panelToOpen;

        // 新增：設定開啟此面板時，是否要關閉攝影機晃動
        [Header("攝影機控制")]
        public bool disableSwayOnOpen = true;

        [Header("Return 設定 (由 Manager 統一生成)")]
        public GameObject returnPrefab;
        public Canvas returnParentCanvas;

        [Header("鏽湖式透明度設定")]
        public float revealRadius = 200f;
        [Range(0f, 1f)] public float minAlpha = 0f;
        [Range(0f, 1f)] public float maxAlpha = 1f;
        public AnimationCurve proximityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // 確保命名空間正確，並加入判斷

        public void OnPointerClick(PointerEventData eventData)
        {
            if (panelToOpen == null) return;

            // 執行關閉晃動
            if (disableSwayOnOpen && CameraFollowMouse.Instance != null)
            {
                CameraFollowMouse.Instance.SetSwayActive(false);
            }

            UIPanelManager.Instance.PushPanel(panelToOpen, this);
        }
    }
}