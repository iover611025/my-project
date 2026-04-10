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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (panelToOpen == null) return;

            // 開啟面板時關閉晃動
            if (CameraFollowMouse.Instance != null)
            {
                CameraFollowMouse.Instance.SetSwayActive(false);
            }

            UIPanelManager.Instance.PushPanel(panelToOpen, this);
        }

        // 提示：你需要在 UIPanelManager 的 PopPanel (關閉 UI) 時，
        // 重新將 CameraFollowMouse.Instance.isSwaying 設為 true。
    }
}