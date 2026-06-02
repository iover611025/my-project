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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (DialogueManager.Instance != null)
            {
                if (useFixedPosition)
                {
                    // 傳入 null，讓 Manager 使用預設座標
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