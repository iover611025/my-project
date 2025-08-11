using UnityEngine;
using UnityEngine.UI;

namespace X
{
    public class InventoryToggleUI_Left : MonoBehaviour
    {
        public RectTransform inventoryPanel;
        public RectTransform triangleButton;
        public float expandedX = 0f;
        public float collapsedX = 300f;
        public float animTime = 0.2f;

        private bool expanded = false;
        private float animProgress = 1f;
        private float animStartX, animTargetX;
        private float animStartRot, animTargetRot;

        void Start()
        {
            // 初始三角形旋轉 -90 度
            triangleButton.localEulerAngles = new Vector3(0, 0, -90);
            // 初始狀態收回
            inventoryPanel.anchoredPosition = new Vector2(collapsedX, inventoryPanel.anchoredPosition.y);

            triangleButton.GetComponent<Button>().onClick.AddListener(ToggleInventory);
        }

        void ToggleInventory()
        {
            expanded = !expanded;
            animProgress = 0f;
            animStartX = inventoryPanel.anchoredPosition.x;
            animTargetX = expanded ? expandedX : collapsedX;
            animStartRot = triangleButton.localEulerAngles.z;
            animTargetRot = expanded ? 0f : -90f; // 展開時 0, 收回時 -90
        }

        void Update()
        {
            if (animProgress < 1f)
            {
                animProgress += Time.unscaledDeltaTime / animTime;
                if (animProgress > 1f) animProgress = 1f;

                // X 位置動畫
                float x = Mathf.Lerp(animStartX, animTargetX, animProgress);
                inventoryPanel.anchoredPosition = new Vector2(x, inventoryPanel.anchoredPosition.y);

                // 三角形旋轉動畫
                float rot = Mathf.LerpAngle(animStartRot, animTargetRot, animProgress);
                triangleButton.localEulerAngles = new Vector3(0, 0, rot);
            }
        }
    }
}