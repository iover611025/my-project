using System;
using System.Collections.Generic;
using UnityEngine;

namespace X
{
    public class GameStateManager : MonoBehaviour
    {
        private static GameStateManager _instance;

        // 公開存取點
        public static GameStateManager Instance
        {
            get
            {
                // 如果在呼叫時 Instance 是空的，嘗試在場景中找找看
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<GameStateManager>();
                }
                return _instance;
            }
        }

        private HashSet<string> _unlockedStates = new HashSet<string>();
        public event Action<string> OnStateAdded;

        private void Awake()
        {
            // --- 核心防錯邏輯 ---

            // 1. 檢查是否有父物件：有的話 DontDestroyOnLoad 會失效
            if (transform.parent != null)
            {
                Debug.LogWarning($"[GameStateManager] 警告：此物件有父物件 {transform.parent.name}，將解除父子關係以確保 DontDestroyOnLoad 運作。");
                transform.SetParent(null);
            }

            // 2. 單例唯一性檢查
            if (_instance != null && _instance != this)
            {
                Debug.Log($"[GameStateManager] 偵測到重複的 Manager 在 {gameObject.name}，正在刪除重複項。");
                Destroy(gameObject);
                return;
            }

            // 3. 正式確立 Instance
            _instance = this;

            // 4. 跨場景保留
            DontDestroyOnLoad(gameObject);

            Debug.Log("[GameStateManager] 初始化成功並已鎖定跨場景保留。");
        }

        public void AddState(string stateName)
        {
            if (_unlockedStates.Add(stateName))
            {
                Debug.Log($"[GameStateManager] 狀態更新：{stateName}");
                OnStateAdded?.Invoke(stateName);
            }
        }

        public bool HasState(string stateName) => _unlockedStates.Contains(stateName);

        public bool HasAllStates(List<string> requiredStates)
        {
            if (requiredStates == null || requiredStates.Count == 0) return true;
            foreach (var s in requiredStates)
            {
                if (!_unlockedStates.Contains(s)) return false;
            }
            return true;
        }

        /// <summary>
        /// 清空所有已解鎖的遊戲狀態（用於重新開始／重玩遊戲）。
        /// </summary>
        public void ClearAllStates()
        {
            _unlockedStates.Clear();
            Debug.Log("[GameStateManager] 已清空所有遊戲狀態。");
        }

        /// <summary>
        /// 場景重載前釋放 DontDestroyOnLoad 舊實例，避免新場景的實例在 Awake 被當成重複項刪除，
        /// 導致新場景內 UnityEvent 綁定出現 Missing (Object)。
        /// </summary>
        public static void ReleaseForReload()
        {
            if (_instance != null)
            {
                var go = _instance.gameObject;
                _instance = null;
                Destroy(go);
            }
        }
    }
}