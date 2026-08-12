using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace X
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Slots (填入 InventorySlot 元件)")]
        public List<InventorySlot> slots = new List<InventorySlot>();

        [Header("握持區 UI Image")]
        public Image heldItemImage;

        [Header("拖放用 Canvas（必填）")]
        public Canvas canvas;

        [Header("展開 / 旋轉設定")]
        public RectTransform panelToMove;
        public float moveDistanceX = -200f;
        public float moveDuration = 0.25f;

        public RectTransform expandToggleUI;
        public bool rotateToggleOnExpand = true;

        // internal
        private Sprite defaultHeldItemSprite;
        private Image _dragIcon;
        private InventorySlot _dragSource;
        private ItemData _heldItemData;

        private bool _isExpanded = false;
        private bool _isAnimating = false;
        private Vector2 _originalAnchoredPos;
        private Vector3 _toggleOriginalEuler;

        void Awake()
        {
            if (heldItemImage != null)
            {
                defaultHeldItemSprite = heldItemImage.sprite;
                if (defaultHeldItemSprite == null)
                {
                    heldItemImage.enabled = false;
                    var c = heldItemImage.color;
                    heldItemImage.color = new Color(c.r, c.g, c.b, 0f);
                }
            }

            if (slots == null || slots.Count == 0)
            {
                var found = GetComponentsInChildren<InventorySlot>(true);
                if (found != null && found.Length > 0)
                    slots = new List<InventorySlot>(found);
            }

            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s == null) continue;
                s.slotIndex = i;
                s.owner = this;
            }

            if (panelToMove == null) panelToMove = GetComponent<RectTransform>();
            if (panelToMove != null) _originalAnchoredPos = panelToMove.anchoredPosition;
            if (expandToggleUI != null) _toggleOriginalEuler = expandToggleUI.localEulerAngles;
        }

        public bool IsHeldEmpty()
        {
            if (heldItemImage == null || !heldItemImage.enabled || _heldItemData == null || _heldItemData.id == 0) return true;
            return false;
        }

        public ItemData GetHeldItemData() => _heldItemData;

        [ContextMenu("ClearAllSlots")]
        public void ClearAllSlots()
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null) continue;
                // 修正 1: 傳入兩個 null 清空格子
                slots[i].UpdateSlot(null, null);
            }
        }

        private List<InventorySlot> GetAssignmentOrder()
        {
            if (slots == null) return new List<InventorySlot>();
            Regex r = new Regex(@"^(\d+)[-_](\d+)$");
            var withKey = new List<(InventorySlot slot, int a, int b)>();
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s == null) continue;
                var m = r.Match(s.gameObject.name.Trim());
                if (m.Success) withKey.Add((s, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)));
                else withKey.Add((s, int.MaxValue, i));
            }
            return withKey.OrderBy(x => x.a).ThenBy(x => x.b).Select(x => x.slot).ToList();
        }

        public void ToggleExpand() { if (!_isAnimating) StartCoroutine(AnimateExpand(!_isExpanded)); }

        private IEnumerator AnimateExpand(bool expand)
        {
            if (panelToMove == null) yield break; // 防呆機制
            _isAnimating = true;

            // --- 1. 計算位移目標 ---
            Vector2 startPos = panelToMove.anchoredPosition;
            Vector2 targetPos = _originalAnchoredPos + (expand ? new Vector2(moveDistanceX, 0f) : Vector2.zero);

            // --- 2. 計算旋轉目標 ---
            // 這裡假設展開時旋轉 -90 度（向左旋轉），收合時回到 0 度
            float startRotation = expandToggleUI != null ? expandToggleUI.localEulerAngles.z : 0f;
            float targetRotation = expand && rotateToggleOnExpand ? 90f : 0f;

            float elapsed = 0f;
            float dur = Mathf.Max(0.001f, moveDuration); // 避免除以 0

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime; // 使用 unscaledDeltaTime 確保暫停時 UI 仍能運作
                float t = Mathf.Clamp01(elapsed / dur);

                // 使用 SmoothStep 讓動畫具備加速與減速的平滑感（加法邏輯）
                float curvedT = Mathf.SmoothStep(0f, 1f, t);

                // 更新位置
                panelToMove.anchoredPosition = Vector2.Lerp(startPos, targetPos, curvedT);

                // 更新旋轉 (關鍵修正點)
                if (expandToggleUI != null && rotateToggleOnExpand)
                {
                    // 使用 Mathf.LerpAngle 處理角度插值，確保旋轉方向正確
                    float currentZ = Mathf.LerpAngle(startRotation, targetRotation, curvedT);
                    expandToggleUI.localEulerAngles = new Vector3(0, 0, currentZ);
                }

                yield return null;
            }

            // 確保最後數值精確
            panelToMove.anchoredPosition = targetPos;
            if (expandToggleUI != null && rotateToggleOnExpand)
                expandToggleUI.localEulerAngles = new Vector3(0, 0, targetRotation);

            _isExpanded = expand;
            _isAnimating = false;
        }

        public void SetHeldItem(Sprite icon, ItemData data = null)
        {
            if (heldItemImage == null) return;
            _heldItemData = data;
            heldItemImage.sprite = icon;
            heldItemImage.enabled = icon != null;
            heldItemImage.color = new Color(1, 1, 1, icon != null ? 1 : 0);
        }

        public void ClearHeldItem() => SetHeldItem(null, null);

        public void OnHeldAreaClick()
        {
            if (IsHeldEmpty()) { ClearHeldItem(); return; }
            if (_heldItemData != null && TryAddToFirstEmpty(_heldItemData)) ClearHeldItem();
        }

        public bool AddItemToSlot(ItemData itemData, int qty = 1)
        {
            if (itemData == null) return false;
            if (TryAddToFirstEmpty(itemData, qty)) return true;
            if (IsHeldEmpty()) { SetHeldItem(itemData.icon, itemData); return true; }
            return false;
        }

        private bool TryAddToFirstEmpty(ItemData itemData, int qty = 1)
        {
            var ordered = GetAssignmentOrder();
            foreach (var s in ordered)
            {
                if (s != null && s.IsEmpty)
                {
                    // 傳入 icon、data 與數量
                    s.UpdateSlot(itemData.icon, itemData, qty);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 預先檢查背包（格子 + 握持區）是否有足夠的空格可放入 slotCount 筆物品。
        /// 用於多物品一次拾取時，確保全部能放入再執行。
        /// </summary>
        public bool HasEnoughSpace(int slotCount)
        {
            if (slotCount <= 0) return true;
            int emptyCount = 0;
            if (slots != null)
            {
                foreach (var s in slots)
                    if (s != null && s.IsEmpty) emptyCount++;
            }
            if (IsHeldEmpty()) emptyCount++; // 握持區也算一個空位
            return emptyCount >= slotCount;
        }

        public void OnSlotClicked(InventorySlot slot)
        {
            if (slot == null) return;

            if (!IsHeldEmpty())
            {
                ItemData temp = slot.itemData;
                // 修正 3: 交換物品時補齊參數
                slot.UpdateSlot(_heldItemData.icon, _heldItemData);
                SetHeldItem(temp?.icon, temp);
            }
            else if (!slot.IsEmpty)
            {
                SetHeldItem(slot.itemData.icon, slot.itemData);
                // 修正 4: 拿起物品後清空格子
                slot.UpdateSlot(null, null);
            }
        }

        public void OnSlotBeginDrag(InventorySlot slot, PointerEventData eventData)
        {
            if (slot == null || slot.IsEmpty || canvas == null) return;
            _dragSource = slot;
            CreateDragIcon(slot.iconImage.sprite);
        }

        public void OnSlotDrag(InventorySlot slot, PointerEventData eventData)
        {
            if (_dragIcon == null) return;
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out pos);
            _dragIcon.rectTransform.anchoredPosition = pos;
        }

        public void OnSlotEndDrag(InventorySlot slot, PointerEventData eventData)
        {
            if (_dragSource == null) { CleanupDrag(); return; }

            GameObject hit = eventData.pointerCurrentRaycast.gameObject;
            InventorySlot targetSlot = hit?.GetComponentInParent<InventorySlot>();

            if (targetSlot != null && targetSlot != _dragSource)
            {
                ItemData srcData = _dragSource.itemData;
                ItemData tgtData = targetSlot.itemData;

                // 修正 5 & 6: 拖放交換
                targetSlot.UpdateSlot(srcData?.icon, srcData);
                _dragSource.UpdateSlot(tgtData?.icon, tgtData);
            }
            else if (heldItemImage != null && RectTransformUtility.RectangleContainsScreenPoint(heldItemImage.rectTransform, eventData.position, canvas.worldCamera))
            {
                SetHeldItem(_dragSource.itemData.icon, _dragSource.itemData);
                // 修正 7: 放入握持區後清空原格子
                _dragSource.UpdateSlot(null, null);
            }
            CleanupDrag();
        }

        private void CreateDragIcon(Sprite sprite)
        {
            GameObject go = new GameObject("DragIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            _dragIcon = go.GetComponent<Image>();
            _dragIcon.sprite = sprite;
            _dragIcon.raycastTarget = false;
            _dragIcon.rectTransform.sizeDelta = new Vector2(64, 64);
        }

        private void CleanupDrag() { if (_dragIcon != null) Destroy(_dragIcon.gameObject); _dragSource = null; }
    }
}