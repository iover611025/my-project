using System;
using System.Collections.Generic;
using UnityEngine;

namespace X
{
    /// <summary>
    /// 線索本門面腳本。
    /// 對外提供 SwitchNext / SwitchPrevious，
    /// 內部委派給 ClueBookPageUnlocker 執行「跳過未解鎖頁」的翻頁邏輯。
    /// 將此腳本的 SwitchNext / SwitchPrevious 綁定到 UI 翻頁按鈕的 onClick。
    /// 同時支援依 RoomUIManager 目前所在的大場景，在開啟線索本時自動跳轉至指定頁籤與頁面。
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class ClueBook : MonoBehaviour
    {
        [Serializable]
        public class BigScenePageMapping
        {
            [Tooltip("大場景 ID（對應 RoomUIManager.bigScenes 中的 id）")]
            public int bigSceneId;

            [Tooltip("選填：若填寫大場景名稱，則改以名稱比對（對應 BigScene.sceneName，忽略大小寫）；留空則使用 bigSceneId")]
            public string sceneName;

            [Tooltip("開啟時要切換到的頁籤索引（對應 ObjectSequenceSwitcher.sequences 的 0-based 索引）")]
            public int sequenceIndex;

            [Tooltip("開啟時要切換到的頁面索引（對應 Sequence.objects 的 0-based 索引）")]
            public int pageIndex;
        }

        [Header("相依元件")]
        [SerializeField] private ObjectSequenceSwitcher switcher;
        [SerializeField] private ClueBookPageUnlocker unlocker;

        [Header("依大場景指定開啟頁面")]
        [Tooltip("是否在線索本啟用時，自動依目前大場景跳轉至指定頁面")]
        [SerializeField] private bool jumpToBigScenePageOnEnable = true;

        [Tooltip("若目標頁面尚未解鎖，是否自動改為該頁籤內第一個已解鎖的頁面（需搭配 ClueBookPageUnlocker）")]
        [SerializeField] private bool respectPageUnlocks = true;

        [Tooltip("各大場景對應的線索本起始頁籤與頁面")]
        [SerializeField] private List<BigScenePageMapping> bigScenePages = new List<BigScenePageMapping>();

        private bool _hasStarted = false;

        private void Reset()
        {
            // 在 Inspector 重置時自動嘗試抓取同 GameObject 上的元件
            switcher = GetComponent<ObjectSequenceSwitcher>();
            unlocker = GetComponent<ClueBookPageUnlocker>();
        }

        private void Awake()
        {
            if (switcher == null)
                switcher = GetComponent<ObjectSequenceSwitcher>();
            if (unlocker == null)
                unlocker = GetComponent<ClueBookPageUnlocker>();
        }

        private void OnEnable()
        {
            if (_hasStarted && jumpToBigScenePageOnEnable)
                ApplyBigSceneStartPage();
        }

        private void Start()
        {
            _hasStarted = true;
            if (jumpToBigScenePageOnEnable)
                ApplyBigSceneStartPage();
        }

        // ──────────────────────────────────────────────
        // 大場景指定頁面 API
        // ──────────────────────────────────────────────

        /// <summary>
        /// 依據 RoomUIManager 目前的大場景，將線索本切換至對應的頁籤與頁面。
        /// 可在 OnEnable 自動呼叫，也可綁定至開啟按鈕的 onClick。
        /// </summary>
        public void ApplyBigSceneStartPage()
        {
            if (switcher == null || RoomUIManager.Instance == null) return;
            if (bigScenePages == null || bigScenePages.Count == 0) return;

            BigScene currentScene = RoomUIManager.Instance.CurrentBigScene;
            int currentId = RoomUIManager.Instance.CurrentBigSceneId;
            string currentName = currentScene != null ? currentScene.sceneName : null;

            BigScenePageMapping mapping = FindMappingForScene(currentId, currentName);
            if (mapping == null) return;

            int seqIdx = mapping.sequenceIndex;
            if (seqIdx < 0 || seqIdx >= switcher.SequenceCount) return;

            int totalPages = switcher.GetObjectsCount(seqIdx);
            if (totalPages <= 0) return;

            int targetPage = Mathf.Clamp(mapping.pageIndex, 0, totalPages - 1);

            if (respectPageUnlocks && unlocker != null)
            {
                if (!unlocker.IsSequenceUnlocked(seqIdx))
                    return;

                if (!unlocker.IsPageUnlocked(seqIdx, targetPage))
                {
                    int firstUnlocked = FindFirstUnlockedPage(seqIdx, totalPages);
                    if (firstUnlocked < 0)
                        return;
                    targetPage = firstUnlocked;
                }
            }

            // 先設定該頁籤的目標頁索引，再切換頁籤以確保顯示正確頁面
            switcher.JumpToPage(seqIdx, targetPage);
            switcher.ActivateSequence(seqIdx);
        }

        private BigScenePageMapping FindMappingForScene(int currentId, string currentName)
        {
            foreach (var entry in bigScenePages)
            {
                if (entry == null) continue;

                if (!string.IsNullOrWhiteSpace(entry.sceneName))
                {
                    if (!string.IsNullOrEmpty(currentName) &&
                        string.Equals(entry.sceneName.Trim(), currentName.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return entry;
                    }
                }
                else if (entry.bigSceneId == currentId)
                {
                    return entry;
                }
            }
            return null;
        }

        private int FindFirstUnlockedPage(int seqIdx, int totalPages)
        {
            for (int i = 0; i < totalPages; i++)
            {
                if (unlocker.IsPageUnlocked(seqIdx, i))
                    return i;
            }
            return -1;
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
