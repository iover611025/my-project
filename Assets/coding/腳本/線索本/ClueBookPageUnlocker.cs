using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace X
{
    /// <summary>
    /// 線索本頁面解鎖系統。
    ///
    /// 掛在與 ObjectSequenceSwitcher 相同的 GameObject 上。
    /// 透過 GameStateManager 的字串狀態驅動兩層解鎖：
    ///   1. Sequence 解鎖：整個頁籤的 triggerObject 隱藏 → 顯示。
    ///   2. Page 解鎖：翻頁時自動跳過尚未解鎖的頁面。
    ///
    /// 解鎖狀態透過 PlayerPrefs 持久化，重啟遊戲後仍保留。
    /// </summary>
    public class ClueBookPageUnlocker : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        // 資料結構
        // ──────────────────────────────────────────────

        [System.Serializable]
        public class PageUnlockConfig
        {
            [Tooltip("對應 Sequence.objects 的索引（0-based）")]
            public int pageIndex;

            [Tooltip("解鎖此頁所需的 GameState 字串（全部滿足才解鎖）")]
            public List<string> requiredStates = new List<string>();

            [Tooltip("解鎖時觸發的事件（可掛接動畫、音效等）")]
            public UnityEvent onPageUnlocked;

            // [System.NonSerialized]：Unity 不序列化此欄位，
            // 確保每次 Play 都從 false 開始，由 RestoreFromPrefs 決定初始狀態。
            [System.NonSerialized] public bool isUnlocked = false;
        }

        [System.Serializable]
        public class SequenceUnlockConfig
        {
            [Tooltip("對應 ObjectSequenceSwitcher.sequences 的索引（0-based）")]
            public int sequenceIndex;

            [Tooltip("留空代表初始就解鎖此頁籤（可在遊戲一開始就顯示的頁籤）")]
            public List<string> requiredStatesToUnlock = new List<string>();

            [Tooltip("解鎖時觸發的事件（可掛接頁籤出現動畫、音效等）")]
            public UnityEvent onSequenceUnlocked;

            [Tooltip("此頁籤內各頁面的解鎖設定（留空代表所有頁面初始就可見）")]
            public List<PageUnlockConfig> pages = new List<PageUnlockConfig>();

            // [System.NonSerialized]：Unity 不序列化此欄位，
            // 確保每次 Play 都從 false 開始，由 RestoreFromPrefs 決定初始狀態。
            [System.NonSerialized] public bool isUnlocked = false;
        }

        // ──────────────────────────────────────────────
        // Inspector 設定
        // ──────────────────────────────────────────────

        [Header("相依元件")]
        [SerializeField] private ObjectSequenceSwitcher switcher;

        [Header("各頁籤的解鎖設定")]
        [SerializeField] private List<SequenceUnlockConfig> sequenceConfigs = new List<SequenceUnlockConfig>();

        [Header("PlayerPrefs 前綴（用於隔離不同場景的存檔）")]
        [Tooltip("建議使用場景名稱，例如 'Bedroom_'")]
        [SerializeField] private string saveKeyPrefix = "ClueBook_";

        // ──────────────────────────────────────────────
        // 常數
        // ──────────────────────────────────────────────

        private const string KEY_SEQ  = "Seq";
        private const string KEY_PAGE = "Page";

        // ──────────────────────────────────────────────
        // 生命週期
        // ──────────────────────────────────────────────

        private void Reset()
        {
            switcher = GetComponent<ObjectSequenceSwitcher>();
        }

        private void Awake()
        {
            if (switcher == null)
                switcher = GetComponent<ObjectSequenceSwitcher>();

            // 從 PlayerPrefs 還原上一次的解鎖狀態（不觸發 UnityEvent）
            RestoreFromPrefs();
        }

        private void OnEnable()
        {
            // 訂閱 GameStateManager 廣播
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateAdded += OnStateAdded;

            // 做一次全量評估（確保 OnEnable 時 UI 狀態正確）
            EvaluateAll(fireEvents: false);
            ApplyAllUI();
        }

        private void Start()
        {
            // OnEnable 時 GameStateManager 可能還未初始化（執行順序問題）。
            // Start 時再補做一次評估，確保「不需要條件」的序列能正確顯示。
            EvaluateAll(fireEvents: false);
            ApplyAllUI();

            // 若 OnEnable 時沒訂閱成功，在 Start 補訂閱
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateAdded -= OnStateAdded; // 防止重複訂閱
                GameStateManager.Instance.OnStateAdded += OnStateAdded;
            }
        }

        private void OnDisable()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateAdded -= OnStateAdded;
        }

        // ──────────────────────────────────────────────
        // 翻頁 API（供 ClueBook 門面腳本呼叫）
        // ──────────────────────────────────────────────

        /// <summary>
        /// 在當前序列中往後翻，自動跳過未解鎖頁面。
        /// </summary>
        public void SwitchNext()
        {
            if (switcher == null) return;

            int seqIdx = switcher.ActiveSequenceIndex;
            if (seqIdx < 0) return;

            int total   = switcher.GetObjectsCount(seqIdx);
            if (total == 0) return;

            int current = switcher.CurrentObjectIndex;
            int next    = FindNextUnlocked(seqIdx, current, total, direction: 1);
            if (next >= 0 && next != current)
                switcher.JumpToPage(seqIdx, next);
        }

        /// <summary>
        /// 在當前序列中往前翻，自動跳過未解鎖頁面。
        /// </summary>
        public void SwitchPrevious()
        {
            if (switcher == null) return;

            int seqIdx = switcher.ActiveSequenceIndex;
            if (seqIdx < 0) return;

            int total   = switcher.GetObjectsCount(seqIdx);
            if (total == 0) return;

            int current = switcher.CurrentObjectIndex;
            int prev    = FindNextUnlocked(seqIdx, current, total, direction: -1);
            if (prev >= 0 && prev != current)
                switcher.JumpToPage(seqIdx, prev);
        }

        // ──────────────────────────────────────────────
        // 公開查詢 API
        // ──────────────────────────────────────────────

        /// <summary>查詢指定頁籤是否已解鎖</summary>
        public bool IsSequenceUnlocked(int seqIndex)
        {
            var cfg = FindSequenceCfg(seqIndex);
            return cfg == null || cfg.isUnlocked; // 沒有設定 = 預設解鎖
        }

        /// <summary>查詢指定頁籤內的指定頁面是否已解鎖</summary>
        public bool IsPageUnlocked(int seqIndex, int pageIndex)
        {
            var cfg = FindSequenceCfg(seqIndex);
            if (cfg == null) return true; // 頁籤沒有設定 = 所有頁預設解鎖

            var pageCfg = cfg.pages.Find(p => p.pageIndex == pageIndex);
            return pageCfg == null || pageCfg.isUnlocked; // 頁面沒有設定 = 預設解鎖
        }

        // ──────────────────────────────────────────────
        // 狀態驅動
        // ──────────────────────────────────────────────

        private void OnStateAdded(string _)
        {
            EvaluateAll(fireEvents: true);
            ApplyAllUI();
        }

        /// <summary>
        /// 掃描所有設定，對新解鎖的項目更新狀態、觸發事件並存檔。
        /// </summary>
        private void EvaluateAll(bool fireEvents)
        {
            // 注意：不在此處做 GameStateManager null 全局返回，
            // 因為空條件列表（Count == 0）的序列不依賴 GameStateManager 就應解鎖。

            foreach (var seqCfg in sequenceConfigs)
            {
                // 頁籤解鎖
                if (!seqCfg.isUnlocked)
                {
                    bool canUnlock;
                    if (seqCfg.requiredStatesToUnlock.Count == 0)
                    {
                        // 不需要任何條件 → 直接解鎖
                        canUnlock = true;
                    }
                    else if (GameStateManager.Instance == null)
                    {
                        // 有條件但 Manager 還沒初始化 → 暫時跳過（Start 會補做）
                        canUnlock = false;
                    }
                    else
                    {
                        canUnlock = GameStateManager.Instance.HasAllStates(seqCfg.requiredStatesToUnlock);
                    }

                    if (canUnlock)
                    {
                        seqCfg.isUnlocked = true;
                        SaveSequenceUnlock(seqCfg.sequenceIndex);

                        if (fireEvents)
                            seqCfg.onSequenceUnlocked?.Invoke();

                        Debug.Log($"[ClueBookPageUnlocker] 頁籤 {seqCfg.sequenceIndex} 已解鎖。");
                    }
                }

                // 頁面解鎖（只有頁籤已解鎖才評估內頁）
                if (!seqCfg.isUnlocked) continue;

                foreach (var pageCfg in seqCfg.pages)
                {
                    if (!pageCfg.isUnlocked)
                    {
                        bool canUnlock;
                        if (pageCfg.requiredStates.Count == 0)
                        {
                            canUnlock = true;
                        }
                        else if (GameStateManager.Instance == null)
                        {
                            canUnlock = false;
                        }
                        else
                        {
                            canUnlock = GameStateManager.Instance.HasAllStates(pageCfg.requiredStates);
                        }

                        if (canUnlock)
                        {
                            pageCfg.isUnlocked = true;
                            SavePageUnlock(seqCfg.sequenceIndex, pageCfg.pageIndex);

                            if (fireEvents)
                                pageCfg.onPageUnlocked?.Invoke();

                            Debug.Log($"[ClueBookPageUnlocker] 頁籤 {seqCfg.sequenceIndex} 第 {pageCfg.pageIndex} 頁已解鎖。");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 根據目前解鎖狀態，更新所有 triggerObject 的顯示/隱藏。
        /// </summary>
        private void ApplyAllUI()
        {
            if (switcher == null) return;

            for (int i = 0; i < switcher.SequenceCount; i++)
            {
                var cfg = FindSequenceCfg(i);
                bool unlocked = cfg == null || cfg.isUnlocked;
                switcher.SetSequenceInteractable(i, unlocked);
            }
        }

        // ──────────────────────────────────────────────
        // 翻頁尋找邏輯
        // ──────────────────────────────────────────────

        /// <summary>
        /// 從 current 開始，往 direction（+1 或 -1）方向找最近的已解鎖頁。
        /// 若兜一圈後找不到任何解鎖頁（理論上不應發生），回傳 current。
        /// </summary>
        private int FindNextUnlocked(int seqIdx, int current, int total, int direction)
        {
            for (int step = 1; step < total; step++)
            {
                int candidate = ((current + direction * step) % total + total) % total;
                if (IsPageUnlocked(seqIdx, candidate))
                    return candidate;
            }
            // 找不到其他解鎖頁，停留原地
            return current;
        }

        // ──────────────────────────────────────────────
        // PlayerPrefs 持久化
        // ──────────────────────────────────────────────

        private string SeqKey(int seqIdx)
            => $"{saveKeyPrefix}{KEY_SEQ}{seqIdx}";

        private string PageKey(int seqIdx, int pageIdx)
            => $"{saveKeyPrefix}{KEY_SEQ}{seqIdx}_{KEY_PAGE}{pageIdx}";

        private void SaveSequenceUnlock(int seqIdx)
        {
            PlayerPrefs.SetInt(SeqKey(seqIdx), 1);
            PlayerPrefs.Save();
        }

        private void SavePageUnlock(int seqIdx, int pageIdx)
        {
            PlayerPrefs.SetInt(PageKey(seqIdx, pageIdx), 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 從 PlayerPrefs 還原所有已解鎖狀態（Awake 時呼叫，不觸發 UnityEvent）。
        /// </summary>
        private void RestoreFromPrefs()
        {
            foreach (var seqCfg in sequenceConfigs)
            {
                if (PlayerPrefs.GetInt(SeqKey(seqCfg.sequenceIndex), 0) == 1)
                    seqCfg.isUnlocked = true;

                foreach (var pageCfg in seqCfg.pages)
                {
                    if (PlayerPrefs.GetInt(PageKey(seqCfg.sequenceIndex, pageCfg.pageIndex), 0) == 1)
                        pageCfg.isUnlocked = true;
                }
            }
        }

        // ──────────────────────────────────────────────
        // 工具方法
        // ──────────────────────────────────────────────

        private SequenceUnlockConfig FindSequenceCfg(int seqIndex)
            => sequenceConfigs.Find(c => c.sequenceIndex == seqIndex);

#if UNITY_EDITOR
        // ──────────────────────────────────────────────
        // Editor 工具（僅在 Editor 中可用）
        // ──────────────────────────────────────────────

        /// <summary>
        /// 清除所有 PlayerPrefs 解鎖存檔（Editor 測試用，Runtime 請勿呼叫）。
        /// </summary>
        [ContextMenu("🗑 清除所有解鎖存檔（Editor Only）")]
        public void ClearAllSaveData()
        {
            foreach (var seqCfg in sequenceConfigs)
            {
                PlayerPrefs.DeleteKey(SeqKey(seqCfg.sequenceIndex));
                foreach (var pageCfg in seqCfg.pages)
                    PlayerPrefs.DeleteKey(PageKey(seqCfg.sequenceIndex, pageCfg.pageIndex));
            }
            PlayerPrefs.Save();
            Debug.Log("[ClueBookPageUnlocker] 所有解鎖存檔已清除。");
        }

        /// <summary>
        /// 解鎖所有頁籤與頁面（Editor 測試用）。
        /// </summary>
        [ContextMenu("🔓 解鎖全部（Editor Only）")]
        public void UnlockAll()
        {
            foreach (var seqCfg in sequenceConfigs)
            {
                seqCfg.isUnlocked = true;
                SaveSequenceUnlock(seqCfg.sequenceIndex);
                foreach (var pageCfg in seqCfg.pages)
                {
                    pageCfg.isUnlocked = true;
                    SavePageUnlock(seqCfg.sequenceIndex, pageCfg.pageIndex);
                }
            }
            ApplyAllUI();
            Debug.Log("[ClueBookPageUnlocker] 所有頁籤與頁面已解鎖。");
        }
#endif
    }
}
