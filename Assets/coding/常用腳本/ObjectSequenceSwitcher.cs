using UnityEngine;
using System.Collections.Generic;

namespace X
{
    public class ObjectSequenceSwitcher : MonoBehaviour
    {
        [Header("設定序列物件")]
        [SerializeField] private List<GameObject> targetObjects; // 在編輯器中放入所有要切換的物件
        private int currentIndex = 0; // 當前顯示物件的索引

        private void Start()
        {
            UpdateVisibility(); // 初始化：確保只有第一個物件顯示
        }

        // 新增此方法至 ObjectSequenceSwitcher 類別中
        public void ResetToFirst()
        {
            // 將索引設回 0
            currentIndex = 0;
    
            // 更新顯示狀態
            UpdateVisibility();
    
            // 選用：若有開發日誌，可在此處 Debug
            // Debug.Log("已重置到第一頁");
        }
        // 向右切換
        public void SwitchNext()
        {
            currentIndex++;
            if (currentIndex >= targetObjects.Count) currentIndex = 0; // 循環回到第一個
            UpdateVisibility();
        }

        // 向左切換
        public void SwitchPrevious()
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = targetObjects.Count - 1; // 循環回到最後一個
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            for (int i = 0; i < targetObjects.Count; i++)
            {
                // 若 i 等於當前索引則顯示，否則隱藏
                targetObjects[i].SetActive(i == currentIndex);
            }
        }
    }
}