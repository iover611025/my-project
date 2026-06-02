using UnityEngine;
using UnityEngine.UI; // 引入 Unity 內建 UI 命名空間

namespace X
{
    /// <summary>
    /// 功能：當此物件被開啟 (Active) 時，自動修改指定目標物件的 Image 照片。
    /// 適用場景：2D解謎遊戲中，點擊某線索開啟大圖時，同步更新旁邊的線索筆記照片。
    /// </summary>
    public class ChangeImageOnEnable : MonoBehaviour
    {
        [Header("目標 UI 設定")]
        [SerializeField] private Image targetImage;   // 想要被修改照片的目標 Image 組件
        [SerializeField] private Sprite newSprite;    // 準備替換上去的新照片

        [Header("進階設定")]
        [SerializeField] private bool restoreOnDisable = false; // 當此物件關閉時，是否還原舊照片？
        
        private Sprite _originalSprite; // 用於記錄原本的照片

        // Unity 內建生命週期：當物件被啟用 (SetActive(true)) 時自動執行
        private void OnEnable()
        {
            if (targetImage == null)
            {
                Debug.LogWarning($"[{name}] 未指派 targetImage，無法更換照片。");
                return;
            }

            if (newSprite == null)
            {
                Debug.LogWarning($"[{name}] 未指派 newSprite，將會把目標照片清空。");
            }

            // 如果開啟了還原功能，先記錄原本的照片是什麼
            if (restoreOnDisable)
            {
                _originalSprite = targetImage.sprite;
            }

            // 核心邏輯：直接替換內建 Image 的 sprite 屬性
            targetImage.sprite = newSprite;
            
            // 效能小提示：確保 Image 的 Raycast Target 根據需求調整，避免引發額外的 UI 射線消耗
            Debug.Log($"[解謎事件] 物件 {gameObject.name} 已開啟，已將 {targetImage.name} 的照片更換為 {newSprite?.name}");
        }

        // Unity 內建生命週期：當物件被關閉 (SetActive(false)) 時自動執行
        private void OnDisable()
        {
            // 如果有勾選還原功能，且目標還存在，就還原照片
            if (restoreOnDisable && targetImage != null)
            {
                targetImage.sprite = _originalSprite;
                Debug.Log($"[解謎事件] 物件 {gameObject.name} 已關閉，已將 {targetImage.name} 的照片還原。");
            }
        }
    }
}