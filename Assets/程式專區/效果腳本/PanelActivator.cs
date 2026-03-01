using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace X
{
    /// <summary>
    /// 點擊某個可互動物件時啟用指定 panel（可選同時關閉另一個 panel）。
    /// 支援在 Inspector 指派一個「圖片返回」prefab，會在指定 parentPanel 生成該 prefab。
    /// 點擊該 prefab 的按鈕會關閉指定的 panel（panelToCloseOnReturn；若未指定則使用 panelToClose）並銷毀該 prefab。
    /// 也保留游標接近時調整透明度、輸入阻擋等行為，並支援顯示時機設定。
    /// </summary>
    public class PanelActivator : MonoBehaviour, IPointerClickHandler
    {
        public enum ReturnShowTiming
        {
            Immediate,
            AfterOpenDelay,
            Manual
        }

        [Header("Panel 設定")]
        [Tooltip("要啟用的 Panel（GameObject）")]
        public GameObject panelToOpen;
        [Tooltip("可選：要關閉的 Panel（啟用時會被關閉，返回時可重新開啟）")]
        public GameObject panelToClose;
        [Tooltip("點擊後是否只在啟用 panelToOpen 時呼叫 SetActive(true)")]
        public bool openOnly = true;

        [Header("Return Prefab（新的行為）")]
        [Tooltip("要生成的『圖片返回』Prefab（必須包含 Image，建議有 Button）")]
        public GameObject returnPrefab;
        [Tooltip("生成 returnPrefab 的父 Canvas（若留空，會嘗試使用 panelToOpen 所在的 Canvas，或場景中第一個 Canvas）")]
        public Canvas returnParentCanvas;
        [Tooltip("點擊返回時要關閉的 Panel（若為 null 則使用 panelToClose）")]
        public GameObject panelToCloseOnReturn;

        [Header("Return 顯示時機")]
        public ReturnShowTiming returnShowTiming = ReturnShowTiming.Immediate;
        [Tooltip("在使用 AfterOpenDelay 時的延遲（秒）")]
        public float returnShowDelay = 0.08f;

        [Header("游標接近顯示設定")]
        [Tooltip("當游標在垂直方向（Y）距離 returnImage 中心小於此值時開始顯示（像素，screen space）")]
        public float revealRadius = 200f;
        [Range(0f, 1f)] public float minAlpha = 0f;
        [Range(0f, 1f)] public float maxAlpha = 1f;
        [Tooltip("用來調整透明度隨距離變化的曲線，x=0(遠) -> x=1(近)")]
        public AnimationCurve proximityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("多層支援")]
        [Tooltip("點擊返回後是否自動銷毀生成的 prefab（通常為 true）")]
        public bool destroyReturnPrefabOnClick = true;

        // 多層阻擋計數（支持多個 activator 疊加）
        private static int s_blockingCount = 0;
        public static bool IsBlockingInput { get { return s_blockingCount > 0; } }

        // internal runtime references
        private GameObject _spawnedReturnObj;
        private Image _spawnedReturnImage;
        private Button _spawnedReturnButton;
        private Coroutine _delayedShowCoroutine;
        private Color _origReturnColor;
        private bool _hasOrigColor = false;
        private bool _subscribed = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (panelToOpen != null)
                panelToOpen.SetActive(true);

            if (panelToClose != null)
                panelToClose.SetActive(false);

            // 決定何時顯示 return prefab
            if (returnPrefab == null) return;

            if (returnShowTiming == ReturnShowTiming.Immediate)
            {
                ShowReturnPrefab();
            }
            else if (returnShowTiming == ReturnShowTiming.AfterOpenDelay)
            {
                if (_delayedShowCoroutine != null) StopCoroutine(_delayedShowCoroutine);
                _delayedShowCoroutine = StartCoroutine(ShowReturnAfterDelay(returnShowDelay));
            }
            else
            {
                // Manual: 不自動顯示
            }
        }

        private IEnumerator ShowReturnAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
            _delayedShowCoroutine = null;
            ShowReturnPrefab();
        }

        // 公開：手動顯示（Manual 模式使用）
        public void ShowReturnPrefab()
        {
            if (returnPrefab == null) return;

            // 若已經生成則不重複生成
            if (_spawnedReturnObj != null) return;

            // 決定 parent：優先使用 returnParentCanvas（已存在的 Canvas），若空則嘗試使用 panelToOpen 的 Canvas，再 fallback 到場景第一個 Canvas，最後使用 panelToOpen.transform
            Transform parent;
            Canvas targetCanvas = returnParentCanvas;
            if (targetCanvas == null && panelToOpen != null)
            {
                targetCanvas = panelToOpen.GetComponentInParent<Canvas>();
            }
            if (targetCanvas == null)
            {
                targetCanvas = FindAnyObjectByType<Canvas>();
            }

            if (targetCanvas != null)
                parent = targetCanvas.transform;
            else if (panelToOpen != null)
                parent = panelToOpen.transform;
            else
                parent = null;

            _spawnedReturnObj = Instantiate(returnPrefab, parent, false);

            // 若 prefab 是 UI 元件且我們把它放到 Canvas 下，確保其 RectTransform 保持 local transform（Instantiate(..., parent, false) 已處理）
            if (parent != null)
                _spawnedReturnObj.transform.SetAsLastSibling();

            // 找 Image 與 Button（在 spawned prefab 的子物件中搜尋）
            _spawnedReturnImage = _spawnedReturnObj.GetComponentInChildren<Image>();
            _spawnedReturnButton = _spawnedReturnObj.GetComponentInChildren<Button>();

            if (_spawnedReturnImage != null && !_hasOrigColor)
            {
                _origReturnColor = _spawnedReturnImage.color;
                _hasOrigColor = true;
            }

            // 初始透明度
            if (_spawnedReturnImage != null)
            {
                SetSpawnedReturnAlpha(minAlpha);
            }

            // 若沒有 Button 則新增一個透明 Button 於根物件上讓它可點擊
            if (_spawnedReturnButton == null)
            {
                _spawnedReturnButton = _spawnedReturnObj.GetComponent<Button>();
                if (_spawnedReturnButton == null)
                    _spawnedReturnButton = _spawnedReturnObj.AddComponent<Button>();
                var cols = _spawnedReturnButton.colors;
                cols.highlightedColor = cols.normalColor;
                cols.pressedColor = cols.normalColor;
                cols.selectedColor = cols.normalColor;
                _spawnedReturnButton.colors = cols;
            }

            SubscribeSpawnedReturn();

            // 增加阻擋計數
            s_blockingCount = Mathf.Max(0, s_blockingCount) + 1;
        }

        private void SubscribeSpawnedReturn()
        {
            if (_spawnedReturnButton == null || _subscribed) return;
            _spawnedReturnButton.onClick.AddListener(OnSpawnedReturnClicked);
            _subscribed = true;
        }

        private void UnsubscribeSpawnedReturn()
        {
            if (_spawnedReturnButton == null || !_subscribed) return;
            _spawnedReturnButton.onClick.RemoveListener(OnSpawnedReturnClicked);
            _subscribed = false;
        }

        private void OnSpawnedReturnClicked()
        {
            // 關閉要關閉的 panel（優先使用 panelToCloseOnReturn，否則使用 panelToClose）
            GameObject toClose = panelToCloseOnReturn != null ? panelToCloseOnReturn : panelToClose;
            if (toClose != null)
                toClose.SetActive(false);

            // 如果 panelToOpen 也要關閉（維持原本行為）
            if (panelToOpen != null)
                panelToOpen.SetActive(false);

            // 清理 listener
            UnsubscribeSpawnedReturn();

            // 銷毀 prefab（若設定）
            if (destroyReturnPrefabOnClick && _spawnedReturnObj != null)
            {
                Destroy(_spawnedReturnObj);
            }

            // 調整阻擋計數
            s_blockingCount = Mathf.Max(0, s_blockingCount - 1);

            // reset local refs
            _spawnedReturnObj = null;
            _spawnedReturnImage = null;
            _spawnedReturnButton = null;
        }

        void Update()
        {
            if (_spawnedReturnImage == null || _spawnedReturnObj == null || !_spawnedReturnObj.activeInHierarchy) return;

            // 取得 spawned image 在螢幕座標中心
            Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(_spawnedReturnImage.canvas?.worldCamera, _spawnedReturnImage.rectTransform.position);
            Vector2 mouse = Input.mousePosition;

            float distY = Mathf.Abs(mouse.y - screenCenter.y);
            float t = Mathf.Clamp01(1f - (distY / Mathf.Max(0.0001f, revealRadius)));
            float eval = proximityCurve != null ? proximityCurve.Evaluate(t) : t;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, eval);

            SetSpawnedReturnAlpha(alpha);
        }

        private void SetSpawnedReturnAlpha(float a)
        {
            if (_spawnedReturnImage == null) return;
            Color c = _spawnedReturnImage.color;
            c.a = Mathf.Clamp01(a);
            _spawnedReturnImage.color = c;
        }

        private void RestoreSpawnedReturnColor()
        {
            if (_spawnedReturnImage == null) return;
            if (_hasOrigColor)
                _spawnedReturnImage.color = _origReturnColor;
        }

        void OnDisable()
        {
            // 停止 delay coroutine
            if (_delayedShowCoroutine != null)
            {
                StopCoroutine(_delayedShowCoroutine);
                _delayedShowCoroutine = null;
            }

            // 清理 spawned return（若存在）
            if (_spawnedReturnObj != null)
            {
                UnsubscribeSpawnedReturn();
                if (destroyReturnPrefabOnClick)
                    Destroy(_spawnedReturnObj);
                else
                    _spawnedReturnObj.SetActive(false);
                _spawnedReturnObj = null;
                _spawnedReturnImage = null;
                _spawnedReturnButton = null;
            }

            // 保險：調整阻擋計數（避免鎖死）
            s_blockingCount = Mathf.Max(0, s_blockingCount - 1);
        }

        void OnDestroy()
        {
            UnsubscribeSpawnedReturn();
            if (_spawnedReturnObj != null)
            {
                if (destroyReturnPrefabOnClick)
                    Destroy(_spawnedReturnObj);
                else
                    _spawnedReturnObj.SetActive(false);
            }
            // 保險：清理計數
            s_blockingCount = Mathf.Max(0, s_blockingCount - 1);
        }
    }
}