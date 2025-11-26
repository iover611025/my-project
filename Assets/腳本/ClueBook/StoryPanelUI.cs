using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace X
{
    public class StoryPanelUI : MonoBehaviour
    {
        [Header("可自訂文字元件（Text 或 TMP）")]
        public List<Text> uiTexts;
        public List<TMP_Text> tmpTexts;

        [Header("可自訂互動按鈕")]
        public List<Button> customButtons;

        [Header("關閉按鈕")]
        public Button closeButton;

        // 設定文字內容
        public void SetTexts(List<string> contents)
        {
            for (int i = 0; i < uiTexts.Count; i++)
                if (i < contents.Count && uiTexts[i] != null)
                    uiTexts[i].text = contents[i];

            for (int i = 0; i < tmpTexts.Count; i++)
                if (i < contents.Count && tmpTexts[i] != null)
                    tmpTexts[i].text = contents[i];
        }

        void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            }
        }
    }
}