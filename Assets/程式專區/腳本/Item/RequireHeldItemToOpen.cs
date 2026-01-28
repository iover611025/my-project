using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace X
{
    public class RequireHeldItemToOpen : MonoBehaviour, IPointerClickHandler
    {
        public enum TransitionType { Simple, WithText }

        public int requiredItemId; // 需要握持的道具ID
        public InventoryUI inventoryUI; // 拖入InventoryUI
        public ToggleUIObject doorToggle; // 門的開關腳本
        public ItemDatabase itemDatabase;

        [Header("場景切換用")]
        public GameObject currentPanel; // 目前UI場景
        public GameObject nextPanel;    // 要切換的UI場景

        [Header("本地黑幕（fallback）")]
        public Image blackFadeImage;    // 黑幕Image（全螢幕，預設透明）
        public float localFadeDuration = 0.5f;
        public float localBlackStayDuration = 1.5f;

        [Header("本地黑幕文字（fallback，可選）")]
        public Text localBlackFadeText; // 若使用本地文字轉場，拖入此 Text
        public float localTextFadeDuration = 0.5f;

        [Header("使用 UICoverManager（可選）")]
        public UICoverManager uiCoverManager; // 如有則使用 UICoverManager 的轉場
        public TransitionType transitionType = TransitionType.Simple;
        public string transitionMessage = "";

        private bool isOpen = false;
        private bool _isSwitching = false; // 防止重複切換

        // 改為使用唯一 bigSceneId（優先使用 id）
        public RoomUIManager roomUIManager; // Inspector 拖入 RoomUIManager
        [Tooltip("要進入的大場景唯一 id（優先使用 id）")]
        public int targetBigSceneId = 1;
        private bool _shouldEnterBigScene = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isOpen)
            {
                if (itemDatabase == null)
                {
                    Debug.LogWarning("[RequireHeldItemToOpen] itemDatabase 未指派！");
                    return;
                }
                if (doorToggle == null)
                {
                    Debug.LogWarning("[RequireHeldItemToOpen] doorToggle 未指派！");
                    return;
                }

                // 優先透過 InventoryUI 的 API 取得握持資料
                ItemData heldData = null;
                if (inventoryUI != null)
                    heldData = inventoryUI.GetHeldItemData();

                bool emptyHand = IsEmptyHand(inventoryUI);

                if (emptyHand)
                {
                    Debug.Log("[RequireHeldItemToOpen] 請先握持正確的道具！");
                    return;
                }

                // 以 heldData 的 id 判斷（更可靠），並把 id==0 視為空手/預設
                if (heldData != null && heldData.id != 0 && heldData.id == requiredItemId)
                {
                    doorToggle.OnClick(); // 開門
                    isOpen = true;
                    Debug.Log("[RequireHeldItemToOpen] 門已開啟！");
                }
                else
                {
                    Debug.Log("[RequireHeldItemToOpen] 握持的不是正確的道具，無法開門！");
                }
            }
            else
            {
                // 門已開啟，僅允許空手切換場景
                if (_isSwitching)
                {
                    Debug.Log("[RequireHeldItemToOpen] 已在切換中，忽略點擊");
                    return;
                }

                if (IsEmptyHand(inventoryUI))
                {
                    _isSwitching = true;
                    _shouldEnterBigScene = true;

                    if (uiCoverManager != null)
                    {
                        if (transitionType == TransitionType.Simple)
                        {
                            uiCoverManager.FadeSwitchPanel(currentPanel, nextPanel);
                            StartCoroutine(WaitCoverComplete(false));
                        }
                        else
                        {
                            uiCoverManager.FadeSwitchPanelWithText(currentPanel, nextPanel, transitionMessage);
                            StartCoroutine(WaitCoverComplete(true));
                        }
                    }
                    else
                    {
                        if (transitionType == TransitionType.Simple)
                            StartCoroutine(FadeAndSwitchPanelSimple());
                        else
                            StartCoroutine(FadeAndSwitchPanelWithTextLocal(transitionMessage));
                    }
                }
                else
                {
                    Debug.Log("[RequireHeldItemToOpen] 仍持有物品，無法切換場景");
                }
            }
        }

        // 改為使用 InventoryUI 提供的 IsHeldEmpty 判斷（會把 id==0 視為空手）
        private bool IsEmptyHand(InventoryUI inv)
        {
            if (inv == null) return true;
            return inv.IsHeldEmpty();
        }

        private IEnumerator FadeAndSwitchPanelSimple()
        {
            if (_isSwitching) yield break;
            _isSwitching = true;

            if (blackFadeImage == null)
            {
                _isSwitching = false;
                yield break;
            }

            blackFadeImage.gameObject.SetActive(true);
            Color col = blackFadeImage.color;
            blackFadeImage.color = new Color(col.r, col.g, col.b, 0f);

            yield return StartCoroutine(FadeBlack(0f, 1f, localFadeDuration));

            // 嘗試讓 RoomUIManager 先同步並啟用目標 panel
            bool handledByRoomManager = false;
            if (_shouldEnterBigScene && roomUIManager != null)
            {
                handledByRoomManager = TryEnterAndActivateNextPanel();
                _shouldEnterBigScene = false;
            }

            // 若 RoomUIManager 未處理成功，使用原本的手動啟用/停用
            if (!handledByRoomManager)
            {
                if (currentPanel != null) currentPanel.SetActive(false);
                if (nextPanel != null) nextPanel.SetActive(true);
            }

            yield return new WaitForSecondsRealtime(localBlackStayDuration);

            yield return StartCoroutine(FadeBlack(1f, 0f, localFadeDuration));

            _isSwitching = false;
        }

        private IEnumerator FadeAndSwitchPanelWithTextLocal(string message)
        {
            if (_isSwitching) yield break;
            _isSwitching = true;

            if (blackFadeImage == null)
            {
                _isSwitching = false;
                yield break;
            }

            blackFadeImage.gameObject.SetActive(true);
            Color col = blackFadeImage.color;
            blackFadeImage.color = new Color(col.r, col.g, col.b, 0f);

            if (localBlackFadeText != null)
            {
                localBlackFadeText.gameObject.SetActive(true);
                localBlackFadeText.text = message ?? "";
                Color tc = localBlackFadeText.color;
                localBlackFadeText.color = new Color(tc.r, tc.g, tc.b, 0f);
            }

            yield return StartCoroutine(FadeBlack(0f, 1f, localFadeDuration));
            if (localBlackFadeText != null)
                yield return StartCoroutine(FadeTextLocal(0f, 1f, localTextFadeDuration));

            // 嘗試讓 RoomUIManager 先同步並啟用目標 panel
            bool handledByRoomManager = false;
            if (_shouldEnterBigScene && roomUIManager != null)
            {
                handledByRoomManager = TryEnterAndActivateNextPanel();
                _shouldEnterBigScene = false;
            }

            if (!handledByRoomManager)
            {
                if (currentPanel != null) currentPanel.SetActive(false);
                if (nextPanel != null) nextPanel.SetActive(true);
            }

            yield return new WaitForSecondsRealtime(localBlackStayDuration);

            if (localBlackFadeText != null)
                yield return StartCoroutine(FadeTextLocal(1f, 0f, localTextFadeDuration));

            yield return StartCoroutine(FadeBlack(1f, 0f, localFadeDuration));

            _isSwitching = false;
        }

        private IEnumerator WaitCoverComplete(bool withText)
        {
            if (uiCoverManager == null)
            {
                _isSwitching = false;
                yield break;
            }

            float fade = uiCoverManager.fadeDuration;
            float stay = uiCoverManager.blackStayDuration;
            float textFade = uiCoverManager.textFadeDuration;

            // 與先前相同：計算總過場時間與 activationDelay（在黑幕完全遮住時同步）
            float totalWait = Mathf.Max(fade * 2f + stay, textFade * 2f + stay);
            float activationDelay = fade + (withText ? textFade : 0f);
            activationDelay = Mathf.Clamp(activationDelay, 0f, totalWait);

            // 等到 UICoverManager 應該已經把黑幕淡入完（此時 toPanel 在 UICoverManager 內會被啟用或即將被啟用）
            yield return new WaitForSecondsRealtime(activationDelay);

            if (_shouldEnterBigScene && roomUIManager != null)
            {
                // 使用統一入口（包含更多診斷資訊與 fallback），避免 ActivatePanel 失敗後沒有進一步資訊
                bool ok = TryEnterAndActivateNextPanel();
                if (!ok)
                    Debug.LogWarning($"[RequireHeldItemToOpen] WaitCoverComplete: TryEnterAndActivateNextPanel returned false for nextPanel '{(nextPanel!=null?nextPanel.name:"null")}'.");
                _shouldEnterBigScene = false;
            }

            // 等待剩下的過場時間（fade out 等）
            float remaining = totalWait - activationDelay;
            if (remaining > 0f)
                yield return new WaitForSecondsRealtime(remaining);

            _isSwitching = false;
        }

        private IEnumerator FadeBlack(float from, float to, float duration)
        {
            if (blackFadeImage == null) yield break;

            float t = 0f;
            Color baseColor = new Color(blackFadeImage.color.r, blackFadeImage.color.g, blackFadeImage.color.b, 0f);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                blackFadeImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }
            blackFadeImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, to);
        }

        private IEnumerator FadeTextLocal(float from, float to, float duration)
        {
            if (localBlackFadeText == null) yield break;

            float t = 0f;
            Color baseColor = new Color(localBlackFadeText.color.r, localBlackFadeText.color.g, localBlackFadeText.color.b, 0f);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                localBlackFadeText.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }
            localBlackFadeText.color = new Color(baseColor.r, baseColor.g, baseColor.b, to);
        }

        // 嘗試以 RoomUIManager 切換並啟用 nextPanel，包含 fallback 與詳細日誌
        private bool TryEnterAndActivateNextPanel()
        {
            if (roomUIManager == null)
            {
                Debug.LogWarning("[RequireHeldItemToOpen] TryEnterAndActivateNextPanel: roomUIManager is null");
                return false;
            }

            // 先關閉 currentPanel（避免切換時原場景仍顯示）
            if (currentPanel != null && currentPanel.activeSelf)
            {
                if (Debug.isDebugBuild) Debug.Log($"[RequireHeldItemToOpen] Closing currentPanel '{currentPanel.name}' before bigScene switch.");
                currentPanel.SetActive(false);
            }

            Debug.Log($"[RequireHeldItemToOpen] TryEnterAndActivateNextPanel: attempting EnterBigSceneById({targetBigSceneId})");
            roomUIManager.EnterBigSceneById(targetBigSceneId, 0);

            // 新增：確保其他 bigScenes 的 panels 都被關閉（只保留目標 bigScene 面板）
            try
            {
                if (roomUIManager.bigScenes != null)
                {
                    for (int bi = 0; bi < roomUIManager.bigScenes.Count; bi++)
                    {
                        var bs = roomUIManager.bigScenes[bi];
                        if (bs == null || bs.roomPanels == null) continue;

                        // 若此 bigScene 與目標 id 不同，將其 panels 全部關閉
                        if (bs.id != targetBigSceneId)
                        {
                            for (int pj = 0; pj < bs.roomPanels.Length; pj++)
                            {
                                var rt = bs.roomPanels[pj];
                                if (rt != null && rt.gameObject != null && rt.gameObject.activeSelf)
                                {
                                    if (Debug.isDebugBuild) Debug.Log($"[RequireHeldItemToOpen] Deactivating panel '{rt.gameObject.name}' from bigScene id={bs.id}");
                                    rt.gameObject.SetActive(false);
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RequireHeldItemToOpen] Deactivating other bigScenes failed: {ex.Message}");
            }

            if (nextPanel == null)
            {
                Debug.LogWarning("[RequireHeldItemToOpen] TryEnterAndActivateNextPanel: nextPanel is null");
                return true; // Entered big scene but no panel to activate
            }

            Debug.Log($"[RequireHeldItemToOpen] TryEnterAndActivateNextPanel: attempting ActivatePanel('{nextPanel.name}')");
            bool activated = roomUIManager.ActivatePanel(nextPanel);
            if (activated)
            {
                Debug.Log($"[RequireHeldItemToOpen] ActivatePanel succeeded for '{nextPanel.name}'");
                return true;
            }

            Debug.LogWarning($"[RequireHeldItemToOpen] ActivatePanel failed for '{nextPanel.name}'. 嘗試使用 SyncToPanel 作為 fallback。");
            bool synced = roomUIManager.SyncToPanel(nextPanel);
            if (synced)
            {
                Debug.Log($"[RequireHeldItemToOpen] SyncToPanel succeeded for '{nextPanel.name}'");
                return true;
            }

            // 更詳細的診斷輸出：列出 RoomUIManager 中的 bigScenes 與各 panel 名稱/索引，方便找出 mapping 差異
            try
            {
                Debug.Log("[RequireHeldItemToOpen] TryEnterAndActivateNextPanel: 列出 RoomUIManager.bigScenes 與 roomPanels 以協助排查：");
                var bsList = roomUIManager.bigScenes;
                if (bsList == null || bsList.Count == 0)
                {
                    Debug.Log("[RequireHeldItemToOpen] RoomUIManager.bigScenes 為空。");
                }
                else
                {
                    for (int i = 0; i < bsList.Count; i++)
                    {
                        var bs = bsList[i];
                        if (bs == null)
                        {
                            Debug.Log($"  bigScene[{i}] = null");
                            continue;
                        }
                        string header = $"  bigScene[{i}] id={bs.id} name='{bs.sceneName}' panels={(bs.roomPanels!=null?bs.roomPanels.Length:0)}";
                        Debug.Log(header);
                        if (bs.roomPanels != null)
                        {
                            for (int j = 0; j < bs.roomPanels.Length; j++)
                            {
                                var rt = bs.roomPanels[j];
                                string name = rt != null && rt.gameObject != null ? rt.gameObject.name : "null";
                                Debug.Log($"    panel[{j}] = '{name}' (gameObject={(rt!=null && rt.gameObject!=null?rt.gameObject.ToString():"null")})");
                            }
                        }
                    }
                }

                // 也列出 fallback roomPanels（若存在）
                if (roomUIManager.roomPanels != null && roomUIManager.roomPanels.Length > 0)
                {
                    Debug.Log($"[RequireHeldItemToOpen] RoomUIManager.fallback roomPanels count = {roomUIManager.roomPanels.Length}");
                    for (int k = 0; k < roomUIManager.roomPanels.Length; k++)
                    {
                        var rt = roomUIManager.roomPanels[k];
                        string name = rt != null && rt.gameObject != null ? rt.gameObject.name : "null";
                        Debug.Log($"    fallback[{k}] = '{name}'");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RequireHeldItemToOpen] 列出 bigScenes 失敗: {ex.Message}");
            }

            Debug.LogWarning($"[RequireHeldItemToOpen] TryEnterAndActivateNextPanel: 無法找到 nextPanel '{nextPanel.name}' 在 RoomUIManager 的 mappings 中。請確認：\n" +
                             $"- targetBigSceneId 是否正確對應到 RoomUIManager.bigScenes 內的某個 bigScene.id\n" +
                             $"- nextPanel 是否為該 bigScene.roomPanels 中的 panel（或其子物件）\n" +
                             $"- roomUIManager 是否為正確的實例（Inspector 指派）");
            return false;
        }
    }
}