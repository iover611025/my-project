using UnityEngine;
using UnityEditor;
using TMPro;

public class ReplaceAllFonts : EditorWindow
{
    public TMP_FontAsset newFont;

    [MenuItem("Tools/批量替換字體 (Replace Fonts)")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceAllFonts>("批量替換字體");
    }

    void OnGUI()
    {
        GUILayout.Label("請選擇要統一替換的 SDF 字體", EditorStyles.boldLabel);
        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("目標字體", newFont, typeof(TMP_FontAsset), false);

        GUILayout.Space(10);

        if (GUILayout.Button("替換【目前場景】中的所有字體"))
        {
            if (newFont == null)
            {
                Debug.LogWarning("請先選擇字體！");
                return;
            }

            // 尋找場景中所有的 TMP_Text (包含隱藏未激活的 UI)
            TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
            int count = 0;

            foreach (TMP_Text text in allTexts)
            {
                Undo.RecordObject(text, "Replace Font"); // 讓操作可以復原 (Ctrl+Z)
                text.font = newFont;
                EditorUtility.SetDirty(text);
                count++;
            }
            
            Debug.Log($"✅ 成功將場景中 {count} 個文字組件替換為 {newFont.name}！");
        }
    }
}