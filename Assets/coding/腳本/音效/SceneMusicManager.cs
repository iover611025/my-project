using System;
using System.Collections.Generic;
using UnityEngine;

namespace X
{
    /// <summary>
    /// 每個大場景對應的音效 GameObject 設定
    /// </summary>
    [Serializable]
    public class BigSceneAudioConfig
    {
        [Tooltip("對應 BigScene 的 id（與 RoomUIManager.bigScenes[i].id 一致）")]
        public int bigSceneId;

        [Tooltip("該大場景啟用的音效物件（可放多個，例如 BGM、Ambience 各一個 GO）")]
        public GameObject[] audioObjects;
    }

    /// <summary>
    /// 依據目前大場景 id 切換音效 GameObject（開/關）。
    /// 本腳本只負責「什麼時候切換」，音樂播放邏輯由各 GameObject 自行處理。
    ///
    /// ✅ 使用方式：
    ///   1. 掛到場景中任一 GameObject
    ///   2. Inspector 拖入 RoomUIManager
    ///   3. 在 audioConfigs 裡依 bigSceneId 填入要啟用的音效 GameObject
    ///   4. 切換大場景後呼叫 OnBigSceneChanged(bigSceneId)
    /// </summary>
    public class SceneMusicManager : MonoBehaviour
    {
        [Header("參照")]
        public RoomUIManager roomUIManager;

        [Header("各大場景音效 GameObject 設定")]
        public List<BigSceneAudioConfig> audioConfigs = new List<BigSceneAudioConfig>();

        [Header("除錯")]
        public bool enableDebugLogs = true;

        private int _currentBigSceneId = -999;

        public static SceneMusicManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            if (roomUIManager == null)
                roomUIManager = FindObjectOfType<RoomUIManager>();

            if (roomUIManager == null)
                Debug.LogWarning("[SceneMusicManager] 找不到 RoomUIManager，請手動拖入。");
        }

        private void Start()
        {
            int initId = GetCurrentBigSceneId();
            if (enableDebugLogs)
                Debug.Log($"[SceneMusicManager] Start → 初始大場景 id = {initId}");

            SwitchAudioObjects(initId);
            _currentBigSceneId = initId;
        }

        private void Update()
        {
            // 每幀自動偵測大場景 id 是否改變，改變時自動切換音效 GO
            int currentId = GetCurrentBigSceneId();
            if (currentId != _currentBigSceneId)
            {
                if (enableDebugLogs)
                    Debug.Log($"[SceneMusicManager] 偵測到大場景變更：{_currentBigSceneId} → {currentId}");
                _currentBigSceneId = currentId;
                SwitchAudioObjects(currentId);
            }
        }

        // ─── 公開 API ─────────────────────────────────────────────────

        /// <summary>
        /// 切換大場景後呼叫此方法，傳入新的 bigSceneId。
        /// 若 id 相同則不重複切換。
        /// </summary>
        public void OnBigSceneChanged(int bigSceneId)
        {
            if (bigSceneId == _currentBigSceneId)
            {
                if (enableDebugLogs)
                    Debug.Log($"[SceneMusicManager] id={bigSceneId} 與目前相同，跳過切換。");
                return;
            }

            if (enableDebugLogs)
                Debug.Log($"[SceneMusicManager] 切換 {_currentBigSceneId} → {bigSceneId}");

            _currentBigSceneId = bigSceneId;
            SwitchAudioObjects(bigSceneId);
        }

        /// <summary>
        /// 手動同步（依 RoomUIManager 當前狀態重新讀取並切換）
        /// </summary>
        public void SyncFromRoomUIManager()
        {
            OnBigSceneChanged(GetCurrentBigSceneId());
        }

        // ─── 內部邏輯 ─────────────────────────────────────────────────

        /// <summary>
        /// 關閉所有音效 GO，只開啟目標 bigSceneId 對應的 GO
        /// </summary>
        private void SwitchAudioObjects(int bigSceneId)
        {
            foreach (var cfg in audioConfigs)
            {
                if (cfg == null || cfg.audioObjects == null) continue;

                bool isTarget = cfg.bigSceneId == bigSceneId;
                foreach (var go in cfg.audioObjects)
                {
                    if (go == null) continue;
                    if (go.activeSelf != isTarget)
                    {
                        go.SetActive(isTarget);
                        if (enableDebugLogs)
                            Debug.Log($"[SceneMusicManager] '{go.name}' → {(isTarget ? "開啟" : "關閉")}");
                    }
                }
            }
        }

        /// <summary>
        /// 透過 Reflection 讀取 RoomUIManager 私有的 currentBigSceneIndex，
        /// 再換算成 bigScene.id
        /// </summary>
        private int GetCurrentBigSceneId()
        {
            if (roomUIManager == null) return -1;

            var field = typeof(RoomUIManager).GetField(
                "currentBigSceneIndex",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field == null)
            {
                Debug.LogWarning("[SceneMusicManager] 找不到 currentBigSceneIndex 欄位。");
                return -1;
            }

            int idx = (int)field.GetValue(roomUIManager);
            var scenes = roomUIManager.bigScenes;
            if (scenes == null || idx < 0 || idx >= scenes.Count) return -1;

            return scenes[idx]?.id ?? -1;
        }

#if UNITY_EDITOR
        [ContextMenu("測試：同步目前大場景音效")]
        private void Editor_SyncNow() => SyncFromRoomUIManager();
#endif
    }
}
