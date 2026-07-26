#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using X;

[CustomEditor(typeof(ObjectSequenceSwitcher))]
public class ObjectSequenceSwitcherEditor : Editor
{
    // 折疊狀態（用序列索引做 key）
    private bool[] foldouts = new bool[0];

    private SerializedProperty sequencesProp;
    private SerializedProperty startIndexProp;

    private void OnEnable()
    {
        sequencesProp  = serializedObject.FindProperty("sequences");
        startIndexProp = serializedObject.FindProperty("startSequenceIndex");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── 起始序列 ──────────────────────────────
        EditorGUILayout.PropertyField(startIndexProp,
            new GUIContent("起始序列索引", "遊戲開始時自動啟動的序列（0-based）"));

        EditorGUILayout.Space(6);

        // ── 序列列表標題 ──────────────────────────
        EditorGUILayout.LabelField("序列列表", EditorStyles.boldLabel);

        // 同步折疊陣列大小
        if (foldouts.Length != sequencesProp.arraySize)
        {
            bool[] newFolds = new bool[sequencesProp.arraySize];
            for (int i = 0; i < Mathf.Min(foldouts.Length, newFolds.Length); i++)
                newFolds[i] = foldouts[i];
            // 新加入的預設展開
            for (int i = foldouts.Length; i < newFolds.Length; i++)
                newFolds[i] = true;
            foldouts = newFolds;
        }

        for (int i = 0; i < sequencesProp.arraySize; i++)
        {
            SerializedProperty seqProp = sequencesProp.GetArrayElementAtIndex(i);

            SerializedProperty nameProp    = seqProp.FindPropertyRelative("sequenceName");
            SerializedProperty triggerProp = seqProp.FindPropertyRelative("triggerObject");
            SerializedProperty objectsProp = seqProp.FindPropertyRelative("objects");

            // 每個序列用帶顏色背景的 Box 包裹
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // ── 序列標題列 ────────────────────────
            EditorGUILayout.BeginHorizontal();

            foldouts[i] = EditorGUILayout.Foldout(foldouts[i],
                $"[{i}]  {nameProp.stringValue}", true, EditorStyles.foldoutHeader);

            GUILayout.FlexibleSpace();

            // 刪除按鈕
            if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(18)))
            {
                sequencesProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                return; // 刪除後立即返回避免越界
            }

            EditorGUILayout.EndHorizontal();

            // ── 展開內容 ──────────────────────────
            if (foldouts[i])
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(nameProp,
                    new GUIContent("序列名稱"));

                EditorGUILayout.PropertyField(triggerProp,
                    new GUIContent("觸發物件",
                        "點擊此物件切換到本序列；留空則只能透過 API 呼叫 ActivateSequence(" + i + ")"));

                EditorGUILayout.PropertyField(objectsProp,
                    new GUIContent("序列物件", "本序列中依序切換的物件清單"), true);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        // ── 新增序列按鈕 ─────────────────────────
        EditorGUILayout.Space(4);
        if (GUILayout.Button("＋  新增序列", GUILayout.Height(28)))
        {
            sequencesProp.InsertArrayElementAtIndex(sequencesProp.arraySize);
            SerializedProperty newSeq = sequencesProp.GetArrayElementAtIndex(sequencesProp.arraySize - 1);
            newSeq.FindPropertyRelative("sequenceName").stringValue = "Sequence " + sequencesProp.arraySize;
            newSeq.FindPropertyRelative("triggerObject").objectReferenceValue = null;
            newSeq.FindPropertyRelative("objects").ClearArray();
        }

        EditorGUILayout.Space(8);

        // ── 執行期狀態顯示（Play Mode 時）────────
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("── 執行期狀態 ──", EditorStyles.centeredGreyMiniLabel);
            var comp = (ObjectSequenceSwitcher)target;
            EditorGUILayout.LabelField("當前序列索引", comp.ActiveSequenceIndex.ToString());
            EditorGUILayout.LabelField("當前物件索引", comp.CurrentObjectIndex.ToString());
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
