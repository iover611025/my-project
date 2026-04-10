using UnityEngine;
using UnityEngine.EventSystems;

namespace X
{
    /// <summary>
    /// 握持指定道具互動後：關閉自身物件 + 開啟指定 UI 面板
    /// </summary>
    public class HeldItemTrigger : MonoBehaviour, IPointerClickHandler
    {
        [Header("互動設定")]
        public int requiredItemId;      // 需要握持的道具 ID
        public bool consumeItem = true; // 是否互動後消耗該道具

        [Header("目標對象")]
        public GameObject panelToOpen;  // 成功後要開啟的 UI Panel
        public GameObject objectToDisable; // 成功後要關閉的物件 (若留空則關閉自身)

        private InventoryUI inventoryUI;

        void Start()
        {
            // Unity 6 建議使用 FindFirstObjectByType
            inventoryUI = Object.FindFirstObjectByType<InventoryUI>();

            if (objectToDisable == null)
                objectToDisable = this.gameObject;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (inventoryUI == null) return;

            // 1. 取得目前玩家握持的道具資料
            ItemData heldItem = inventoryUI.GetHeldItemData();

            // 2. 判斷 ID 是否符合
            if (heldItem != null && heldItem.id == requiredItemId)
            {
                ExecuteTrigger();
            }
            else
            {
                Debug.Log($"[Trigger] 道具不匹配！需要 ID:{requiredItemId}，目前是 ID:{(heldItem != null ? heldItem.id : 0)}");
            }
        }

        private void ExecuteTrigger()
        {
            // 消耗道具
            if (consumeItem)
            {
                inventoryUI.ClearHeldItem();
            }

            // 開啟目標面板
            if (panelToOpen != null)
            {
                panelToOpen.SetActive(true);

                // 同步 RoomManager (延續你之前的邏輯，確保世界物件同步)
                RoomUIManager uiManager = Object.FindFirstObjectByType<RoomUIManager>();
                if (uiManager != null)
                {
                    uiManager.SyncByPanel(panelToOpen);
                }
            }

            // 關閉自身或指定物件
            if (objectToDisable != null)
            {
                objectToDisable.SetActive(false);
            }

            Debug.Log($"[Trigger] 互動成功：已開啟 {panelToOpen.name}");
        }
    }
}