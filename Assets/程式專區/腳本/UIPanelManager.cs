using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace X
{
    public class UIPanelManager : MonoBehaviour
    {
        public static UIPanelManager Instance;

        private struct PanelState
        {
            public GameObject panel;
            public PanelActivator settings; // 儲存該層的透明度與距離設定
        }

        private Stack<PanelState> _history = new Stack<PanelState>();

        [Header("共用返回按鈕實體")]
        private GameObject _activeReturnObj;
        private Image _activeReturnImage;
        private Button _activeReturnButton;
        public bool IsBlockingInput;

        void Awake() { Instance = this; }

        public void PushPanel(GameObject panel, PanelActivator settings)
        {
            // 如果堆疊中有前一個，先把它「凍結」
            IsBlockingInput = true;
            if (_history.Count > 0)
            {
                var top = _history.Peek();
                if (top.panel.TryGetComponent<CanvasGroup>(out var cg))
                {
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }
            }

            panel.SetActive(true);
            _history.Push(new PanelState { panel = panel, settings = settings });

            // 確保返回按鈕存在且在最前層
            SetupReturnButton(settings);
        }

        private void SetupReturnButton(PanelActivator settings)
        {
            // 如果還沒生成過按鈕，則生成
            if (_activeReturnObj == null && settings.returnPrefab != null)
            {
                _activeReturnObj = Instantiate(settings.returnPrefab, settings.returnParentCanvas?.transform ?? transform, false);
                _activeReturnImage = _activeReturnObj.GetComponentInChildren<Image>();
                _activeReturnButton = _activeReturnObj.GetComponentInChildren<Button>();

                if (_activeReturnButton == null) _activeReturnButton = _activeReturnObj.AddComponent<Button>();
                _activeReturnButton.onClick.AddListener(PopPanel);
            }

            if (_activeReturnObj != null)
            {
                _activeReturnObj.SetActive(true);
                _activeReturnObj.transform.SetAsLastSibling();
            }
        }

        public void PopPanel()
        {
            if (_history.Count == 0) return;

            var last = _history.Pop();
            last.panel.SetActive(false);

            // 如果還有下一層，恢復它
            if (_history.Count > 0)
            {
                var next = _history.Peek();
                next.panel.SetActive(true); // 確保它是開啟的
                if (next.panel.TryGetComponent<CanvasGroup>(out var cg))
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
            else
            {
                // 全部退出了，隱藏返回按鈕
                if (_activeReturnObj != null) _activeReturnObj.SetActive(false);
                IsBlockingInput = false;

            }
        }

        void Update()
        {
            // 處理當前最上層的透明度邏輯 (鏽湖風格)
            if (_history.Count > 0 && _activeReturnImage != null)
            {
                var current = _history.Peek().settings;
                UpdateAlpha(current);
            }
        }

        private void UpdateAlpha(PanelActivator s)
        {
            Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(_activeReturnImage.canvas?.worldCamera, _activeReturnImage.rectTransform.position);
            float distY = Mathf.Abs(Input.mousePosition.y - screenCenter.y);
            float t = Mathf.Clamp01(1f - (distY / Mathf.Max(0.0001f, s.revealRadius)));
            float alpha = Mathf.Lerp(s.minAlpha, s.maxAlpha, s.proximityCurve.Evaluate(t));

            Color c = _activeReturnImage.color;
            c.a = alpha;
            _activeReturnImage.color = c;
        }
    }
}