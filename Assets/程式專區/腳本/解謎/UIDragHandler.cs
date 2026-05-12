using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace X
{
    public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("配置設定")]
        public RectTransform targetRect;
        public float snapThreshold = 50f;

        // 引用管理器
        public UIPuzzleManager manager;

        private RectTransform _rectTransform;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Vector2 _originalPosition;
        private Vector2 _dragOffset; // 新增：指標與物件的偏移
        public bool IsSolved { get; private set; } = false; // 讓管理器可以讀取狀態

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _originalPosition = _rectTransform.anchoredPosition;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsSolved) return;
            _canvasGroup.alpha = 0.7f;
            _canvasGroup.blocksRaycasts = false;
            transform.SetAsLastSibling(); // 確保拖動時在最上層

            // 計算滑鼠點擊位置與 RectTransform 錨點座標之間的偏移（解決 scaleFactor/CanvasScaler 導致的跳動）
            if (_canvas != null)
            {
                RectTransform canvasRect = _canvas.transform as RectTransform;
                Vector2 localPointer;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, _canvas.worldCamera, out localPointer);
                _dragOffset = _rectTransform.anchoredPosition - localPointer;
            }
            else
            {
                _dragOffset = Vector2.zero;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsSolved) return;

            // 使用 ScreenPointToLocalPointInRectangle 轉換為 Canvas 的本地座標，再加上偏移，避免直接使用 delta / scaleFactor 帶來的不一致
            if (_canvas != null)
            {
                RectTransform canvasRect = _canvas.transform as RectTransform;
                Vector2 localPointer;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, _canvas.worldCamera, out localPointer))
                {
                    _rectTransform.anchoredPosition = localPointer + _dragOffset;
                    return;
                }
            }

            // fallback（如果沒有 Canvas 或轉換失敗）
            _rectTransform.anchoredPosition += eventData.delta / Mathf.Max(0.0001f, (_canvas != null ? _canvas.scaleFactor : 1f));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsSolved) return;
            _canvasGroup.alpha = 1.0f;
            _canvasGroup.blocksRaycasts = true;

            float distance = Vector2.Distance(_rectTransform.anchoredPosition, targetRect.anchoredPosition);

            if (distance <= snapThreshold)
            {
                SnapToTarget();
            }
            else
            {
                _rectTransform.anchoredPosition = _originalPosition;
            }
        }

        private void SnapToTarget()
        {
            _rectTransform.anchoredPosition = targetRect.anchoredPosition;
            IsSolved = true;

            // 通知管理器檢查進度
            if (manager != null)
            {
                manager.CheckPuzzleStatus();
            }
        }
    }
}