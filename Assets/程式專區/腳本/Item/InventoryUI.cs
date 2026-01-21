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
        public Image heldItemImage; // 握持區 UI Image

        [Header("拖放用 Canvas（必填）")]
        public Canvas canvas;

        [Header("展開 / 旋轉設定")]
        [Tooltip("要沿 X 軸平移的 RectTransform（若留空會自動使用本物件的 RectTransform）")]
        public RectTransform panelToMove;
        [Tooltip("展開時沿 X 軸移動的距離（負為向左）")]
        public float moveDistanceX = -200f; // 預設向左移動
        [Tooltip("展開/收合 動畫耗時（秒）")]
        public float moveDuration = 0.25f;

        [Tooltip("點擊用的 Toggle UI（會在展開/收合時旋轉）")]
        public RectTransform expandToggleUI;
        [Tooltip("是否在切換時讓 expandToggleUI 旋轉 90 度（向左為 -90）")]
        public bool rotateToggleOnExpand = true;

        // internal
        private Sprite defaultHeldItemSprite;
        private Image _dragIcon;
        private InventorySlot _dragSource;
        private ItemData _heldItemData;

        // expand state
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

            // 若 inspector 未填 slots，嘗試自動抓取子節點上的 InventorySlot
            if (slots == null || slots.Count == 0)
            {
                var found = GetComponentsInChildren<InventorySlot>(true);
                if (found != null && found.Length > 0)
                    slots = new List<InventorySlot>(found);
            }

            // 註冊 slots owner（無論 inspector 或自動填充）
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s == null) continue;
                s.slotIndex = i;
                s.owner = this;
            }

            // Panel setup
            if (panelToMove == null)
            {
                panelToMove = GetComponent<RectTransform>();
            }
            if (panelToMove != null)
            {
                _originalAnchoredPos = panelToMove.anchoredPosition;
            }

            // Toggle UI 原始旋轉記錄
            if (expandToggleUI != null)
            {
                _toggleOriginalEuler = expandToggleUI.localEulerAngles;
            }

            // 快速檢查：避免 expandToggleUI 被誤指為握持區（或同一個 GameObject）
            if (expandToggleUI != null && heldItemImage != null)
            {
                if (expandToggleUI.gameObject == heldItemImage.gameObject)
                {
                    Debug.LogWarning("[InventoryUI] expandToggleUI 與 heldItemImage 指向同一個 GameObject！請在 Inspector 指派專用的展開按鈕 UI（expandToggleUI）與握持區（heldItemImage）為不同物件。");
                }
            }

            // 初始列印 slots 狀態，協助定位「為何啟動時都滿格」
            Debug.Log($"[InventoryUI] Awake: slots.Count={(slots!=null?slots.Count:0)}");
            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    if (s == null)
                    {
                        Debug.Log($"  slot[{i}] = null");
                        continue;
                    }
                    var d = s.itemData;
                    Debug.Log($"  slot[{i}] name={s.gameObject.name} itemData={(d!=null? d.itemName + $"(id={d.id})" : "null")}");
                }
            }

            // 列印目前分配順序（依名稱解析）
            Debug.Log("[InventoryUI] Assignment order on Awake:");
            var ord = GetAssignmentOrder();
            for (int i = 0; i < ord.Count; i++)
            {
                var s = ord[i];
                Debug.Log($"  order[{i}] = {(s!=null? s.gameObject.name : "null")}");
            }
        }

        // 稱為給外部檢查握持區是否為空（RequireHeldItemToOpen 等會用到）
        public bool IsHeldEmpty()
        {
            if (heldItemImage == null) return true;
            if (!heldItemImage.enabled) return true;
            if (heldItemImage.sprite == null) return true;
            if (heldItemImage.color.a <= 0.01f) return true;
            if (_heldItemData == null) return true;
            return false;
        }

        // 可在 Inspector 右鍵呼叫：列印目前 slot 詳細狀態
        [ContextMenu("PrintSlotsState")]
        public void PrintSlotsState()
        {
            Debug.Log($"[InventoryUI] PrintSlotsState: slots.Count={(slots!=null?slots.Count:0)}");
            if (slots == null) return;
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s == null)
                {
                    Debug.Log($"  slot[{i}] = null");
                    continue;
                }
                var d = s.itemData;
                Debug.Log($"  slot[{i}] name={s.gameObject.name} itemData={(d!=null? d.itemName + $"(id={d.id})" : "null")}");
            }
        }

        // 可在 Inspector 右鍵呼叫：清除所有 slot（僅測試用）
        [ContextMenu("ClearAllSlots")]
        public void ClearAllSlots()
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s == null) continue;
                s.itemData = null;
                s.SetIcon(null);
            }
            Debug.Log("[InventoryUI] ClearAllSlots: cleared all slot.itemData");
        }

        // 可在 Inspector 右鍵呼叫：列印分配順序
        [ContextMenu("PrintAssignmentOrder")]
        public void PrintAssignmentOrder()
        {
            var ord = GetAssignmentOrder();
            Debug.Log($"[InventoryUI] PrintAssignmentOrder count={ord.Count}");
            for (int i = 0; i < ord.Count; i++)
                Debug.Log($"  {i}: {(ord[i]!=null? ord[i].gameObject.name : "null")}");
        }

        // 取得對應你要的分配順序：解析 slot 名稱 "X-Y"（例如 "1-3"），先按 X 再按 Y
        private List<InventorySlot> GetAssignmentOrder()
        {
            if (slots == null) return new List<InventorySlot>();
            Regex r = new Regex(@"^(\d+)[-_](\d+)$");
            var withKey = new List<(InventorySlot slot, int a, int b, bool ok)>();
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s == null)
                {
                    withKey.Add((s, int.MaxValue, int.MaxValue, false));
                    continue;
                }
                var name = s.gameObject.name.Trim();
                var m = r.Match(name);
                if (m.Success)
                {
                    int a = int.Parse(m.Groups[1].Value);
                    int b = int.Parse(m.Groups[2].Value);
                    withKey.Add((s, a, b, true));
                }
                else
                {
                    // 若名稱不符合，給最大排序值但保留穩定順序（放後面）
                    withKey.Add((s, int.MaxValue, i, false));
                }
            }

            var ordered = withKey.OrderBy(x => x.a).ThenBy(x => x.b).Select(x => x.slot).ToList();
            return ordered;
        }

        // 公開：切換展開/收合（可以綁到 Button.OnClick）
        public void ToggleExpand()
        {
            if (panelToMove == null) return;
            if (_isAnimating) return;
            StartCoroutine(AnimateExpand(!_isExpanded));
        }

        // 公開：直接設定展開狀態（instant=true 立即切換）
        public void SetExpanded(bool expand, bool instant = false)
        {
            if (panelToMove == null) return;
            if (_isAnimating) return;
            if (instant)
            {
                _isExpanded = expand;
                var targetPos = _originalAnchoredPos + (expand ? new Vector2(moveDistanceX, 0f) : Vector2.zero);
                panelToMove.anchoredPosition = targetPos;
                if (expandToggleUI != null && rotateToggleOnExpand)
                {
                    // 展開時向左旋轉 -90，收合還原
                    var rot = _toggleOriginalEuler + (expand ? new Vector3(0f, 0f, 0f) : new Vector3(0f,0f,-90f));
                    expandToggleUI.localEulerAngles = rot;
                }
            }
            else
            {
                StartCoroutine(AnimateExpand(expand));
            }
        }

        private IEnumerator AnimateExpand(bool expand)
        {
            if (panelToMove == null) yield break;
            _isAnimating = true;

            Vector2 startPos = panelToMove.anchoredPosition;
            Vector2 targetPos = _originalAnchoredPos + (expand ? new Vector2(moveDistanceX, 0f) : Vector2.zero);

            Vector3 startToggleEuler = expandToggleUI != null ? expandToggleUI.localEulerAngles : Vector3.zero;
            Vector3 targetToggleEuler = _toggleOriginalEuler + (expand && rotateToggleOnExpand ? new Vector3(0f, 0f, -90f) : Vector3.zero);

            float elapsed = 0f;
            float dur = Mathf.Max(0.0001f, moveDuration);

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                // smooth step for nicer motion
                float s = Mathf.SmoothStep(0f, 1f, t);
                panelToMove.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, s);
                if (expandToggleUI != null && rotateToggleOnExpand)
                    expandToggleUI.localEulerAngles = Vector3.LerpUnclamped(startToggleEuler, targetToggleEuler, s);
                yield return null;
            }

            panelToMove.anchoredPosition = targetPos;
            if (expandToggleUI != null && rotateToggleOnExpand)
                expandToggleUI.localEulerAngles = targetToggleEuler;

            _isExpanded = expand;
            _isAnimating = false;
        }

        // 將某道具圖示設定為手持（外部可呼叫）
        public void SetHeldItem(Sprite icon, ItemData data = null)
        {
            if (heldItemImage == null) return;
            heldItemImage.sprite = icon;
            _heldItemData = data;
            if (icon == null)
            {
                heldItemImage.enabled = false;
                var c = heldItemImage.color;
                heldItemImage.color = new Color(c.r, c.g, c.b, 0f);
            }
            else
            {
                heldItemImage.enabled = true;
                var c = heldItemImage.color;
                heldItemImage.color = new Color(c.r, c.g, c.b, 1f);
            }
        }

        // 清除手持
        public void ClearHeldItem()
        {
            if (heldItemImage == null) return;
            if (defaultHeldItemSprite != null)
            {
                heldItemImage.sprite = defaultHeldItemSprite;
                heldItemImage.enabled = true;
                var c = heldItemImage.color;
                heldItemImage.color = new Color(c.r, c.g, c.b, 1f);
                _heldItemData = null;
            }
            else
            {
                heldItemImage.sprite = null;
                heldItemImage.enabled = false;
                var c = heldItemImage.color;
                heldItemImage.color = new Color(c.r, c.g, c.b, 0f);
                _heldItemData = null;
            }
        }

        // pickup.cs 呼這個把道具放入第一個空 slot
        // 回傳 true 表示接受此道具（包含已安置、放入握持區或已排程展開後加入）
        public bool AddItemToSlot(ItemData itemData)
        {
            if (itemData == null)
            {
                Debug.LogWarning("[InventoryUI] AddItemToSlot: itemData is null");
                return false;
            }

            // 診斷：列出目前 slots 數量與每個 slot 是否為空
            Debug.Log($"[InventoryUI] AddItemToSlot: slots.Count={slots?.Count ?? 0}");
            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    bool empty = s == null || s.itemData == null;
                    Debug.Log($"  slot[{i}] = {(s==null? "null" : s.gameObject.name)} empty={empty}");
                }
            }

            // 嘗試加入（使用指定分配順序）
            if (TryAddToFirstEmpty(itemData))
                return true;

            // 若第一次失敗，重新掃描 slots（如果 inspector 並未填入或動態改變）
            var found = GetComponentsInChildren<InventorySlot>(true);
            Debug.Log($"[InventoryUI] After first try, found child InventorySlot count={found?.Length ?? 0}");
            if (found != null && found.Length > 0)
            {
                slots = new List<InventorySlot>(found);
                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    if (s == null) continue;
                    s.slotIndex = i;
                    s.owner = this;
                }

                if (TryAddToFirstEmpty(itemData))
                    return true;
            }

            // 若仍找不到空位：若握持區為空，放到握持區作為 fallback（避免直接丟警告）
            if (IsHeldEmpty())
            {
                Debug.Log("[InventoryUI] 沒有空位，但握持區為空，將物品放到握持區（fallback）");
                SetHeldItem(itemData.icon, itemData);
                return true;
            }

            // 嘗試自動展開（若設定了 panelToMove），然後在展開完成後再嘗試加入一次（視為接受並排程）
            if (panelToMove != null)
            {
                Debug.Log("[InventoryUI] No empty slot found — 嘗試自動展開並再次加入（已排程）");
                StartCoroutine(TryAddAfterExpand(itemData));
                return true;
            }

            Debug.LogWarning("[InventoryUI] AddItemToSlot: no empty slot found");
            return false;
        }

        // helper: 嘗試把 item 加到第一個空 slot（回傳成功與否）
        private bool TryAddToFirstEmpty(ItemData itemData)
        {
            var ordered = GetAssignmentOrder();
            for (int i = 0; i < ordered.Count; i++)
            {
                var s = ordered[i];
                if (s == null) continue;
                bool empty = s.itemData == null;
                if (empty)
                {
                    s.itemData = itemData;
                    s.SetIcon(itemData.icon);
                    if (Debug.isDebugBuild) Debug.Log($"[InventoryUI] Added item '{itemData.itemName}' (id {itemData.id}) to slot {i} name={s.gameObject.name}");
                    return true;
                }
            }
            return false;
        }

        private IEnumerator TryAddAfterExpand(ItemData itemData)
        {
            // 如果正在動畫中則等候
            if (_isAnimating)
                yield return new WaitUntil(() => !_isAnimating);

            // 發起展開（如果尚未展開）
            if (!_isExpanded)
                StartCoroutine(AnimateExpand(true));

            // 等待展開動畫完成（加上一點 margin）
            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, moveDuration + 0.05f));

            // 再次嘗試加入（展開後某些 hidden slot 可能會被啟用）
            // 重新掃描子節點（以防動態面板內有 slot）
            var found = GetComponentsInChildren<InventorySlot>(true);
            if (found != null && found.Length > 0)
            {
                slots = new List<InventorySlot>(found);
                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    if (s == null) continue;
                    s.slotIndex = i;
                    s.owner = this;
                }
            }

            if (TryAddToFirstEmpty(itemData))
                yield break;

            Debug.LogWarning("[InventoryUI] AddItemToSlot after expand: still no empty slot found");
        }

        // UI Button or EventTrigger 可以綁到這個以清除握持
        public void OnHeldAreaClick()
        {
            ClearHeldItem();
            if (Debug.isDebugBuild) Debug.Log("[InventoryUI] OnHeldAreaClick: cleared held item");
        }

        // ---- Drag API called from InventorySlot ----
        public void OnSlotClicked(InventorySlot slot)
        {
            if (slot == null) return;

            // 若握持區有物品 => 與 slot 交換
            if (_heldItemData != null)
            {
                // swap
                ItemData temp = slot.itemData;
                slot.itemData = _heldItemData;
                slot.SetIcon(slot.itemData != null ? slot.itemData.icon : null);

                if (temp != null)
                {
                    SetHeldItem(temp.icon, temp);
                }
                else
                {
                    ClearHeldItem();
                }
                return;
            }

            // 若握持區空，slot 有 item，則把 slot 的 item 拿起放到握持區
            if (slot.itemData != null)
            {
                SetHeldItem(slot.itemData.icon, slot.itemData);
                slot.itemData = null;
                slot.SetIcon(null);
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log("[InventoryUI] OnSlotClicked: clicked empty slot");
            }
        }

        public void OnSlotBeginDrag(InventorySlot slot, PointerEventData eventData)
        {
            if (slot == null || slot.itemData == null) return;
            if (canvas == null)
            {
                Debug.LogWarning("[InventoryUI] Canvas 未指定，無法拖放");
                return;
            }

            _dragSource = slot;
            CreateDragIcon(slot.iconImage != null ? slot.iconImage.sprite : null);
            UpdateDragIconPosition(eventData);
            // 阻擋拖放圖示被 Raycast
            if (_dragIcon != null) _dragIcon.raycastTarget = false;
        }

        public void OnSlotDrag(InventorySlot slot, PointerEventData eventData)
        {
            if (_dragIcon == null) return;
            UpdateDragIconPosition(eventData);
        }

        public void OnSlotEndDrag(InventorySlot slot, PointerEventData eventData)
        {
            if (_dragSource == null)
            {
                CleanupDrag();
                return;
            }

            // 嘗試找到落下目標（先由 eventData.pointerCurrentRaycast）
            GameObject hit = eventData.pointerCurrentRaycast.gameObject;

            InventorySlot targetSlot = null;
            if (hit != null)
                targetSlot = hit.GetComponentInParent<InventorySlot>();

            // 若丟到另一個 slot 上（不同 slot） => 移動或交換
            if (targetSlot != null && targetSlot != _dragSource)
            {
                // 如果 target 有東西就交換，否則移動
                var srcData = _dragSource.itemData;
                var tgtData = targetSlot.itemData;

                targetSlot.itemData = srcData;
                targetSlot.SetIcon(srcData != null ? srcData.icon : null);

                _dragSource.itemData = tgtData;
                _dragSource.SetIcon(tgtData != null ? tgtData.icon : null);
            }
            else
            {
                // 檢查是否丟到握持區（heldItemImage 的 RectTransform）
                if (heldItemImage != null)
                {
                    RectTransform heldRect = heldItemImage.rectTransform;
                    if (RectTransformUtility.RectangleContainsScreenPoint(heldRect, eventData.position, canvas.worldCamera))
                    {
                        // 把來源 slot 的物品放到握持區（如果握持區已經有物則替換）
                        SetHeldItem(_dragSource.itemData != null ? _dragSource.itemData.icon : null, _dragSource.itemData);
                        _dragSource.itemData = null;
                        _dragSource.SetIcon(null);
                        CleanupDrag();
                        return;
                    }
                }

                // 其它位置：回復原位（不改）
            }

            CleanupDrag();
        }

        // helper: create follow icon
        private void CreateDragIcon(Sprite sprite)
        {
            CleanupDrag();
            if (canvas == null) return;
            if (sprite == null) return;

            GameObject go = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            _dragIcon = go.GetComponent<Image>();
            _dragIcon.sprite = sprite;
            _dragIcon.raycastTarget = false;
            var rt = _dragIcon.rectTransform;
            rt.sizeDelta = new Vector2(64, 64);
            CanvasGroup cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
        }

        private void UpdateDragIconPosition(PointerEventData eventData)
        {
            if (_dragIcon == null || canvas == null) return;
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out pos);
            _dragIcon.rectTransform.anchoredPosition = pos;
        }

        private void CleanupDrag()
        {
            if (_dragIcon != null)
            {
                Destroy(_dragIcon.gameObject);
                _dragIcon = null;
            }
            _dragSource = null;
        }
    }
}