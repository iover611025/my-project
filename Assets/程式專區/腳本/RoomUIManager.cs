using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace X
{
    public class RoomUIManager : MonoBehaviour
    {
        [Header("可設定多個大場景（拖放）")]
        public List<BigScene> bigScenes = new List<BigScene>();

        [Header("備用：單一房間列表（若不使用大場景）")]
        public RectTransform[] roomPanels; // 備用（相容舊版）

        [Header("UI轉場管理器")]
        public UICoverManager uiCoverManager; // Inspector拖入（可為 null）

        [Header("實際房間管理（同步實際房間 GameObject）")]
        public RoomManager roomManager; // Inspector 拖入以同步實際房間切換

        // 可在 Inspector 開啟詳細除錯
        public bool enableDebugLogs = true;

        // currentBigSceneIndex == -1 表示使用 fallback roomPanels
        private int currentBigSceneIndex = 0;
        private int currentRoomIndex = 0;

        private bool isTransitioning = false; // 轉場中禁止輸入

        void OnEnable()
        {
            if (enableDebugLogs) Debug.Log("[RoomUIManager] OnEnable");
        }

        void Start()
        {
            if (enableDebugLogs) Debug.Log($"[RoomUIManager] Start currentBigSceneIndex={currentBigSceneIndex} currentRoomIndex={currentRoomIndex}");
            ValidateConfiguration(); // 新增：檢查 Inspector 設定協助除錯
            ShowRoom(currentRoomIndex);
        }

        void Update()
        {
            if (isTransitioning) return; // 轉場期間禁止輸入

            // 新增：當 PanelActivator 正在顯示 returnImage（等待玩家點擊返回）時，禁止 A/D 切換
            if (PanelActivator.IsBlockingInput)
            {
                if (enableDebugLogs) Debug.Log("[RoomUIManager] Update: input blocked by PanelActivator (returnImage shown)");
                return;
            }

            // 若 UICoverManager 正在播放黑幕過場，也禁止輸入（避免快速按鍵穿透）
            if ((uiCoverManager != null && uiCoverManager.IsFading) || UICoverManager.GlobalIsFading) return;

            // 若主選單為目前顯示的 Panel，禁止使用 A/D 切換
            if (uiCoverManager != null)
            {
                var active = uiCoverManager.GetCurrentlyActivePanel();
                if (active != null && active == uiCoverManager.mainMenuPanel)
                    return;
            }

            var panels = GetCurrentPanels();
            if (panels == null || panels.Length == 0) return;

            if (Input.GetKeyDown(KeyCode.A))
            {
                if (enableDebugLogs) Debug.Log($"[RoomUIManager] Update: Key A pressed (currentBigSceneIndex={currentBigSceneIndex} currentRoomIndex={currentRoomIndex}) panels.Length={panels.Length}");
                SwitchRoom(-1);
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (enableDebugLogs) Debug.Log($"[RoomUIManager] Update: Key D pressed (currentBigSceneIndex={currentBigSceneIndex} currentRoomIndex={currentRoomIndex}) panels.Length={panels.Length}");
                SwitchRoom(1);
            }
        }

        // 方向 -1 或 +1（已改為循環）
        public void SwitchRoom(int direction)
        {
            var panels = GetCurrentPanels();
            if (panels == null || panels.Length == 0)
            {
                if (enableDebugLogs) Debug.LogWarning("[RoomUIManager] SwitchRoom: no panels available");
                return;
            }

            // 以目前實際顯示的 panel 為準，避免 currentRoomIndex 舊值導致跳回其他 bigScene
            int activeIndex = GetActivePanelIndex(panels);
            int baseIndex = activeIndex >= 0 ? activeIndex : currentRoomIndex;

            int n = panels.Length;
            int raw = baseIndex + direction;
            int newIndex = ((raw % n) + n) % n; // 正確處理負方向的循環

            if (newIndex != baseIndex)
            {
                if (enableDebugLogs) Debug.Log($"[RoomUIManager] SwitchRoom: direction={direction} from={baseIndex} to={newIndex} (currentBigSceneIndex={currentBigSceneIndex})");
                StartCoroutine(RoomTransition(baseIndex, newIndex));
                currentRoomIndex = newIndex;
            }
            else if (enableDebugLogs)
            {
                Debug.Log($"[RoomUIManager] SwitchRoom: wrapped to same index {baseIndex}");
            }
        }

        // 找到目前 panels 中被啟用的索引（沒有則回傳 -1）
        private int GetActivePanelIndex(RectTransform[] panels)
        {
            if (panels == null) return -1;
            for (int i = 0; i < panels.Length; i++)
            {
                var rt = panels[i];
                if (rt != null && rt.gameObject != null && rt.gameObject.activeSelf)
                    return i;
            }
            return -1;
        }

        private IEnumerator RoomTransition(int fromIndex, int toIndex)
        {
            isTransitioning = true;

            var panels = GetCurrentPanels();
            if (enableDebugLogs)
            {
                Debug.Log($"[RoomUIManager] RoomTransition start from={fromIndex} to={toIndex} panelsExists={(panels != null)} panelsLength={(panels != null ? panels.Length : 0)}");
                if (panels != null)
                {
                    for (int i = 0; i < panels.Length; i++)
                        Debug.Log($"[RoomUIManager] panel[{i}] name={(panels[i] != null ? panels[i].gameObject.name : "null")} active={(panels[i] != null ? panels[i].gameObject.activeSelf.ToString() : "-")}");
                }
            }

            GameObject fromPanel = panels != null && panels.Length > fromIndex && panels[fromIndex] != null ? panels[fromIndex].gameObject : null;
            GameObject toPanel = panels != null && panels.Length > toIndex && panels[toIndex] != null ? panels[toIndex].gameObject : null;

            // 如果目標 panel 的父物件被停用，先啟用它的 ancestors（避免後續切換跳回其他 bigScene）
            if (toPanel != null && !toPanel.activeInHierarchy)
            {
                ActivateAncestors(toPanel);
            }

            if (uiCoverManager != null)
            {
                // 啟動 UICover 的過場（UICoverManager 會在淡入完成時把 toPanel 設為 active）
                uiCoverManager.FadeSwitchPanel(fromPanel, toPanel);

                // 計算等待時間與 activationDelay（在黑幕淡入完成時就執行實際的 panels 切換與 RoomManager 同步）
                float fade = uiCoverManager.fadeDuration;
                float stay = uiCoverManager.blackStayDuration;
                float waitTime = fade * 2f + stay;
                float activationDelay = fade; // 在淡入完成時就執行 activation

                // 等待到黑幕已覆蓋（fade in 完成）
                yield return new WaitForSecondsRealtime(activationDelay);

                // 這裡立刻把 panels 狀態設為目標（確保 UI 與 RoomManager 同步，且是在黑幕下完成）
                var postPanels = GetCurrentPanels();
                if (postPanels != null && postPanels.Length > 0)
                {
                    if (enableDebugLogs) Debug.Log($"[RoomUIManager] Post-fade (at activationDelay) ActivateOnly index={toIndex}, panels.Length={postPanels.Length}");
                    RoomHelper.ActivateOnly(postPanels, toIndex);

                    if (enableDebugLogs)
                    {
                        for (int i = 0; i < postPanels.Length; i++)
                            Debug.Log($"[RoomUIManager] PostPanel[{i}] name={(postPanels[i] != null ? postPanels[i].gameObject.name : "null")} active={(postPanels[i] != null ? postPanels[i].gameObject.activeSelf.ToString() : "-")}");
                    }

                    var toObj = postPanels.Length > toIndex && postPanels[toIndex] != null ? postPanels[toIndex].gameObject : null;
                    EnsureUIPanelVisible(toObj);
                }
                else if (roomPanels != null && roomPanels.Length > 0)
                {
                    if (enableDebugLogs) Debug.Log($"[RoomUIManager] Post-fade fallback ActivateOnly index={toIndex}, roomPanels.Length={roomPanels.Length}");
                    RoomHelper.ActivateOnly(roomPanels, toIndex);

                    var toObj = roomPanels.Length > toIndex && roomPanels[toIndex] != null ? roomPanels[toIndex].gameObject : null;
                    EnsureUIPanelVisible(toObj);
                }
                else
                {
                    Debug.LogWarning("[RoomUIManager] No panels found to activate during transition.");
                }

                // 同步實際房間索引（若有） — 也在黑幕下完成
                if (roomManager != null)
                {
                    if (enableDebugLogs) Debug.Log($"[RoomUIManager] Syncing RoomManager.SetRoomIndex({toIndex}) at activationDelay");
                    roomManager.SetRoomIndex(toIndex);
                }

                // 等候剩下的過場時間（黑幕停留 + fade out）
                float remaining = waitTime - activationDelay;
                if (remaining > 0f)
                    yield return new WaitForSecondsRealtime(remaining);
            }
            else
            {
                // 沒有 UICoverManager，原本流程：短暫等待，然後在此處處理 panels 切換
                yield return null;

                var postPanels = GetCurrentPanels();
                if (postPanels != null && postPanels.Length > 0)
                {
                    if (enableDebugLogs) Debug.Log($"[RoomUIManager] Post-fade ActivateOnly index={toIndex}, panels.Length={postPanels.Length}");
                    RoomHelper.ActivateOnly(postPanels, toIndex);

                    if (enableDebugLogs)
                    {
                        for (int i = 0; i < postPanels.Length; i++)
                            Debug.Log($"[RoomUIManager] PostPanel[{i}] name={(postPanels[i] != null ? postPanels[i].gameObject.name : "null")} active={(postPanels[i] != null ? postPanels[i].gameObject.activeSelf.ToString() : "-")}");
                    }

                    var toObj = postPanels.Length > toIndex && postPanels[toIndex] != null ? postPanels[toIndex].gameObject : null;
                    EnsureUIPanelVisible(toObj);
                }
                else if (roomPanels != null && roomPanels.Length > 0)
                {
                    if (enableDebugLogs) Debug.Log($"[RoomUIManager] Post-fade fallback ActivateOnly index={toIndex}, roomPanels.Length={roomPanels.Length}");
                    RoomHelper.ActivateOnly(roomPanels, toIndex);

                    var toObj = roomPanels.Length > toIndex && roomPanels[toIndex] != null ? roomPanels[toIndex].gameObject : null;
                    EnsureUIPanelVisible(toObj);
                }
                else
                {
                    Debug.LogWarning("[RoomUIManager] No panels found to activate after transition.");
                }

                if (roomManager != null)
                {
                    if (enableDebugLogs) Debug.Log($"[RoomUIManager] Syncing RoomManager.SetRoomIndex({toIndex})");
                    roomManager.SetRoomIndex(toIndex);
                }
            }

            isTransitioning = false;
            if (enableDebugLogs) Debug.Log($"[RoomUIManager] RoomTransition end to={toIndex}");
        }

        // 簡單的可視性檢查與修正（非破壞性）
        private void EnsureUIPanelVisible(GameObject panel)
        {
            if (panel == null)
            {
                if (enableDebugLogs) Debug.LogWarning("[RoomUIManager] EnsureUIPanelVisible: panel is null");
                return;
            }

            // 檢查 ancestors 是否都 active；若父物件被停用就自動啟用（由上而下）
            bool allActive = panel.activeInHierarchy;
            if (!allActive)
            {
                if (enableDebugLogs) Debug.LogWarning($"[RoomUIManager] Panel '{panel.name}' activeInHierarchy=false. 嘗試啟用被停用的父物件。");

                var toActivate = new List<GameObject>();
                Transform p = panel.transform.parent;
                while (p != null)
                {
                    if (!p.gameObject.activeSelf)
                        toActivate.Add(p.gameObject);
                    p = p.parent;
                }
                for (int i = toActivate.Count - 1; i >= 0; i--)
                {
                    var go = toActivate[i];
                    if (go != null)
                    {
                        if (enableDebugLogs) Debug.Log($"[RoomUIManager] Activating parent '{go.name}' to ensure '{panel.name}' visible.");
                        go.SetActive(true);
                    }
                }
            }

            // 若有 CanvasGroup 且透明或blocksRaycasts導致不可見，調整 alpha（只在偵錯模式下）
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg != null && cg.alpha <= 0.01f)
            {
                if (enableDebugLogs) Debug.Log($"[RoomUIManager] CanvasGroup alpha is {cg.alpha} on '{panel.name}'. Setting alpha=1 for visibility.");
                cg.alpha = 1f;
            }

            // 若 panel 本身有 Canvas，確保它啟用
            var canvas = panel.GetComponent<Canvas>();
            if (canvas != null && !canvas.enabled)
            {
                if (enableDebugLogs) Debug.Log($"[RoomUIManager] Canvas.enabled=false on '{panel.name}', enabling it.");
                canvas.enabled = true;
            }

            // 將該 panel 推到同父層的最後（讓它在兄弟之上）
            try
            {
                panel.transform.SetAsLastSibling();
                if (enableDebugLogs) Debug.Log($"[RoomUIManager] SetAsLastSibling called on '{panel.name}'.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RoomUIManager] SetAsLastSibling exception on '{panel.name}': {ex.Message}");
            }
        }

        // 在 RoomUIManager 類別內新增 ShowRoom 方法
        public void ShowRoom(int roomIndex)
        {
            var panels = GetCurrentPanels();
            if ((panels == null || panels.Length == 0) && (roomPanels == null || roomPanels.Length == 0))
            {
                if (enableDebugLogs) Debug.LogWarning("[RoomUIManager] ShowRoom: no panels to show");
                return;
            }

            // 優先使用當前大場景的 panels
            if (panels != null && panels.Length > 0)
            {
                int idx = Mathf.Clamp(roomIndex, 0, panels.Length - 1);
                RoomHelper.ActivateOnly(panels, idx);
                var toObj = panels.Length > idx && panels[idx] != null ? panels[idx].gameObject : null;
                EnsureUIPanelVisible(toObj);
                currentRoomIndex = idx;
            }
            else // fallback to single roomPanels
            {
                int idx = Mathf.Clamp(roomIndex, 0, roomPanels.Length - 1);
                RoomHelper.ActivateOnly(roomPanels, idx);
                var toObj = roomPanels.Length > idx && roomPanels[idx] != null ? roomPanels[idx].gameObject : null;
                EnsureUIPanelVisible(toObj);
                currentRoomIndex = idx;
            }

            // 同步實際房間索引（若有）
            if (roomManager != null)
            {
                if (enableDebugLogs) Debug.Log($"[RoomUIManager] ShowRoom: Syncing RoomManager.SetRoomIndex({currentRoomIndex})");
                roomManager.SetRoomIndex(currentRoomIndex);
            }
        }

        // 新增：進入大場景（切換 bigScenes 的索引並顯示該場景的指定房間）
        public void EnterBigScene(int bigSceneIndex, int startRoomIndex = 0)
        {
            if (bigScenes != null && bigScenes.Count > 0 && bigSceneIndex >= 0 && bigSceneIndex < bigScenes.Count)
            {
                // 先停用所有 bigScene 的 panels（避免 shared reference 或舊場景殘留）
                try
                {
                    for (int i = 0; i < bigScenes.Count; i++)
                    {
                        var bsAll = bigScenes[i];
                        if (bsAll == null || bsAll.roomPanels == null) continue;
                        for (int j = 0; j < bsAll.roomPanels.Length; j++)
                        {
                            var rt = bsAll.roomPanels[j];
                            if (rt != null && rt.gameObject != null && rt.gameObject.activeSelf)
                            {
                                if (enableDebugLogs) Debug.Log($"[RoomUIManager] Deactivating panel '{rt.gameObject.name}' from bigScene index {i} id={bsAll.id}");
                                rt.gameObject.SetActive(false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RoomUIManager] EnterBigScene: error deactivating other panels: {ex.Message}");
                }

                currentBigSceneIndex = bigSceneIndex;
                var panels = bigScenes[bigSceneIndex]?.roomPanels;
                if (panels != null && panels.Length > 0)
                {
                    int idx = Mathf.Clamp(startRoomIndex, 0, panels.Length - 1);

                    // 確保目標 panel 的父物件鏈先被啟用（避免 activeInHierarchy = false）
                    var target = panels[idx] != null ? panels[idx].gameObject : null;
                    if (target != null && !target.activeInHierarchy)
                        ActivateAncestors(target);

                    RoomHelper.ActivateOnly(panels, idx);
                    EnsureUIPanelVisible(panels[idx].gameObject);
                    currentRoomIndex = idx;
                    if (roomManager != null) roomManager.SetRoomIndex(idx);
                    if (enableDebugLogs) Debug.Log($"[RoomUIManager] EnterBigScene: entered bigScene {bigSceneIndex} startRoom {idx} (id={bigScenes[bigSceneIndex].id})");
                    return;
                }
            }

            // fallback: treat roomPanels as single scene
            if (roomPanels != null && roomPanels.Length > 0)
            {
                int idx = Mathf.Clamp(startRoomIndex, 0, roomPanels.Length - 1);
                RoomHelper.ActivateOnly(roomPanels, idx);
                EnsureUIPanelVisible(roomPanels[idx].gameObject);
                currentRoomIndex = idx;
                if (roomManager != null) roomManager.SetRoomIndex(idx);
                if (enableDebugLogs) Debug.Log($"[RoomUIManager] EnterBigScene: fallback startRoom {idx}");
                return;
            }

            if (enableDebugLogs) Debug.LogWarning("[RoomUIManager] EnterBigScene: no panels available to enter.");
        }

        // 以唯一 id 找到 bigScene 並進入（優先推薦使用 id）
        public void EnterBigSceneById(int bigSceneId, int startRoomIndex = 0)
        {
            if (bigScenes == null || bigScenes.Count == 0)
            {
                if (enableDebugLogs) Debug.LogWarning("[RoomUIManager] EnterBigSceneById: no bigScenes defined");
                return;
            }

            for (int i = 0; i < bigScenes.Count; i++)
            {
                var bs = bigScenes[i];
                if (bs != null && bs.id == bigSceneId)
                {
                    EnterBigScene(i, startRoomIndex);
                    return;
                }
            }

            if (enableDebugLogs) Debug.LogWarning($"[RoomUIManager] EnterBigSceneById: no bigScene found with id {bigSceneId}");
        }

        // 新增：以名稱找到 bigScene 並進入（相容需求）
        public void EnterBigSceneByName(string sceneName, int startRoomIndex = 0)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                if (enableDebugLogs) Debug.LogWarning("[RoomUIManager] EnterBigSceneByName: sceneName is null or empty");
                return;
            }

            if (bigScenes == null || bigScenes.Count == 0)
            {
                if (enableDebugLogs) Debug.LogWarning("[RoomUIManager] EnterBigSceneByName: no bigScenes defined");
                return;
            }

            for (int i = 0; i < bigScenes.Count; i++)
            {
                var bs = bigScenes[i];
                if (bs != null && !string.IsNullOrEmpty(bs.sceneName) &&
                    string.Equals(bs.sceneName.Trim(), sceneName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    EnterBigScene(i, startRoomIndex);
                    return;
                }
            }

            if (enableDebugLogs) Debug.LogWarning($"[RoomUIManager] EnterBigSceneByName: no bigScene found with name '{sceneName}'");
        }

        // 取得目前有效的 panels（依 bigScenes 或 fallback roomPanels）
        private RectTransform[] GetCurrentPanels()
        {
            if (bigScenes != null && bigScenes.Count > 0)
            {
                if (currentBigSceneIndex >= 0 && currentBigSceneIndex < bigScenes.Count)
                {
                    var bs = bigScenes[currentBigSceneIndex];
                    if (bs != null && bs.roomPanels != null && bs.roomPanels.Length > 0)
                        return bs.roomPanels;
                }
            }

            if (roomPanels != null && roomPanels.Length > 0)
                return roomPanels;

            return null;
        }

        // 只修改 MatchPanel，移除「同名比對」，改為嚴格比對或子物件比對
        private bool MatchPanel(RectTransform rt, GameObject panel)
        {
            if (rt == null || panel == null) return false;
            // 完全相等（首選）
            if (rt.gameObject == panel) return true;
            // 若 panel 是 rt 的子物件（nextPanel 可能是內層子物件）
            if (panel.transform.IsChildOf(rt.transform)) return true;
            // 不再以名稱相同視為 match（會造成不同 bigScene 的同名 panel 被誤判）
            return false;
        }

        // 公開：根據已啟用的或剛被啟用的 panel 來同步管理狀態（供外部呼叫）
        public bool SyncToPanel(GameObject panel)
        {
            if (panel == null) return false;

            if (bigScenes != null)
            {
                for (int i = 0; i < bigScenes.Count; i++)
                {
                    var bs = bigScenes[i];
                    if (bs == null || bs.roomPanels == null) continue;
                    for (int j = 0; j < bs.roomPanels.Length; j++)
                    {
                        var rt = bs.roomPanels[j];
                        if (rt == null) continue;
                        if (MatchPanel(rt, panel))
                        {
                            currentBigSceneIndex = i;
                            currentRoomIndex = j;
                            if (roomManager != null) roomManager.SetRoomIndex(j);
                            if (enableDebugLogs) Debug.Log($"[RoomUIManager] SyncToPanel: matched bigScene[{i}] room[{j}] for '{panel.name}'");
                            return true;
                        }
                    }
                }
            }

            if (roomPanels != null)
            {
                for (int j = 0; j < roomPanels.Length; j++)
                {
                    var rt = roomPanels[j];
                    if (rt == null) continue;
                    if (MatchPanel(rt, panel))
                    {
                        currentBigSceneIndex = -1; // use fallback
                        currentRoomIndex = j;
                        if (roomManager != null) roomManager.SetRoomIndex(j);
                        if (enableDebugLogs) Debug.Log($"[RoomUIManager] SyncToPanel: matched fallback roomPanels[{j}] for '{panel.name}'");
                        return true;
                    }
                }
            }

            if (enableDebugLogs) Debug.LogWarning($"[RoomUIManager] SyncToPanel: panel '{panel.name}' not found in bigScenes or fallback roomPanels.");
            return false;
        }

        // 公開：直接以 GameObject 為目標啟用對應 panel（會更新 currentBigSceneIndex/currentRoomIndex 並啟用該陣列）
        public bool ActivatePanel(GameObject panel)
        {
            if (panel == null) return false;

            if (bigScenes != null)
            {
                for (int i = 0; i < bigScenes.Count; i++)
                {
                    var bs = bigScenes[i];
                    if (bs == null || bs.roomPanels == null) continue;
                    for (int j = 0; j < bs.roomPanels.Length; j++)
                    {
                        var rt = bs.roomPanels[j];
                        if (rt == null) continue;
                        if (MatchPanel(rt, panel))
                        {
                            currentBigSceneIndex = i;
                            currentRoomIndex = j;
                            RoomHelper.ActivateOnly(bs.roomPanels, j);
                            EnsureUIPanelVisible(bs.roomPanels[j].gameObject);
                            if (roomManager != null) roomManager.SetRoomIndex(j);
                            if (enableDebugLogs) Debug.Log($"[RoomUIManager] ActivatePanel: activated bigScene[{i}] room[{j}] for '{panel.name}'");
                            return true;
                        }
                    }
                }
            }

            if (roomPanels != null)
            {
                for (int j = 0; j < roomPanels.Length; j++)
                {
                    var rt = roomPanels[j];
                    if (rt == null) continue;
                    if (MatchPanel(rt, panel))
                    {
                        currentBigSceneIndex = -1;
                        currentRoomIndex = j;
                        RoomHelper.ActivateOnly(roomPanels, j);
                        EnsureUIPanelVisible(roomPanels[j].gameObject);
                        if (roomManager != null) roomManager.SetRoomIndex(j);
                        if (enableDebugLogs) Debug.Log($"[RoomUIManager] ActivatePanel: activated fallback roomPanels[{j}] for '{panel.name}'");
                        return true;
                    }
                }
            }

            if (enableDebugLogs) Debug.LogWarning($"[RoomUIManager] ActivatePanel: panel '{panel.name}' not found.");
            return false;
        }

        // 幫助：若目標 panel 的父物件被停用，從上到下啟用需要的 ancestors
        private void ActivateAncestors(GameObject panel)
        {
            if (panel == null) return;
            var toEnable = new List<GameObject>();
            Transform p = panel.transform.parent;
            while (p != null)
            {
                if (!p.gameObject.activeSelf)
                    toEnable.Add(p.gameObject);
                p = p.parent;
            }
            for (int i = toEnable.Count - 1; i >= 0; i--)
            {
                var go = toEnable[i];
                if (go != null)
                {
                    if (enableDebugLogs) Debug.Log($"[RoomUIManager] Activating ancestor '{go.name}' for panel '{panel.name}'.");
                    go.SetActive(true);
                }
            }
        }

        // 新增：檢查 Inspector 設定（在 Start 呼叫）
        private void ValidateConfiguration()
        {
            if (!enableDebugLogs) return;

            // 列印 bigScenes 內容
            if (bigScenes != null && bigScenes.Count > 0)
            {
                Debug.Log($"[RoomUIManager] bigScenes.Count = {bigScenes.Count}");
                var seenIds = new HashSet<int>();
                var panelMap = new Dictionary<GameObject, int>();
                for (int i = 0; i < bigScenes.Count; i++)
                {
                    var bs = bigScenes[i];
                    if (bs == null)
                    {
                        Debug.LogWarning($"[RoomUIManager] bigScenes[{i}] is null");
                        continue;
                    }
                    Debug.Log($"[RoomUIManager] bigScene[{i}] id={bs.id} name='{bs.sceneName}' panels={(bs.roomPanels != null ? bs.roomPanels.Length : 0)}");
                    if (seenIds.Contains(bs.id))
                        Debug.LogWarning($"[RoomUIManager] Duplicate bigScene id detected: {bs.id} (index {i})");
                    else
                        seenIds.Add(bs.id);

                    if (bs.roomPanels == null || bs.roomPanels.Length == 0)
                        Debug.LogWarning($"[RoomUIManager] bigScene[{i}] has no roomPanels assigned.");

                    if (bs.roomPanels != null)
                    {
                        for (int j = 0; j < bs.roomPanels.Length; j++)
                        {
                            var rt = bs.roomPanels[j];
                            if (rt == null) continue;
                            var go = rt.gameObject;
                            if (panelMap.TryGetValue(go, out int prev))
                            {
                                Debug.LogWarning($"[RoomUIManager] Panel '{go.name}' is used in bigScene[{prev}] and bigScene[{i}] (shared reference).");
                            }
                            else
                            {
                                panelMap[go] = i;
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.Log("[RoomUIManager] no bigScenes configured.");
            }

            if (bigScenes != null && bigScenes.Count > 0 && roomPanels != null && roomPanels.Length > 0)
            {
                Debug.LogWarning("[RoomUIManager] Both bigScenes and fallback roomPanels are populated. If you use bigScenes, clear or leave fallback roomPanels empty to avoid confusion.");
            }
        }
    }
}