using UnityEngine;

using System.Collections.Generic;

namespace X
{
    public class BigSceneManager : MonoBehaviour
    {
        public static BigSceneManager Instance;

        [Header("大場景設定")]
        public List<BigScene> bigScenes = new List<BigScene>();

        [Header("目前狀態")]
        public int currentSceneId;
        public int currentRoomIndex;

        [Header("轉場 (可選)")]
        public UICoverManager uiCoverManager;

        void Awake() { Instance = this; }

        // 只需在 Inspector 拖入 BigScene，即可呼叫此方法切換
        public void SwitchTo(int sceneId, int roomIndex)
        {
            BigScene target = bigScenes.Find(s => s.id == sceneId);
            if (target == null) return;

            currentSceneId = sceneId;
            currentRoomIndex = roomIndex;

            // 1. 管理 UI Panel (RectTransform)
            if (target.roomPanels != null)
            {
                for (int i = 0; i < target.roomPanels.Length; i++)
                {
                    if (target.roomPanels[i] != null)
                        target.roomPanels[i].gameObject.SetActive(i == roomIndex);
                }
            }

            // 2. 如果你有世界物件 (原本 RoomManager 管的)，也可以直接在這裡處理
            // 你可以在 BigScene 類別中多加一個 GameObject[] worldObjects 欄位

            Debug.Log($"已切換至場景 {sceneId} 的第 {roomIndex} 個房間");
        }
    }
}