#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace X
{
    [CustomEditor(typeof(RoomStateDebugOverlay))]
    public class RoomStateDebugOverlayEditor : Editor
    {
        // 折疊狀態（per-group）
        private Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();

        private static readonly Color ColorHeader    = new Color(1f, 0.85f, 0.3f);
        private static readonly Color ColorReceiver  = new Color(0.4f, 0.85f, 1f);
        private static readonly Color ColorReporter  = new Color(0.6f, 1f, 0.6f);
        private static readonly Color ColorInactive  = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color ColorNoMatch   = new Color(1f, 0.5f, 0.5f);

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var overlay = (RoomStateDebugOverlay)target;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("── Debug 工具 ──", EditorStyles.boldLabel);

            // ── 掃描按鈕 ──────────────────────────────────────────────
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
            if (GUILayout.Button("🔍  掃描 State Reporter / Receiver（含停用物件）", GUILayout.Height(32)))
            {
                overlay.Scan();
                _foldouts.Clear();
                EditorUtility.SetDirty(target);
            }
            GUI.backgroundColor = Color.white;

            if (!overlay.HasScanned)
            {
                EditorGUILayout.HelpBox("尚未掃描，請按上方按鈕。", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4);

            // ── 各大場景 ──────────────────────────────────────────────
            foreach (var group in overlay.Groups)
                DrawGroup(group.sceneName, group.entries, isUnclassified: false);

            // ── 不分類 ────────────────────────────────────────────────
            if (overlay.Unclassified != null && overlay.Unclassified.Count > 0)
                DrawGroup("【不分類】", overlay.Unclassified, isUnclassified: true);
        }

        private void DrawGroup(string label, List<RoomStateDebugOverlay.ReceiverEntry> entries, bool isUnclassified)
        {
            if (!_foldouts.ContainsKey(label)) _foldouts[label] = true;

            // 大場景標題
            var origColor = GUI.contentColor;
            GUI.contentColor = isUnclassified ? ColorInactive : ColorHeader;
            _foldouts[label] = EditorGUILayout.Foldout(_foldouts[label], $"📁  {label}  ({entries.Count} Receiver)", true, EditorStyles.foldoutHeader);
            GUI.contentColor = origColor;

            if (!_foldouts[label]) return;

            EditorGUI.indentLevel++;

            if (entries == null || entries.Count == 0)
            {
                EditorGUILayout.LabelField("（無 Receiver）", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                return;
            }

            foreach (var entry in entries)
            {
                var rec = entry.receiver;
                if (rec == null) continue;

                bool recActive = rec.gameObject.activeInHierarchy;
                string recName = rec.gameObject.name + (recActive ? "" : "  ⊘ 停用");

                // Receiver 行 ── 點擊可選取物件
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.contentColor = recActive ? ColorReceiver : ColorInactive;
                    EditorGUILayout.LabelField($"📦  {recName}", EditorStyles.boldLabel);
                    GUI.contentColor = origColor;

                    if (GUILayout.Button("選取", GUILayout.Width(42), GUILayout.Height(16)))
                        Selection.activeGameObject = rec.gameObject;
                }

                // requiredStates 摘要
                if (rec.requiredStates != null && rec.requiredStates.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"needs: [{string.Join(", ", rec.requiredStates)}]",
                        EditorStyles.miniLabel);
                    EditorGUI.indentLevel--;
                }

                // Reporter 子項
                EditorGUI.indentLevel++;
                if (entry.matchedReporters == null || entry.matchedReporters.Count == 0)
                {
                    GUI.contentColor = ColorNoMatch;
                    EditorGUILayout.LabelField("└  （無配對 Reporter）", EditorStyles.miniLabel);
                    GUI.contentColor = origColor;
                }
                else
                {
                    int cnt = entry.matchedReporters.Count;
                    for (int i = 0; i < cnt; i++)
                    {
                        var rep = entry.matchedReporters[i];
                        if (rep == null) continue;
                        bool repActive = rep.gameObject.activeInHierarchy;
                        string prefix   = (i == cnt - 1) ? "└" : "├";
                        string repName  = rep.gameObject.name + (repActive ? "" : "  ⊘");

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUI.contentColor = repActive ? ColorReporter : ColorInactive;
                            EditorGUILayout.LabelField($"{prefix}  📢  {repName}  →  \"{rep.stateToReport}\"",
                                EditorStyles.miniLabel);
                            GUI.contentColor = origColor;

                            if (GUILayout.Button("選取", GUILayout.Width(42), GUILayout.Height(16)))
                                Selection.activeGameObject = rep.gameObject;
                        }
                    }
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(2);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }
    }
}
#endif
