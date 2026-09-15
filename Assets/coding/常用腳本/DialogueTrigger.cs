using UnityEngine;
using UnityEngine.EventSystems;

namespace X
{
    public class UIDialogueTrigger : MonoBehaviour, IPointerClickHandler
    {
        [Header("對話設定")]
        [TextArea(3, 5)]
        public string content = "在這裡輸入對話文字...";

        [Range(0f, 8f)]
        public float displayDuration = 2.5f;

        [Header("位置設定")]
        public bool useFixedPosition = true; // 勾選則使用固定位置

        [Tooltip("若不使用固定位置，則使用此座標")]
        public Vector2 targetPosition = new Vector2(0, -350);

        [Header("觸發設定")]
        public bool triggerOnEnable = false; // 勾選則在物件啟用時自動觸發

        private bool _hasTriggeredOnEnable = false;
        private bool _disabled = false; // 被 DisableDialogue() 永久關閉後不再觸發

        private void OnEnable()
        {
            _hasTriggeredOnEnable = false;
            if (triggerOnEnable)
            {
                TriggerDialogue();
            }
        }

        private void Start()
        {
            if (triggerOnEnable && !_hasTriggeredOnEnable)
            {
                TriggerDialogue();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            TriggerDialogue();
        }

        /// <summary>
        /// 永久停用此對話框。
        /// 可接在 PickupableItem.onPickedUp 事件上：物品被拾取後，對話框不再觸發。
        /// </summary>
        public void DisableDialogue()
        {
            _disabled = true;
        }

        /// <summary>
        /// 讓外部腳本（例如 PickupableItem）在同一幀點擊時，
        /// 壓制這次的對話觸發。需在此元件的 OnPointerClick 被呼叫前設定。
        /// </summary>
        public void SuppressNextClick()
        {
            _suppressThisClick = true;
        }

        private bool _suppressThisClick = false;

        private void TriggerDialogue()
        {
            // 已被永久停用（例如物品已拾取）
            if (_disabled) return;

            // 被外部同幀壓制
            if (_suppressThisClick)
            {
                _suppressThisClick = false;
                return;
            }

            if (DialogueManager.Instance != null)
            {
                _hasTriggeredOnEnable = true;
                if (useFixedPosition)
                {
                    DialogueManager.Instance.ShowDialogue(content, displayDuration, null);
                }
                else
                {
                    DialogueManager.Instance.ShowDialogue(content, displayDuration, targetPosition);
                }
            }
        }
    }
}