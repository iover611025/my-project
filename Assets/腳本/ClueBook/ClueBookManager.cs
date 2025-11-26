using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace X
{
    [System.Serializable]
    public class ClueSection
    {
        public string sectionName;
        public RectTransform panel; // Panel物件
        public List<GameObject> hiddenObjects; // 需解鎖才顯示的物件
        [TextArea(2, 5)]
        public string content; // 可選：用於動態填充
    }

    public class ClueBookManager : MonoBehaviour
    {
        public GameObject clueBookPanel;
        public Button openBookButton;
        public List<Button> bookmarkButtons; // 書籤按鈕
        public List<ClueSection> sections = new List<ClueSection>();

        private int currentSectionIndex = 0;

        void Awake()
        {
            if (clueBookPanel != null)
                clueBookPanel.SetActive(false);

            if (openBookButton != null)
                openBookButton.onClick.AddListener(ToggleClueBook);

            for (int i = 0; i < bookmarkButtons.Count; i++)
            {
                int idx = i;
                bookmarkButtons[i].onClick.AddListener(() => SwitchSection(idx));
            }

            HideAllSections();
        }

        public void ToggleClueBook()
        {
            if (clueBookPanel == null) return;
            clueBookPanel.SetActive(!clueBookPanel.activeSelf);
            if (clueBookPanel.activeSelf)
                ShowSection(currentSectionIndex);
        }

        public void SwitchSection(int index)
        {
            currentSectionIndex = index;
            ShowSection(index);
        }

        void ShowSection(int index)
        {
            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i].panel != null)
                    sections[i].panel.gameObject.SetActive(i == index);
            }
        }

        void HideAllSections()
        {
            foreach (var section in sections)
                if (section.panel != null)
                    section.panel.gameObject.SetActive(false);
        }

        // 解鎖指定分區的內容
        public void UnlockSection(string sectionName, string content)
        {
            var section = sections.Find(s => s.sectionName == sectionName);
            if (section != null)
            {
                section.content = content;
                // 顯示所有需解鎖的物件
                foreach (var obj in section.hiddenObjects)
                    if (obj != null) obj.SetActive(true);
                // 可選：自動切換到該分區
                ShowSection(sections.IndexOf(section));
            }
        }
    }
}