using UnityEngine;

namespace X
{
    /// <summary>
    /// 線索本門面腳本。
    /// 對外提供 SwitchNext / SwitchPrevious，
    /// 內部委派給 ClueBookPageUnlocker 執行「跳過未解鎖頁」的翻頁邏輯。
    /// 將此腳本的 SwitchNext / SwitchPrevious 綁定到 UI 翻頁按鈕的 onClick。
    /// </summary>
    public class ClueBook : MonoBehaviour
    {
        [Header("相依元件")]
        [SerializeField] private ObjectSequenceSwitcher switcher;
        [SerializeField] private ClueBookPageUnlocker unlocker;

        private void Reset()
        {
            // 在 Inspector 重置時自動嘗試抓取同 GameObject 上的元件
            switcher = GetComponent<ObjectSequenceSwitcher>();
            unlocker = GetComponent<ClueBookPageUnlocker>();
        }

        // ──────────────────────────────────────────────
        // 翻頁 API（供 UI 按鈕綁定）
        // ──────────────────────────────────────────────

        /// <summary>
        /// 顯示當前序列的「下一頁」，自動跳過未解鎖頁面。
        /// </summary>
        public void SwitchNext()
        {
            if (unlocker != null)
                unlocker.SwitchNext();
            else
                switcher?.SwitchNext();
        }

        /// <summary>
        /// 顯示當前序列的「上一頁」，自動跳過未解鎖頁面。
        /// </summary>
        public void SwitchPrevious()
        {
            if (unlocker != null)
                unlocker.SwitchPrevious();
            else
                switcher?.SwitchPrevious();
        }
    }
}
