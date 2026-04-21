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
        [Tooltip("相對於 Canvas 中心的位置 (X, Y)")]
        public Vector2 targetPosition = new Vector2(0, -350);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue(content, displayDuration, targetPosition);
            }
        }
    }
}