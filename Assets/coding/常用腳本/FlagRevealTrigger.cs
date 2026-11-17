using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace X
{
    /// <summary>
    /// 掛在「燈泡」（觸發器）上。
    /// 點擊燈泡後，會尋找場景中所有 FlagMarker 物件，
    /// 並讓有旗幟的物件顯示白框；再次點擊則隱藏所有白框。
    ///
    /// 使用方式：
    ///   1. 將此腳本掛在燈泡物件上（需有 Image 元件）。
    ///   2. 場景中所有要被標記的物件掛上 FlagMarker 腳本。
    ///   3. 點擊燈泡即可切換顯示模式（預設為 Toggle）。
    ///      若想用外部 OnClick() 控制，也可呼叫 Reveal() / Hide() / ToggleReveal()。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class FlagRevealTrigger : MonoBehaviour, IPointerClickHandler
    {
        [Header("搜尋範圍")]
        [Tooltip("要搜尋 FlagMarker 的根物件。留空則搜尋全場景（FindObjectsOfType）。")]
        [SerializeField] private Transform searchRoot;

        [Header("燈泡行為")]
        [Tooltip("點擊燈泡時的行為：Toggle（切換）、RevealOnly（只開）、HideOnly（只關）。")]
        [SerializeField] private TriggerMode mode = TriggerMode.Toggle;

        [Tooltip("燈泡初始是否處於顯示模式。")]
        [SerializeField] private bool startRevealed = false;

        public enum TriggerMode { Toggle, RevealOnly, HideOnly }

        private bool _isRevealed;

        // ────────────────────────────────────────────────────────────
        // Unity 生命週期
        // ────────────────────────────────────────────────────────────

        private void Start()
        {
            _isRevealed = startRevealed;
            BroadcastReveal(_isRevealed);
        }

        // ────────────────────────────────────────────────────────────
        // IPointerClickHandler：點擊燈泡
        // ────────────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (mode)
            {
                case TriggerMode.Toggle:     ToggleReveal(); break;
                case TriggerMode.RevealOnly: Reveal();       break;
                case TriggerMode.HideOnly:   Hide();         break;
            }
        }

        // ────────────────────────────────────────────────────────────
        // 公開方法：供 OnClick() / UnityEvent 呼叫
        // ────────────────────────────────────────────────────────────

        /// <summary>開啟顯示模式：所有有旗幟的物件顯示白框。</summary>
        public void Reveal()
        {
            _isRevealed = true;
            BroadcastReveal(true);
        }

        /// <summary>關閉顯示模式：隱藏所有白框。</summary>
        public void Hide()
        {
            _isRevealed = false;
            BroadcastReveal(false);
        }

        /// <summary>切換顯示模式。</summary>
        public void ToggleReveal()
        {
            _isRevealed = !_isRevealed;
            BroadcastReveal(_isRevealed);
        }

        /// <summary>查詢目前是否處於顯示模式。</summary>
        public bool IsRevealed => _isRevealed;

        // ────────────────────────────────────────────────────────────
        // 內部方法
        // ────────────────────────────────────────────────────────────

        /// <summary>將顯示狀態廣播給所有 FlagMarker。</summary>
        private void BroadcastReveal(bool active)
        {
            FlagMarker[] markers = searchRoot != null
                ? searchRoot.GetComponentsInChildren<FlagMarker>(includeInactive: true)
                : FindObjectsByType<FlagMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (FlagMarker marker in markers)
                marker.SetRevealActive(active);
        }
    }
}
