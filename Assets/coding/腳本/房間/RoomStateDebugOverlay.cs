using System.Collections.Generic;
using UnityEngine;

namespace X
{
    /// <summary>
    /// 掛在與 RoomUIManager 相同（或任意）GameObject 上。
    /// 在遊戲執行時以 OnGUI Overlay 顯示：
    ///   大場景
    ///     └ ConditionReceiver（母）
    ///           └ StateReporter（子，依 state 字串配對）
    ///
    /// ● 含停用物件一併掃描
    /// ● 不自動持續掃描，改由 Inspector「掃描」按鈕或 ContextMenu 手動觸發
    /// </summary>
    public class RoomStateDebugOverlay : MonoBehaviour
    {
        [Header("參照")]
        public RoomUIManager roomUIManager;

        [Header("顯示控制")]
        public bool showOverlay = true;
        public int  fontSize    = 16;
        [Tooltip("Overlay 起始位置（右上角為基準，往左/往下偏移）")]
        public Vector2 position = new Vector2(10f, 10f);
        [Tooltip("Overlay 最大寬度（像素）")]
        public float maxWidth = 440f;

        // ── 內部快取（掃描後保留，不自動刷新）────────────────────────────────
        [HideInInspector] public List<BigSceneGroup> Groups       = new List<BigSceneGroup>();
        [HideInInspector] public List<ReceiverEntry> Unclassified = new List<ReceiverEntry>();
        [HideInInspector] public bool HasScanned = false;

        // ── GUI 樣式 ──────────────────────────────────────────────────────────
        private GUIStyle _styleHeader;
        private GUIStyle _styleReceiver;
        private GUIStyle _styleReporter;
        private GUIStyle _styleBg;
        private bool     _stylesInit;

        // ══════════════════════════════════════════════════════════════════════
        // 公開資料結構（讓 Editor 腳本也能讀）
        // ══════════════════════════════════════════════════════════════════════

        [System.Serializable]
        public class ReceiverEntry
        {
            public ConditionReceiver     receiver;
            public List<StateReporter>   matchedReporters = new List<StateReporter>();
        }

        [System.Serializable]
        public class BigSceneGroup
        {
            public string              sceneName;
            public List<ReceiverEntry> entries = new List<ReceiverEntry>();
        }

        // ══════════════════════════════════════════════════════════════════════
        // 掃描（含停用物件，一次性，手動呼叫）
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 手動掃描所有 ConditionReceiver / StateReporter（含停用物件）。
        /// 由 Inspector 按鈕或 ContextMenu 呼叫。
        /// </summary>
        [ContextMenu("掃描 State Reporter / Receiver")]
        public void Scan()
        {
            Groups       = new List<BigSceneGroup>();
            Unclassified = new List<ReceiverEntry>();

            // 1. 收集所有 StateReporter（含停用）
            var allReporters = new List<StateReporter>(
                FindObjectsByType<StateReporter>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            // 2. 收集所有 ConditionReceiver（含停用），建立配對
            var allReceivers = FindObjectsByType<ConditionReceiver>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            var entries = new List<ReceiverEntry>(allReceivers.Length);
            foreach (var rec in allReceivers)
            {
                if (rec == null) continue;
                var entry = new ReceiverEntry { receiver = rec };

                if (rec.requiredStates != null && rec.requiredStates.Count > 0)
                {
                    var stateSet = new HashSet<string>(rec.requiredStates);
                    foreach (var rep in allReporters)
                    {
                        if (rep != null &&
                            !string.IsNullOrEmpty(rep.stateToReport) &&
                            stateSet.Contains(rep.stateToReport))
                        {
                            entry.matchedReporters.Add(rep);
                        }
                    }
                }
                entries.Add(entry);
            }

            // 3. 按 BigScene 分組
            if (roomUIManager != null && roomUIManager.bigScenes != null)
            {
                foreach (var bs in roomUIManager.bigScenes)
                {
                    if (bs == null) continue;
                    var group = new BigSceneGroup
                    {
                        sceneName = string.IsNullOrEmpty(bs.sceneName)
                            ? $"大場景 id={bs.id}"
                            : bs.sceneName
                    };

                    foreach (var entry in entries)
                    {
                        if (IsUnderBigScene(entry.receiver.transform, bs))
                            group.entries.Add(entry);
                    }

                    if (group.entries.Count > 0)
                        Groups.Add(group);
                }
            }

            // 4. 不屬於任何大場景 → 不分類
            var classified = new HashSet<ConditionReceiver>();
            foreach (var g in Groups)
                foreach (var e in g.entries)
                    classified.Add(e.receiver);

            foreach (var entry in entries)
                if (!classified.Contains(entry.receiver))
                    Unclassified.Add(entry);

            HasScanned = true;
            Debug.Log($"[RoomStateDebugOverlay] 掃描完成：{Groups.Count} 個大場景，{Unclassified.Count} 個不分類 Receiver。");
        }

        private bool IsUnderBigScene(Transform target, BigScene bs)
        {
            if (bs.roomPanels == null) return false;
            foreach (var rt in bs.roomPanels)
            {
                if (rt == null) continue;
                if (target == rt.transform || target.IsChildOf(rt.transform))
                    return true;
            }
            return false;
        }

        // ══════════════════════════════════════════════════════════════════════
        // OnGUI Overlay（執行時顯示，資料來自上次掃描結果）
        // ══════════════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            if (!showOverlay) return;
            InitStyles();

            if (!HasScanned)
            {
                // 尚未掃描，提示文字
                var hint = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = fontSize,
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = Color.yellow }
                };
                float sw = Screen.width;
                GUI.Label(new Rect(sw - maxWidth - position.x, position.y, maxWidth, 30), "RoomStateDebug：請先按下「掃描」", hint);
                return;
            }

            var lines = BuildLines();
            if (lines.Count == 0) return;

            float lineH  = fontSize + 6f;
            float totalH = lines.Count * lineH + 8f;
            float x      = Screen.width - maxWidth - position.x;
            float y      = position.y;

            GUI.Box(new Rect(x - 4, y - 4, maxWidth + 8, totalH), GUIContent.none, _styleBg);

            float cy = y;
            foreach (var (text, style) in lines)
            {
                GUI.Label(new Rect(x, cy, maxWidth, lineH + 2), text, style);
                cy += lineH;
            }
        }

        private List<(string text, GUIStyle style)> BuildLines()
        {
            var lines = new List<(string, GUIStyle)>();
            lines.Add(("── State Reporter / Receiver ──", _styleHeader));

            void AddGroup(string label, List<ReceiverEntry> entries)
            {
                lines.Add(($"📁 {label}", _styleHeader));

                if (entries == null || entries.Count == 0)
                {
                    lines.Add(("  （無 Receiver）", _styleReceiver));
                    return;
                }

                foreach (var entry in entries)
                {
                    var rec = entry.receiver;
                    string recLabel = rec != null ? rec.gameObject.name : "（null）";
                    bool   recActive = rec != null && rec.gameObject.activeInHierarchy;

                    string statesSummary = "";
                    if (rec != null && rec.requiredStates != null && rec.requiredStates.Count > 0)
                        statesSummary = $"  [{string.Join(", ", rec.requiredStates)}]";

                    // 停用物件加 ⊘ 標記
                    string activeTag = recActive ? "" : " ⊘";
                    lines.Add(($"  📦 {recLabel}{activeTag}{statesSummary}", _styleReceiver));

                    if (entry.matchedReporters == null || entry.matchedReporters.Count == 0)
                    {
                        lines.Add(("      └ （無配對 Reporter）", _styleReporter));
                    }
                    else
                    {
                        int cnt = entry.matchedReporters.Count;
                        for (int i = 0; i < cnt; i++)
                        {
                            var rep = entry.matchedReporters[i];
                            if (rep == null) continue;
                            bool repActive = rep.gameObject.activeInHierarchy;
                            string prefix    = (i == cnt - 1) ? "└" : "├";
                            string repActive2 = repActive ? "" : " ⊘";
                            lines.Add(($"      {prefix} 📢 {rep.gameObject.name}{repActive2}  →  \"{rep.stateToReport}\"",
                                _styleReporter));
                        }
                    }
                }
            }

            foreach (var g in Groups)
                AddGroup(g.sceneName, g.entries);

            if (Unclassified != null && Unclassified.Count > 0)
                AddGroup("【不分類】", Unclassified);

            return lines;
        }

        private void InitStyles()
        {
            if (_stylesInit) return;
            _stylesInit = true;

            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.60f));
            bgTex.Apply();
            _styleBg = new GUIStyle { normal = { background = bgTex }, padding = new RectOffset(6, 6, 4, 4) };

            _styleHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize  = fontSize, fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(1f, 0.85f, 0.3f) },
                padding   = new RectOffset(4, 4, 2, 2)
            };
            _styleReceiver = new GUIStyle(GUI.skin.label)
            {
                fontSize  = fontSize, fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.5f, 0.9f, 1f) },
                padding   = new RectOffset(4, 4, 1, 1)
            };
            _styleReporter = new GUIStyle(GUI.skin.label)
            {
                fontSize  = fontSize, fontStyle = FontStyle.Normal,
                normal    = { textColor = new Color(0.75f, 1f, 0.75f) },
                padding   = new RectOffset(4, 4, 1, 1)
            };
        }
    }
}
