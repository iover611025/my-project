using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic; // 引入集合命名空間以支援 List

namespace X
{
    /// <summary>
    /// 握持指定道具互動後：關閉多個指定物件 + 開啟多個指定 UI 面板
    /// </summary>
    public class HeldItemTrigger : MonoBehaviour, IPointerClickHandler
    {
        [Header("互動設定")]
        [Tooltip("需要握持的道具 ID")]
        public int requiredItemId;      
        [Tooltip("是否在互動成功後消耗（清空）該握持道具")]
        public bool consumeItem = true; 

        [Header("目標對象（多選）")]
        [Tooltip("成功後要開啟的多個 UI Panel")]
        public List<GameObject> panelsToOpen = new List<GameObject>();  
        
        [Tooltip("成功後要關閉的多個物件 (若列表為空，則預設關閉自身)")]
        public List<GameObject> objectsToDisable = new List<GameObject>(); 

        private InventoryUI inventoryUI;

        void Start()
        {
            // Unity 6 推薦高效 API：尋找場景中的背包系統 UI
            inventoryUI = Object.FindFirstObjectByType<InventoryUI>();

            // 如果玩家沒有指派任何要關閉的物件，自動將自身加入列表，確保基本邏輯運作
            if (objectsToDisable == null || objectsToDisable.Count == 0)
            {
                objectsToDisable = new List<GameObject> { this.gameObject };
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (inventoryUI == null) return;

            // 1. 取得目前玩家握持的道具資料
            ItemData heldItem = inventoryUI.GetHeldItemData();

            // 2. 判斷 ID 是否符合機關需求
            if (heldItem != null && heldItem.id == requiredItemId)
            {
                ExecuteTrigger();
            }
            else
            {
                // 當不匹配時，給出友好的 Debug 提示
                int currentHeldId = (heldItem != null) ? heldItem.id : 0;
                Debug.Log($"[Trigger] 道具不匹配！需要 ID:{requiredItemId}，目前是 ID:{currentHeldId}");
            }
        }

        /// <summary>
        /// 執行觸發後的連鎖反應
        /// </summary>
        private void ExecuteTrigger()
        {
            // 1. 消耗道具
            if (consumeItem)
            {
                inventoryUI.ClearHeldItem();
            }

            // 2. 批次開啟目標面板，並同步 RoomManager
            if (panelsToOpen != null && panelsToOpen.Count > 0)
            {
                RoomUIManager uiManager = Object.FindFirstObjectByType<RoomUIManager>();

                foreach (GameObject panel in panelsToOpen)
                {
                    if (panel != null)
                    {
                        panel.SetActive(true);

                        // 如果場景中有 RoomUIManager，同步世界物件狀態
                        if (uiManager != null)
                        {
                            uiManager.SyncByPanel(panel);
                        }
                        Debug.Log($"[Trigger] 互動成功：已開啟面板 {panel.name}");
                    }
                }
            }

            // 3. 批次關閉指定物件（如迷霧、鎖、拉桿等）
            if (objectsToDisable != null && objectsToDisable.Count > 0)
            {
                foreach (GameObject obj in objectsToDisable)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                        Debug.Log($"[Trigger] 互動成功：已關閉物件 {obj.name}");
                    }
                }
            }
        }
    }
}