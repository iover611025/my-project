using UnityEngine;
using UnityEngine.UI;

namespace X
{
    /// <summary>
    /// 掛在「可被標記旗幟」的物件上。
    /// 旗幟可透過呼叫 AddFlag() / RemoveFlag() / ToggleFlag() 增刪。
    /// 白框由 FlagRevealTrigger（燈泡）統一控制，
    /// 只有在燈泡開啟 且 此物件有旗幟時才會顯示描邊外框。
    ///
    /// 外框做法：在此物件內建立四條細長 Image（上下左右），
    /// 向外延伸 borderSize 像素，拼成空心矩形描邊。
    /// （不填中間，純粹是邊框線條）
    ///
    /// 使用方式：
    ///   1. 將此腳本掛在目標 UI 物件上（需有 Image 元件）。
    ///   2. 在任意 OnClick() 或 UnityEvent 呼叫 AddFlag() / RemoveFlag() / ToggleFlag()。
    ///   3. 白框的顯示由 FlagRevealTrigger 統一控制，不需要額外設定。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class FlagMarker : MonoBehaviour
    {
        [Header("旗幟設定")]
        [Tooltip("物件的初始旗幟狀態。")]
        [SerializeField] private bool hasFlag = false;

        [Header("外框樣式")]
        [Tooltip("外框顏色（預設白色）。")]
        [SerializeField] private Color borderColor = Color.white;

        [Tooltip("外框線條粗細（像素）。")]
        [SerializeField] private float borderSize = 3f;

        private GameObject _borderContainer;
        private bool _revealActive = false;

        // ────────────────────────────────────────────────────────────
        // Unity 生命週期
        // ────────────────────────────────────────────────────────────

        private void Awake()
        {
            CreateBorderStrips();
            RefreshBorder();
        }

        private void OnDestroy()
        {
            if (_borderContainer != null)
                Destroy(_borderContainer);
        }

        // ────────────────────────────────────────────────────────────
        // 公開方法：供 OnClick() / UnityEvent 呼叫，管理旗幟狀態
        // ────────────────────────────────────────────────────────────

        /// <summary>增加旗幟。若燈泡已開啟，立即顯示描邊外框。</summary>
        public void AddFlag()
        {
            hasFlag = true;
            RefreshBorder();
        }

        /// <summary>移除旗幟，並立即隱藏描邊外框。</summary>
        public void RemoveFlag()
        {
            hasFlag = false;
            RefreshBorder();
        }

        /// <summary>切換旗幟狀態（有則移除，無則增加）。</summary>
        public void ToggleFlag()
        {
            hasFlag = !hasFlag;
            RefreshBorder();
        }

        /// <summary>查詢目前是否有旗幟。</summary>
        public bool HasFlag => hasFlag;

        // ────────────────────────────────────────────────────────────
        // 由 FlagRevealTrigger（燈泡）呼叫
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// 由燈泡呼叫，設定目前是否為「顯示模式」。
        /// 只有在顯示模式 且 有旗幟時才會顯示描邊外框。
        /// </summary>
        public void SetRevealActive(bool active)
        {
            _revealActive = active;
            RefreshBorder();
        }

        // ────────────────────────────────────────────────────────────
        // 內部方法
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// 建立四條描邊線條（上、下、左、右），
        /// 向外延伸 borderSize 像素，拼成空心矩形外框。
        /// 上下條包含四個角落，左右條只填側邊。
        /// </summary>
        private void CreateBorderStrips()
        {
            if (_borderContainer != null) return;

            // 建立容器（與此物件等大，作為四條線的父物件）
            _borderContainer = new GameObject("_FlagBorder");
            _borderContainer.transform.SetParent(transform, false);

            RectTransform containerRT = _borderContainer.AddComponent<RectTransform>();
            containerRT.anchorMin = Vector2.zero;
            containerRT.anchorMax = Vector2.one;
            containerRT.offsetMin = Vector2.zero;
            containerRT.offsetMax = Vector2.zero;

            float bs = borderSize;

            //  上邊：全寬（含四個角落）
            //  anchorMin=(0,1) anchorMax=(1,1) → 貼齊上緣，向上延伸 bs
            CreateStrip(_borderContainer.transform, "Top",
                anchorMin : new Vector2(0f, 1f),
                anchorMax : new Vector2(1f, 1f),
                offsetMin : new Vector2(-bs, 0f),
                offsetMax : new Vector2( bs, bs));

            //  下邊：全寬（含四個角落）
            CreateStrip(_borderContainer.transform, "Bottom",
                anchorMin : new Vector2(0f, 0f),
                anchorMax : new Vector2(1f, 0f),
                offsetMin : new Vector2(-bs, -bs),
                offsetMax : new Vector2( bs,  0f));

            //  左邊：只填高度（角落已由上下條覆蓋）
            CreateStrip(_borderContainer.transform, "Left",
                anchorMin : new Vector2(0f, 0f),
                anchorMax : new Vector2(0f, 1f),
                offsetMin : new Vector2(-bs, 0f),
                offsetMax : new Vector2( 0f, 0f));

            //  右邊：只填高度
            CreateStrip(_borderContainer.transform, "Right",
                anchorMin : new Vector2(1f, 0f),
                anchorMax : new Vector2(1f, 1f),
                offsetMin : new Vector2(0f, 0f),
                offsetMax : new Vector2(bs, 0f));

            _borderContainer.SetActive(false);
        }

        /// <summary>建立一條描邊 Image 並設定其 RectTransform。</summary>
        private void CreateStrip(Transform parent, string stripName,
                                  Vector2 anchorMin, Vector2 anchorMax,
                                  Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(stripName);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            Image img = go.AddComponent<Image>();
            img.color = borderColor;
            img.raycastTarget = false;
        }

        /// <summary>根據燈泡狀態與旗幟狀態決定外框是否顯示。</summary>
        private void RefreshBorder()
        {
            if (_borderContainer == null) return;
            _borderContainer.SetActive(_revealActive && hasFlag);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_borderContainer == null) return;
            foreach (Image img in _borderContainer.GetComponentsInChildren<Image>(true))
                img.color = borderColor;
        }
#endif
    }
}
