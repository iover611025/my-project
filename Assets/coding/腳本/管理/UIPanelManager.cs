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
            // --- 新增：當放大檢視或開啟子面板時，強制關閉對話框 ---
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ForceCloseDialogue();
            }
            // ----------------------------------------------------

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

            // --- 新增：當玩家點擊返回（Fade按鈕）時，立即關閉對話框 ---
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ForceCloseDialogue();
            }
            // -------------------------------------------------------

            var last = _history.Pop();
            last.panel.SetActive(false);

            // ... (其餘原有的 PopPanel 邏輯，包含恢復攝影機晃動等)
            if (_history.Count > 0)
            {
                // ...
            }
            else
            {
                if (CameraFollowMouse.Instance != null) CameraFollowMouse.Instance.SetSwayActive(true);
                if (_activeReturnObj != null) _activeReturnObj.SetActive(false);
                IsBlockingInput = false;
            }
        }

        /// <summary>
        /// 供外部腳本呼叫：強制關閉所有已開啟的面板並清理 Return 按鈕。
        /// 用於面板被外部邏輯（如解謎成功、條件達成等）直接關閉時，
        /// 確保 UIPanelManager 的堆疊與 UI 狀態保持同步。
        /// </summary>
        public void ForceCloseAllPanels()
        {
            // 關閉對話框
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ForceCloseDialogue();
            }

            // 逐一關閉堆疊中所有面板
            while (_history.Count > 0)
            {
                var state = _history.Pop();
                if (state.panel != null)
                {
                    state.panel.SetActive(false);
                }
            }

            // 隱藏 Return 按鈕
            if (_activeReturnObj != null)
            {
                _activeReturnObj.SetActive(false);
            }

            // 恢復攝影機晃動
            if (CameraFollowMouse.Instance != null)
            {
                CameraFollowMouse.Instance.SetSwayActive(true);
            }

            // 解除輸入阻擋
            IsBlockingInput = false;
        }

        /// <summary>
        /// 替換堆疊頂層的面板（不改變堆疊深度）。
        /// 適用於：放大畫面中透過道具互動切換物件狀態時
        /// （例如：花盆「初始」→「種子」），讓 Return 按鈕指向新面板，
        /// 玩家點擊 Return 能正確關閉新面板並返回房間。
        /// </summary>
        public void ReplaceTopPanel(GameObject newPanel)
        {
            if (_history.Count == 0 || newPanel == null) return;

            // 取出舊的頂層（面板已經被外部關閉了，只需更新堆疊記錄）
            var oldState = _history.Pop();

            // 確保舊面板已關閉
            if (oldState.panel != null && oldState.panel.activeSelf)
            {
                oldState.panel.SetActive(false);
            }

            // 開啟新面板並推入堆疊（沿用原本的 settings，保留 return 設定）
            newPanel.SetActive(true);
            _history.Push(new PanelState { panel = newPanel, settings = oldState.settings });

            // 確保 Return 按鈕維持在最前層
            if (_activeReturnObj != null)
            {
                _activeReturnObj.transform.SetAsLastSibling();
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