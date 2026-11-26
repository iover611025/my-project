using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace X
{
    /// <summary>
    /// 掛在「燈泡」（觸發器）上。
    /// 點擊燈泡後，會尋找場景中所有 FlagMarker 物件，
    /// 並讓有旗幟的物件顯示白框；再次點擊則隱藏所有白框。
    ///
    /// 使用方式：
    ///   1. 將此腳本掛在燈泡物件上（需有 Image 元件）。
    ///   2. 場景中所有要被標記的物件掛上 FlagMarker 腳本。
    ///   3. 點擊燈泡即可切換顯示模式（預設為 Toggle）。
    ///      若想用外部 OnClick() 控制，也可呼叫 Reveal() / Hide() / ToggleReveal()。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class FlagRevealTrigger : MonoBehaviour, IPointerClickHandler
    {
        [Header("搜尋範圍")]
        [Tooltip("要搜尋 FlagMarker 的根物件。留空則搜尋全場景（FindObjectsOfType）。")]
        [SerializeField] private Transform searchRoot;

        [Header("燈泡行為")]
        [Tooltip("點擊燈泡時的行為：Toggle（切換）、RevealOnly（只開）、HideOnly（只關）。")]
        [SerializeField] private TriggerMode mode = TriggerMode.Toggle;

        [Tooltip("燈泡初始是否處於顯示模式。")]
        [SerializeField] private bool startRevealed = false;

        [Header("自動關閉設定")]
        [Tooltip("當更換大場景或小場景（房間）時，自動關閉顯示模式。")]
        [SerializeField] private bool autoCloseOnSceneChange = true;

        [Tooltip("當點擊其他物件時，自動關閉顯示模式。")]
        [SerializeField] private bool autoCloseOnObjectClick = true;

        [Header("圖示同步（選填）")]
        [Tooltip("搭配使用的 ImageToggleOnClick；留空時會自動抓取同物件上的元件。")]
        [SerializeField] private ImageToggleOnClick imageToggle;

        public enum TriggerMode { Toggle, RevealOnly, HideOnly }

        private bool _isRevealed;
        private bool _isHandlingPointerClick;
        private bool _hasRecordedRoomUI;
        private int _lastBigSceneIndex;
        private int _lastRoomIndex;
        private bool _hasRecordedBigSceneManager;
        private int _lastBigSceneManagerId;
        private int _lastBigSceneManagerRoomIndex;
        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

        // ────────────────────────────────────────────────────────────
        // Unity 生命週期
        // ────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (imageToggle == null)
                TryGetComponent(out imageToggle);
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void Start()
        {
            _isRevealed = startRevealed;
            if (_isRevealed)
                RecordCurrentSceneState();
            BroadcastReveal(_isRevealed);
            SyncImageToggle(_isRevealed);
        }

        private void Update()
        {
            if (!_isRevealed) return;

            if (autoCloseOnSceneChange && HasSceneOrRoomChanged())
            {
                Hide();
                return;
            }

            if (autoCloseOnObjectClick && Input.GetMouseButtonDown(0) && IsClickingOtherObject())
            {
                Hide();
            }
        }

        private void LateUpdate()
        {
            if (imageToggle != null && imageToggle.IsUsingSecond != _isRevealed)
                imageToggle.SetUsingSecond(_isRevealed);
        }

        // ────────────────────────────────────────────────────────────
        // IPointerClickHandler：點擊燈泡
        // ────────────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            _isHandlingPointerClick = true;
            switch (mode)
            {
                case TriggerMode.Toggle:     ToggleReveal(); break;
                case TriggerMode.RevealOnly: Reveal();       break;
                case TriggerMode.HideOnly:   Hide();         break;
            }
            _isHandlingPointerClick = false;
        }

        // ────────────────────────────────────────────────────────────
        // 公開方法：供 OnClick() / UnityEvent 呼叫
        // ────────────────────────────────────────────────────────────

        /// <summary>開啟顯示模式：所有有旗幟的物件顯示白框。</summary>
        public void Reveal()
        {
            _isRevealed = true;
            RecordCurrentSceneState();
            BroadcastReveal(true);
            if (!_isHandlingPointerClick)
                SyncImageToggle(true);
        }

        /// <summary>關閉顯示模式：隱藏所有白框。</summary>
        public void Hide()
        {
            _isRevealed = false;
            BroadcastReveal(false);
            if (!_isHandlingPointerClick)
                SyncImageToggle(false);
        }

        /// <summary>切換顯示模式。</summary>
        public void ToggleReveal()
        {
            _isRevealed = !_isRevealed;
            if (_isRevealed)
                RecordCurrentSceneState();
            BroadcastReveal(_isRevealed);
            if (!_isHandlingPointerClick)
                SyncImageToggle(_isRevealed);
        }

        /// <summary>查詢目前是否處於顯示模式。</summary>
        public bool IsRevealed => _isRevealed;

        // ────────────────────────────────────────────────────────────
        // 內部方法
        // ────────────────────────────────────────────────────────────

        /// <summary>將顯示狀態廣播給所有 FlagMarker。</summary>
        private void BroadcastReveal(bool active)
        {
            FlagMarker[] markers = searchRoot != null
                ? searchRoot.GetComponentsInChildren<FlagMarker>(includeInactive: true)
                : FindObjectsByType<FlagMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (FlagMarker marker in markers)
                marker.SetRevealActive(active);
        }

        private void SyncImageToggle(bool active)
        {
            if (imageToggle != null)
                imageToggle.SetUsingSecond(active);
        }

        private void OnActiveSceneChanged(Scene prev, Scene next)
        {
            if (_isRevealed && autoCloseOnSceneChange)
                Hide();
        }

        /// <summary>記錄目前所在的大小場景與房間索引，供後續偵測切換。</summary>
        private void RecordCurrentSceneState()
        {
            if (RoomUIManager.Instance != null)
            {
                _hasRecordedRoomUI = true;
                _lastBigSceneIndex = RoomUIManager.Instance.CurrentBigSceneIndex;
                _lastRoomIndex = RoomUIManager.Instance.CurrentRoomIndex;
            }
            else
            {
                _hasRecordedRoomUI = false;
            }

            if (BigSceneManager.Instance != null)
            {
                _hasRecordedBigSceneManager = true;
                _lastBigSceneManagerId = BigSceneManager.Instance.currentSceneId;
                _lastBigSceneManagerRoomIndex = BigSceneManager.Instance.currentRoomIndex;
            }
            else
            {
                _hasRecordedBigSceneManager = false;
            }
        }

        /// <summary>檢查大場景或小場景（房間）是否已切換。</summary>
        private bool HasSceneOrRoomChanged()
        {
            if (RoomUIManager.Instance != null)
            {
                if (!_hasRecordedRoomUI)
                {
                    _hasRecordedRoomUI = true;
                    _lastBigSceneIndex = RoomUIManager.Instance.CurrentBigSceneIndex;
                    _lastRoomIndex = RoomUIManager.Instance.CurrentRoomIndex;
                }
                else if (RoomUIManager.Instance.CurrentBigSceneIndex != _lastBigSceneIndex ||
                         RoomUIManager.Instance.CurrentRoomIndex != _lastRoomIndex)
                {
                    return true;
                }
            }

            if (BigSceneManager.Instance != null)
            {
                if (!_hasRecordedBigSceneManager)
                {
                    _hasRecordedBigSceneManager = true;
                    _lastBigSceneManagerId = BigSceneManager.Instance.currentSceneId;
                    _lastBigSceneManagerRoomIndex = BigSceneManager.Instance.currentRoomIndex;
                }
                else if (BigSceneManager.Instance.currentSceneId != _lastBigSceneManagerId ||
                         BigSceneManager.Instance.currentRoomIndex != _lastBigSceneManagerRoomIndex)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>檢查滑鼠點擊是否命中燈泡以外的 UI 或世界物件。</summary>
        private bool IsClickingOtherObject()
        {
            if (EventSystem.current != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                _raycastResults.Clear();
                EventSystem.current.RaycastAll(pointerData, _raycastResults);

                if (_raycastResults.Count > 0)
                {
                    GameObject topHit = _raycastResults[0].gameObject;
                    if (topHit == gameObject || topHit.transform.IsChildOf(transform))
                        return false;

                    return true;
                }
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 mousePos = Input.mousePosition;
                Ray ray = cam.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit hit3D))
                {
                    if (hit3D.transform != transform && !hit3D.transform.IsChildOf(transform))
                        return true;
                }

                Vector2 worldPoint2D = cam.ScreenToWorldPoint(mousePos);
                Collider2D hit2D = Physics2D.OverlapPoint(worldPoint2D);
                if (hit2D != null && hit2D.transform != transform && !hit2D.transform.IsChildOf(transform))
                    return true;
            }

            return false;
        }
    }
}
