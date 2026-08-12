using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace X
{
    [RequireComponent(typeof(Image))]
    public class InventorySlot : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler // 新增懸停介面
    {
        public Image iconImage;
        [Tooltip("（選填）顯示數量的 TextMeshPro 元件，不指定則不顯示數量")]
        public TextMeshProUGUI quantityText; // 數量標籤，如「x3」
        [HideInInspector] public ItemData itemData;
        [HideInInspector] public int quantity = 1; // 此格子存放的數量
        [HideInInspector] public int slotIndex;
        [HideInInspector] public InventoryUI owner;

        private Image _rootImage;

        // --- 新增：供 InventoryUI 檢查格子是否為空 ---
        public bool IsEmpty => itemData == null || itemData.id == 0;

        void Awake()
        {
            if (_rootImage == null) _rootImage = GetComponent<Image>();
            // 如果 iconImage 沒指派，預設使用自身的 Image
            if (iconImage == null) iconImage = _rootImage;
        }

        // --- 修正：這是 InventoryUI 真正呼叫的方法 ---
        public void UpdateSlot(Sprite icon, ItemData data = null, int qty = 1)
        {
            itemData = data;
            quantity = (data == null || data.id == 0) ? 0 : Mathf.Max(1, qty);

            if (icon == null)
            {
                iconImage.sprite = null;
                var c = iconImage.color;
                iconImage.color = new Color(c.r, c.g, c.b, 0f); // 隱藏圖示
            }
            else
            {
                iconImage.sprite = icon;
                var c = iconImage.color;
                iconImage.color = new Color(c.r, c.g, c.b, 1f); // 顯示圖示
            }

            // 更新數量標籤
            if (quantityText != null)
            {
                bool showQty = data != null && data.id != 0 && quantity > 1;
                quantityText.text = showQty ? $"x{quantity}" : "";
                quantityText.gameObject.SetActive(showQty);
            }
        }

        // --- 核心功能：滑鼠懸停顯示描述 ---
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 只有當格子有物品時才顯示
            if (!IsEmpty && !eventData.dragging)
            {
                if (InventoryTooltip.Instance != null)
                {
                    // 改用專屬的 InventoryTooltip
                    InventoryTooltip.Instance.Show(itemData.itemName, itemData.pickupDescription);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (InventoryTooltip.Instance != null)
            {
                InventoryTooltip.Instance.Hide();
            }
        }
        // 原有邏輯保持不變
        public void OnPointerClick(PointerEventData eventData) { if (owner != null) owner.OnSlotClicked(this); }
        public void OnBeginDrag(PointerEventData eventData) { if (owner != null) owner.OnSlotBeginDrag(this, eventData); }
        public void OnDrag(PointerEventData eventData) { if (owner != null) owner.OnSlotDrag(this, eventData); }
        public void OnEndDrag(PointerEventData eventData) { if (owner != null) owner.OnSlotEndDrag(this, eventData); }
    }
}