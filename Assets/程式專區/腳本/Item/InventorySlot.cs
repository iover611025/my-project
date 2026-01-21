using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace X
{
    [RequireComponent(typeof(Image))]
    public class InventorySlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image iconImage;
        [HideInInspector] public ItemData itemData;
        [HideInInspector] public int slotIndex;
        [HideInInspector] public InventoryUI owner;

        // root image（slot 背景）參考，用於避免不小心把根 image alpha 設為 0
        private Image _rootImage;

        void Reset()
        {
            // 嘗試自動找出子物件的 icon Image，避免誤用 slot 根節點的 Image（會導致整個slot被隱藏）
            var imgs = GetComponentsInChildren<Image>(true);
            _rootImage = GetComponent<Image>();
            iconImage = null;
            foreach (var img in imgs)
            {
                if (img == _rootImage) continue;
                iconImage = img;
                break;
            }
            if (iconImage == null)
                iconImage = _rootImage;
        }

        void Awake()
        {
            // 確保 root image 參考
            if (_rootImage == null)
                _rootImage = GetComponent<Image>();
            // 若 iconImage 未指派，嘗試 Reset 的邏輯一次
            if (iconImage == null)
                Reset();
        }

        public void SetIcon(Sprite icon)
        {
            if (iconImage == null) return;

            // 若 iconImage 是根節點（代表此 slot 沒有獨立的 icon 子物件），
            // 我們只改變 sprite，不把整個 Image 的 alpha 設為 0（避免隱藏背景）
            bool isRoot = iconImage == _rootImage;

            if (icon == null)
            {
                if (isRoot)
                {
                    // 清除 sprite，但保留顏色/alpha（背景仍可見）
                    iconImage.sprite = null;
                }
                else
                {
                    // 若有獨立 icon，使用 alpha 隱藏 icon
                    iconImage.sprite = null;
                    var c = iconImage.color;
                    iconImage.color = new Color(c.r, c.g, c.b, 0f);
                }
            }
            else
            {
                // 設定 sprite 並顯示
                iconImage.sprite = icon;
                var c = iconImage.color;
                iconImage.color = new Color(c.r, c.g, c.b, 1f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (owner == null) return;
            owner.OnSlotClicked(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (owner == null) return;
            owner.OnSlotBeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (owner == null) return;
            owner.OnSlotDrag(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (owner == null) return;
            owner.OnSlotEndDrag(this, eventData);
        }
    }
}