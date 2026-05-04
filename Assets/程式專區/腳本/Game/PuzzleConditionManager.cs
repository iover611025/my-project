using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace X
{
    public class PuzzleConditionManager : MonoBehaviour
    {
        [System.Serializable]
        public class ConditionGroup
        {
            public string description;        // 註解用（例如：花開且蝴蝶在）
            public List<GameObject> requiredActiveObjects; // 這些物件必須是 Active
            public UnityEvent onConditionsMet; // 達成後的事件
            [HideInInspector] public bool isTriggered = false;
        }

        [Header("解謎條件清單")]
        public List<ConditionGroup> conditions;

        [Header("掃描頻率")]
        public float checkInterval = 0.2f;

        void Start()
        {
            // 使用 InvokeRepeating 減少每幀 Update 的效能消耗
            InvokeRepeating(nameof(CheckAllConditions), 1f, checkInterval);
        }

        private void CheckAllConditions()
        {
            foreach (var group in conditions)
            {
                if (group.isTriggered) continue;

                if (AreAllObjectsActive(group.requiredActiveObjects))
                {
                    group.isTriggered = true;
                    Debug.Log($"[Manager] 條件達成: {group.description}");
                    group.onConditionsMet?.Invoke();
                }
            }
        }

        private bool AreAllObjectsActive(List<GameObject> objs)
        {
            if (objs == null || objs.Count == 0) return false;

            foreach (var obj in objs)
            {
                // 只要清單中有一個物件是隱藏的，條件就不成立
                if (obj == null || !obj.activeInHierarchy) return false;
            }
            return true;
        }

        // 外部手動呼叫重置（選用）
        public void ResetCondition(int index)
        {
            if (index < conditions.Count) conditions[index].isTriggered = false;
        }
    }
}