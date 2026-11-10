using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace X
{
    /// <summary>
    /// 指定道具互動觸發器：持有指定道具 + 開啟指定 UI 目標。
    /// </summary>
    public class HeldItemTrigger : MonoBehaviour, IPointerClickHandler
    {
        [Header("道具設定")]
        [Tooltip("需要持有的 ID（可多個，任一符合即觸發）")]
        public List<int> requiredItemIds = new List<int>();
        [Tooltip("是否在觸發後消耗（移除）道具")]
        public bool consumeItem = true;

        [Header("目標物件（複數）")]
        [Tooltip("觸發後要開啟的複數 UI Panel")]
        public List<GameObject> panelsToOpen = new List<GameObject>();

        [Tooltip("觸發後要關閉的物件（若空白，預設關閉自己）")]
        public List<GameObject> objectsToDisable = new List<GameObject>();

        [Header("縮放替換 (選項)")]
        [Tooltip("如果在縮放狀態下觸發，此物件將取代舊 Panel，一併加入 Return 鏈。")]
        public GameObject newZoomPanelToReplace;

        [Header("音效")]
        [Tooltip("道具符合、觸發成功時播放的音效。")]
        [SerializeField] private AudioClip successClip;
        [Tooltip("道具不符合時播放的音效（選填）。")]
        [SerializeField] private AudioClip failClip;
        [Tooltip("音量（0 ~ 1）")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [Header("觸發事件（選填）")]
        [Tooltip("觸發後可在 Inspector 綁定 function（例如 GameStateManager.AddState）。")]
        public UnityEvent onTriggered;

        private InventoryUI inventoryUI;

        void Start()
        {
            // Unity 6 推薦 API：尋找（含非 UI）
            inventoryUI = Object.FindFirstObjectByType<InventoryUI>();

            // 如果沒指定要關閉誰，預設把自己加入清單，確保觸發後移除互動
            if (objectsToDisable == null || objectsToDisable.Count == 0)
            {
                objectsToDisable = new List<GameObject> { this.gameObject };
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (inventoryUI == null) return;

            // 1. 取得當前持有道具
            ItemData heldItem = inventoryUI.GetHeldItemData();

            // 2. 判斷 ID 是否符合需求（任一 ID 符合即可）
            if (heldItem != null && requiredItemIds != null && requiredItemIds.Contains(heldItem.id))
            {
                ExecuteTrigger();
            }
            else
            {
                // 不符合時播放失敗音效（選填）
                PlayClip(failClip);

                int currentHeldId = (heldItem != null) ? heldItem.id : 0;
                string requiredList = (requiredItemIds != null && requiredItemIds.Count > 0)
                    ? string.Join(", ", requiredItemIds)
                    : "（未設定）";
                Debug.Log($"[HeldItemTrigger] 道具不符合！需要 ID:[{requiredList}]，當前持有 ID:{currentHeldId}");
            }
        }

        /// <summary>
        /// 觸發後的完整流程。
        /// </summary>
        private void ExecuteTrigger()
        {
            // 1. 播放成功音效
            PlayClip(successClip);

            // 2. 消耗道具
            if (consumeItem)
            {
                inventoryUI.ClearHeldItem();
            }

            // 3. 逐一開啟目標物件，並同步 RoomManager
            if (panelsToOpen != null && panelsToOpen.Count > 0)
            {
                RoomUIManager uiManager = Object.FindFirstObjectByType<RoomUIManager>();

                foreach (GameObject panel in panelsToOpen)
                {
                    if (panel != null)
                    {
                        panel.SetActive(true);

                        if (uiManager != null)
                        {
                            uiManager.SyncByPanel(panel);
                        }
                        Debug.Log($"[HeldItemTrigger] 觸發後：已開啟 {panel.name}");
                    }
                }
            }

            // 4. 處理縮放替換
            if (newZoomPanelToReplace != null && UIPanelManager.Instance != null && UIPanelManager.Instance.IsBlockingInput)
            {
                UIPanelManager.Instance.ReplaceTopPanel(newZoomPanelToReplace);
                Debug.Log($"[HeldItemTrigger] 已縮放替換：{newZoomPanelToReplace.name}");
            }

            // 5. 逐一關閉（預設自己、或 Inspector 指定）
            if (objectsToDisable != null && objectsToDisable.Count > 0)
            {
                foreach (GameObject obj in objectsToDisable)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                        Debug.Log($"[HeldItemTrigger] 觸發後：已關閉 {obj.name}");
                    }
                }
            }

            // 6. 觸發事件
            onTriggered?.Invoke();
        }

        // ────────────────────────────────────────
        // 內部音效播放
        // ────────────────────────────────────────

        private void PlayClip(AudioClip clip)
        {
            if (clip == null) return;

            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.Play(clip, volume);
            }
            else
            {
                Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
                AudioSource.PlayClipAtPoint(clip, pos, volume);
            }
        }
    }
}