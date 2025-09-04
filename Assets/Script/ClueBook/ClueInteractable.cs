using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace X
{
    public class ClueInteractable : MonoBehaviour, IPointerClickHandler
    {
        [Header("要顯示的故事Panel（Prefab或場景物件）")]
        public GameObject storyPanelPrefabOrObj;

        [Header("要顯示的文字內容（依序對應 StoryPanelUI 的文字元件）")]
        public List<string> storyTexts;

        public void OnPointerClick(PointerEventData eventData)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null || storyPanelPrefabOrObj == null) return;

            // 若是Prefab則Instantiate，若是場景物件則直接SetActive
            GameObject panelObj;
            bool isPrefab = storyPanelPrefabOrObj.scene.rootCount == 0;
            if (isPrefab)
                panelObj = Instantiate(storyPanelPrefabOrObj, canvas.transform);
            else
                panelObj = storyPanelPrefabOrObj;

            var panel = panelObj.GetComponent<StoryPanelUI>();
            if (panel != null)
            {
                panel.SetTexts(storyTexts);
                panelObj.SetActive(true);
            }
        }
    }
}