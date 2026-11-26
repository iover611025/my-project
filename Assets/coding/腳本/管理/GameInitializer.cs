using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace X
{
    /// <summary>
    /// 遊戲初始化與重玩管理器。
    /// 負責：
    ///   1. 遊戲啟動時的基礎環境初始化（TimeScale、全局過場狀態）。
    ///   2. 通關或重玩時：清理遊戲進度（GameStateManager、背包、線索本存檔、面板/對話），
    ///      保留玩家設定（音量 Slider、SettingsToggleController 開關、攝影機晃動），
    ///      並重新載入場景停在主選單。
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        public static GameInitializer Instance { get; private set; }

        [Header("場景設定")]
        [Tooltip("要重載的場景名稱；留空則重新載入目前啟用的場景")]
        [SerializeField] private string targetSceneName = "";

        [Header("重玩轉場（選填）")]
        [Tooltip("重載前是否先淡入黑幕（需場景內有 UICoverManager 且已設定 blackFadeImage）")]
        [SerializeField] private bool fadeOutBeforeReload = true;

        [Header("大場景記憶設定")]
        [Tooltip("呼叫記憶大場景重玩時，是否連同當時的房間索引一起記住（關閉則預設從該大場景的第 0 房開始）")]
        [SerializeField] private bool rememberRoomIndex = false;

        [Header("不需要初始化的物件")]
        [Tooltip("放入此清單的物件在重玩時不會被重置（保留其顯示/隱藏狀態，並跳過其上的背包與線索本清除）。")]
        [SerializeField] private List<GameObject> excludedObjects = new List<GameObject>();

        [Header("除錯")]
        [SerializeField] private bool enableDebugLogs = true;

        private bool _isRestarting = false;

        // 跨場景重載保留的「開始遊戲目標大場景」
        private struct PendingBigSceneTarget
        {
            public bool hasTarget;
            public bool useId;
            public int bigSceneId;
            public int bigSceneIndex;
            public int roomIndex;
        }

        private static PendingBigSceneTarget _pendingBigScene;

        // 跨場景重載保留的設定快照
        private struct SettingsSnapshot
        {
            public bool hasData;
            public float musicVolume;
            public bool hasMusicSlider;
            public float sfxVolume;
            public bool hasSfxSlider;
            public bool cameraSwayActive;
            public bool hasCameraSway;
            public Dictionary<string, bool> toggleStates;
            public Dictionary<string, bool> excludedObjectActiveStates;
        }

        private static SettingsSnapshot _pendingSettings;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeRuntimeDefaults();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// 遊戲啟動或重載後的基礎環境初始化。
        /// </summary>
        private void InitializeRuntimeDefaults()
        {
            Time.timeScale = 1f;
            UICoverManager.ResetGlobalFadeState();

            if (EventSystem.current != null && !EventSystem.current.enabled)
                EventSystem.current.enabled = true;
        }

        // ──────────────────────────────────────────────
        // 公開 API（供通關事件或 UI 按鈕 OnClick 綁定）
        // ──────────────────────────────────────────────

        /// <summary>
        /// 重玩遊戲：清理進度、保留設定、重新載入場景並停在主選單（開始遊戲後從預設大場景開始）。
        /// </summary>
        public void RestartGame()
        {
            _pendingBigScene = default;
            RestartGame(targetSceneName);
        }

        /// <summary>
        /// 重玩遊戲（指定目標 Unity 場景名稱）。
        /// </summary>
        public void RestartGame(string sceneName)
        {
            if (_isRestarting) return;

            string sceneToLoad = string.IsNullOrEmpty(sceneName)
                ? SceneManager.GetActiveScene().name
                : sceneName;

            StartCoroutine(RestartRoutine(sceneToLoad));
        }

        /// <summary>
        /// 記住呼叫當下的大場景，初始化並回到主選單；
        /// 之後玩家在主選單點擊「開始遊戲」時，會直接進入該大場景。
        /// </summary>
        public void RestartGameRememberCurrentBigScene()
        {
            if (_isRestarting) return;

            var roomUI = RoomUIManager.Instance != null
                ? RoomUIManager.Instance
                : FindFirstObjectByType<RoomUIManager>(FindObjectsInactive.Include);

            if (roomUI != null)
            {
                int id = roomUI.CurrentBigSceneId;
                int idx = roomUI.CurrentBigSceneIndex;
                int room = rememberRoomIndex ? roomUI.CurrentRoomIndex : 0;

                _pendingBigScene = new PendingBigSceneTarget
                {
                    hasTarget = true,
                    useId = id >= 0,
                    bigSceneId = id,
                    bigSceneIndex = idx,
                    roomIndex = room
                };

                if (enableDebugLogs)
                    Debug.Log($"[GameInitializer] 已記住大場景：id={id}, idx={idx}, room={room}");
            }
            else
            {
                _pendingBigScene = default;
                Debug.LogWarning("[GameInitializer] 找不到 RoomUIManager，無法記住當前大場景。");
            }

            RestartGame(targetSceneName);
        }

        /// <summary>
        /// 指定目標大場景 ID，初始化並回到主選單；
        /// 之後玩家在主選單點擊「開始遊戲」時，會直接進入該大場景。
        /// </summary>
        public void RestartGameAndReturnToBigScene(int bigSceneId)
        {
            if (_isRestarting) return;

            _pendingBigScene = new PendingBigSceneTarget
            {
                hasTarget = true,
                useId = true,
                bigSceneId = bigSceneId,
                bigSceneIndex = -1,
                roomIndex = 0
            };

            if (enableDebugLogs)
                Debug.Log($"[GameInitializer] 已設定重玩後開始遊戲目標大場景 ID={bigSceneId}");

            RestartGame(targetSceneName);
        }

        /// <summary>
        /// 供 UICoverManager 在切換到 gamePanel（開始遊戲）時呼叫：
        /// 若先前有記住的大場景，則切換至該大場景並清除暫存。
        /// </summary>
        public static void ApplyPendingBigSceneIfAny()
        {
            if (!_pendingBigScene.hasTarget) return;

            var target = _pendingBigScene;
            _pendingBigScene = default;

            var roomUI = RoomUIManager.Instance != null
                ? RoomUIManager.Instance
                : FindFirstObjectByType<RoomUIManager>(FindObjectsInactive.Include);

            if (roomUI == null)
            {
                Debug.LogWarning("[GameInitializer] ApplyPendingBigSceneIfAny: 找不到 RoomUIManager。");
                return;
            }

            if (target.useId)
                roomUI.EnterBigSceneById(target.bigSceneId, target.roomIndex);
            else if (target.bigSceneIndex >= 0)
                roomUI.EnterBigScene(target.bigSceneIndex, target.roomIndex);
        }

        // ──────────────────────────────────────────────
        // 重載流程
        // ──────────────────────────────────────────────

        private IEnumerator RestartRoutine(string sceneToLoad)
        {
            _isRestarting = true;

            if (enableDebugLogs)
                Debug.Log($"[GameInitializer] 開始初始化並重玩遊戲，目標場景：{sceneToLoad}");

            // 1. 在清理前先擷取玩家目前的設定（音量、切換開關、攝影機晃動）
            CaptureSettingsSnapshot();

            // 2. 關閉對話框與所有彈出面板
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.ForceCloseDialogue();

            if (UIPanelManager.Instance != null)
                UIPanelManager.Instance.ForceCloseAllPanels();

            // 3. 若啟用黑幕淡出且場上有 UICoverManager 的 blackFadeImage，先淡入黑幕遮蔽畫面
            if (fadeOutBeforeReload)
                yield return StartCoroutine(FadeToBlackIfAvailable());

            // 4. 清理遊戲進度狀態（保留設定），並釋放 DontDestroyOnLoad 舊單例
            //    避免重載後新場景的 Manager 在 Awake 被誤刪，導致 UnityEvent 出現 Missing (Object)
            ClearGameProgress();
            GameStateManager.ReleaseForReload();
            SceneTransitionManager.ReleaseForReload();

            // 5. 確保時間縮放與全局旗標正常後重新載入場景
            Time.timeScale = 1f;
            UICoverManager.ResetGlobalFadeState();

            SceneManager.LoadScene(sceneToLoad);
        }

        private void ClearGameProgress()
        {
            // 清空跨場景遊戲狀態旗標
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.ClearAllStates();

            // 清空背包與握持區（跳過不需要初始化的物件）
            var inventories = FindObjectsByType<InventoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var inv in inventories)
            {
                if (inv == null || IsExcludedObject(inv.gameObject)) continue;
                inv.ClearAllSlots();
                inv.ClearHeldItem();
            }

            // 清除線索本存檔（不呼叫 PlayerPrefs.DeleteAll，避免誤刪其他設定；跳過不需要初始化的物件）
            var clueUnlockers = FindObjectsByType<ClueBookPageUnlocker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var unlocker in clueUnlockers)
            {
                if (unlocker != null && !IsExcludedObject(unlocker.gameObject))
                    unlocker.ClearAllSaveData();
            }
        }

        private void CaptureSettingsSnapshot()
        {
            var snap = new SettingsSnapshot
            {
                hasData = true,
                toggleStates = new Dictionary<string, bool>(),
                excludedObjectActiveStates = new Dictionary<string, bool>()
            };

            var cover = FindFirstObjectByType<UICoverManager>(FindObjectsInactive.Include);
            if (cover != null)
            {
                if (cover.musicSlider != null)
                {
                    snap.hasMusicSlider = true;
                    snap.musicVolume = cover.musicSlider.value;
                }
                if (cover.sfxSlider != null)
                {
                    snap.hasSfxSlider = true;
                    snap.sfxVolume = cover.sfxSlider.value;
                }
            }

            if (CameraFollowMouse.Instance != null)
            {
                snap.hasCameraSway = true;
                snap.cameraSwayActive = CameraFollowMouse.Instance.isSwaying;
            }

            var toggles = FindObjectsByType<SettingsToggleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in toggles)
            {
                if (t == null) continue;
                string key = GetHierarchyPath(t.transform);
                snap.toggleStates[key] = t.IsOn;
            }

            if (excludedObjects != null)
            {
                foreach (var obj in excludedObjects)
                {
                    if (obj == null) continue;
                    string key = GetHierarchyPath(obj.transform);
                    snap.excludedObjectActiveStates[key] = obj.activeSelf;
                }
            }

            _pendingSettings = snap;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _isRestarting = false;
            InitializeRuntimeDefaults();

            if (!_pendingSettings.hasData) return;

            RestoreSettingsSnapshot(_pendingSettings, scene);
            _pendingSettings = default;

            if (enableDebugLogs)
                Debug.Log($"[GameInitializer] 場景 {scene.name} 已重新載入，設定已還原並停在主選單。");
        }

        private void RestoreSettingsSnapshot(SettingsSnapshot snap, Scene scene)
        {
            var cover = FindFirstObjectByType<UICoverManager>(FindObjectsInactive.Include);
            if (cover != null)
            {
                if (snap.hasMusicSlider && cover.musicSlider != null)
                {
                    cover.musicSlider.SetValueWithoutNotify(snap.musicVolume);
                    cover.SetMusicVolume(snap.musicVolume);
                }
                if (snap.hasSfxSlider && cover.sfxSlider != null)
                {
                    cover.sfxSlider.SetValueWithoutNotify(snap.sfxVolume);
                    cover.SetSFXVolume(snap.sfxVolume);
                }

                // 確保停在主選單
                if (cover.mainMenuPanel != null) cover.mainMenuPanel.SetActive(true);
                if (cover.settingsPanel != null) cover.settingsPanel.SetActive(false);
                if (cover.gamePanel != null) cover.gamePanel.SetActive(false);
            }

            if (snap.hasCameraSway && CameraFollowMouse.Instance != null)
                CameraFollowMouse.Instance.SetSwayActive(snap.cameraSwayActive);

            if (snap.toggleStates != null)
            {
                var toggles = FindObjectsByType<SettingsToggleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var t in toggles)
                {
                    if (t == null) continue;
                    string key = GetHierarchyPath(t.transform);
                    if (snap.toggleStates.TryGetValue(key, out bool savedState))
                        t.SetStateImmediate(savedState);
                }
            }

            if (snap.excludedObjectActiveStates != null)
            {
                foreach (var kvp in snap.excludedObjectActiveStates)
                {
                    var obj = FindByHierarchyPath(scene, kvp.Key);
                    if (obj != null)
                        obj.SetActive(kvp.Value);
                }
            }
        }

        private bool IsExcludedObject(GameObject target)
        {
            if (target == null || excludedObjects == null || excludedObjects.Count == 0)
                return false;

            Transform targetTr = target.transform;
            foreach (var excluded in excludedObjects)
            {
                if (excluded == null) continue;
                if (targetTr == excluded.transform || targetTr.IsChildOf(excluded.transform))
                    return true;
            }
            return false;
        }

        private static GameObject FindByHierarchyPath(Scene scene, string path)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(path)) return null;

            string[] parts = path.Split('/');
            var roots = scene.GetRootGameObjects();
            Transform current = null;

            foreach (var root in roots)
            {
                if (root.name == parts[0])
                {
                    current = root.transform;
                    break;
                }
            }

            for (int i = 1; i < parts.Length && current != null; i++)
            {
                current = current.Find(parts[i]);
            }

            return current != null ? current.gameObject : null;
        }

        private IEnumerator FadeToBlackIfAvailable()
        {
            var cover = FindFirstObjectByType<UICoverManager>(FindObjectsInactive.Include);
            if (cover == null || cover.blackFadeImage == null) yield break;

            Image img = cover.blackFadeImage;
            img.gameObject.SetActive(true);

            var cg = img.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = true;
            img.raycastTarget = true;

            float duration = Mathf.Max(0.01f, cover.fadeDuration);
            float startAlpha = img.color.a;
            Color baseColor = img.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(startAlpha, 1f, Mathf.Clamp01(elapsed / duration));
                img.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }

            img.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        }

        private static string GetHierarchyPath(Transform tr)
        {
            var path = tr.name;
            while (tr.parent != null)
            {
                tr = tr.parent;
                path = tr.name + "/" + path;
            }
            return path;
        }
    }
}
