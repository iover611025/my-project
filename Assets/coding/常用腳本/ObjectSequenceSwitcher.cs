using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace X
{
    public class ObjectSequenceSwitcher : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        // 資料結構
        // ──────────────────────────────────────────────

        [System.Serializable]
        public class Sequence
        {
            [Tooltip("序列名稱（僅供編輯器識別用）")]
            public string sequenceName = "Sequence";

            [Tooltip("點擊此物件來切換到本序列；留空則只能透過 API 切換")]
            public GameObject triggerObject;

            [Tooltip("本序列的所有物件，由 SwitchNext/SwitchPrevious 在其中切換")]
            public List<GameObject> objects = new List<GameObject>();

            // 記住離開時的位置（不公開，不在 Inspector 顯示）
            [HideInInspector] public int savedIndex = 0;
        }

        // ──────────────────────────────────────────────
        // Inspector 設定
        // ──────────────────────────────────────────────

        [Header("所有序列（平等，皆可切換）")]
        [SerializeField] private List<Sequence> sequences = new List<Sequence>();

        [Header("起始序列索引（0 = 第一個）")]
        [SerializeField] private int startSequenceIndex = 0;

        // ──────────────────────────────────────────────
        // 執行期狀態
        // ──────────────────────────────────────────────

        private int activeSeqIndex = -1; // 當前使用中的序列索引

        // ──────────────────────────────────────────────
        // 生命週期
        // ──────────────────────────────────────────────

        private void Start()
        {
            HideAll();
            SetupTriggerListeners(); // 自動綁定觸發物件的點擊事件
            ActivateSequence(startSequenceIndex);
        }

        // ──────────────────────────────────────────────
        // 自動綁定點擊監聽
        // ──────────────────────────────────────────────

        /// <summary>
        /// 對每個序列的 triggerObject 自動掛上點擊監聽：
        ///   - 若有 Button 元件 → 加入 onClick
        ///   - 否則 → 加入 / 使用 EventTrigger (PointerClick)
        /// </summary>
        private void SetupTriggerListeners()
        {
            for (int i = 0; i < sequences.Count; i++)
            {
                int capturedIndex = i; // Lambda 捕獲需使用區域變數
                GameObject trigger = sequences[i].triggerObject;
                if (trigger == null) continue;

                // 優先使用 Button
                Button btn = trigger.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => ActivateSequence(capturedIndex));
                    continue;
                }

                // 沒有 Button → 使用 EventTrigger
                EventTrigger et = trigger.GetComponent<EventTrigger>();
                if (et == null) et = trigger.AddComponent<EventTrigger>();

                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                entry.callback.AddListener(_ => ActivateSequence(capturedIndex));
                et.triggers.Add(entry);
            }
        }

        // ──────────────────────────────────────────────
        // 序列內切換 API（在當前序列內移動）
        // ──────────────────────────────────────────────

        /// <summary>顯示當前序列的下一個物件（循環）</summary>
        public void SwitchNext()
        {
            if (!HasActiveSequence()) return;

            Sequence seq = sequences[activeSeqIndex];
            seq.savedIndex++;
            if (seq.savedIndex >= seq.objects.Count) seq.savedIndex = 0;
            UpdateSequenceVisibility(seq);
        }

        /// <summary>顯示當前序列的上一個物件（循環）</summary>
        public void SwitchPrevious()
        {
            if (!HasActiveSequence()) return;

            Sequence seq = sequences[activeSeqIndex];
            seq.savedIndex--;
            if (seq.savedIndex < 0) seq.savedIndex = seq.objects.Count - 1;
            UpdateSequenceVisibility(seq);
        }

        /// <summary>將當前序列重置回第一個物件</summary>
        public void ResetToFirst()
        {
            if (!HasActiveSequence()) return;

            Sequence seq = sequences[activeSeqIndex];
            seq.savedIndex = 0;
            UpdateSequenceVisibility(seq);
        }

        // ──────────────────────────────────────────────
        // 序列切換 API（在序列之間切換）
        // ──────────────────────────────────────────────

        /// <summary>
        /// 切換到指定索引的序列（0-based）。
        /// 若已在此序列，不做任何事。
        /// </summary>
        public void ActivateSequence(int index)
        {
            if (index < 0 || index >= sequences.Count) return;
            if (activeSeqIndex == index) return;

            // 隱藏目前序列的所有物件（savedIndex 已自動保存）
            if (activeSeqIndex >= 0)
                HideSequence(sequences[activeSeqIndex]);

            // 切換並顯示新序列當前物件
            activeSeqIndex = index;
            UpdateSequenceVisibility(sequences[activeSeqIndex]);
        }

        /// <summary>
        /// 透過觸發物件切換序列（手動 API 版本，自動綁定後通常不需要手動呼叫）。
        /// </summary>
        public void ActivateSequenceByTrigger(GameObject trigger)
        {
            for (int i = 0; i < sequences.Count; i++)
            {
                if (sequences[i].triggerObject == trigger)
                {
                    ActivateSequence(i);
                    return;
                }
            }
        }

        /// <summary>切換到下一個序列（循環）</summary>
        public void NextSequence()
        {
            if (sequences.Count == 0) return;
            int next = (activeSeqIndex + 1) % sequences.Count;
            ActivateSequence(next);
        }

        /// <summary>切換到上一個序列（循環）</summary>
        public void PreviousSequence()
        {
            if (sequences.Count == 0) return;
            int prev = (activeSeqIndex - 1 + sequences.Count) % sequences.Count;
            ActivateSequence(prev);
        }

        // ──────────────────────────────────────────────
        // 唯讀狀態查詢
        // ──────────────────────────────────────────────

        /// <summary>當前使用的序列索引（-1 代表無）</summary>
        public int ActiveSequenceIndex => activeSeqIndex;

        /// <summary>當前序列內的物件索引</summary>
        public int CurrentObjectIndex => HasActiveSequence() ? sequences[activeSeqIndex].savedIndex : -1;

        // ──────────────────────────────────────────────
        // 內部方法
        // ──────────────────────────────────────────────

        private bool HasActiveSequence()
        {
            return activeSeqIndex >= 0 && activeSeqIndex < sequences.Count
                   && sequences[activeSeqIndex].objects.Count > 0;
        }

        private void UpdateSequenceVisibility(Sequence seq)
        {
            for (int i = 0; i < seq.objects.Count; i++)
            {
                if (seq.objects[i] != null)
                    seq.objects[i].SetActive(i == seq.savedIndex);
            }
        }

        private void HideSequence(Sequence seq)
        {
            foreach (var obj in seq.objects)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        private void HideAll()
        {
            foreach (var seq in sequences)
                HideSequence(seq);
        }
    }
}
