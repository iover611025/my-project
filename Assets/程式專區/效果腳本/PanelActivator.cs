using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace X
{
    /// <summary>
    /// 點擊某個可互動物件時啟用指定 panel（可選同時關閉另一個 panel）。
    /// 支援在 Inspector 指派一個 Image（或其上附加的 Button），
    /// 當該 Image 被點擊時會關閉剛啟用的 panel（並可選擇重新開啟 panelToClose）。
    /// 點擊啟用 panel 時會同時啟用 returnImage；點擊 returnImage 會關閉 panel 並關閉 returnImage。
    /// 新增：當 returnImage 出現時，可根據游標與 image 的垂直（Y 軸）距離改變透明度（游標越接近越不透明）。
    /// 同時在 returnImage 顯示期間會阻擋全局 A/D 輸入（由 RoomUIManager 檢查 PanelActivator.IsBlockingInput）。
    /// </summary>
    public class PanelActivator : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("要啟用的 Panel（GameObject）")]
        public GameObject panelToOpen;

        [Tooltip("可選：要關閉的 Panel（啟用時會被關閉，返回時可重新開啟）")]
        public GameObject panelToClose;

        [Tooltip("點擊後是否只在啟用 panelToOpen 時呼叫 SetActive(true)（預設 true）")]
        public bool openOnly = true;

        [Header("返回設定")]
        [Tooltip("點擊 panelToOpen 後，指定此 Image 作為返回按鈕（若該 Image 沒有 Button 元件，會在執行時自動新增一個 Button）。")]
        public Image returnImage;

        [Header("游標接近顯示設定")]
        [Tooltip("當游標在垂直方向（Y）距離 returnImage 中心小於此值時開始顯示（像素，screen space）")]
        public float revealRadius = 200f;
        [Tooltip("最小透明度（出現時的初始透明度）")]
        [Range(0f, 1f)]
        public float minAlpha = 0f;
        [Tooltip("游標完全接近時的最大透明度")]
        [Range(0f, 1f)]
        public float maxAlpha = 1f;
        [Tooltip("用來調整透明度隨距離變化的曲線，x=0(遠) -> x=1(近)")]
        public AnimationCurve proximityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // 當 returnImage 顯示且尚未點擊返回時，阻擋 RoomUIManager 的 A/D 輸入
        public static bool IsBlockingInput { get; private set; } = false;

        // internal
        private Button _returnButton;
        private bool _subscribedToReturn = false;
        private Color _origReturnColor = Color.white;
        private bool _hasOrigColor = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (panelToOpen != null)
            {
                panelToOpen.SetActive(true);
            }

            if (panelToClose != null)
            {
                panelToClose.SetActive(false);
            }

            // 若有指定 returnImage，確保其可點擊、顯示並訂閱回調
            if (returnImage != null)
            {
                EnsureReturnButton();

                // 記錄原始 color（僅第一次）
                if (!_hasOrigColor)
                {
                    _origReturnColor = returnImage.color;
                    _hasOrigColor = true;
                }

                // 顯示 returnImage（可能初始為隱藏）
                if (!returnImage.gameObject.activeSelf)
                    returnImage.gameObject.SetActive(true);

                // 初始設為 minAlpha（完全隱藏或低透明）
                SetReturnImageAlpha(minAlpha);

                SubscribeReturn();

                // 啟動全局輸入阻擋（RoomUIManager 會檢查此旗標）
                IsBlockingInput = true;
            }
        }

        private void EnsureReturnButton()
        {
            if (returnImage == null) return;

            if (_returnButton == null)
            {
                // 先嘗試取得已存在的 Button
                _returnButton = returnImage.GetComponent<Button>();
                // 若沒有，新增一個 Button 使 Image 可點擊
                if (_returnButton == null)
                {
                    _returnButton = returnImage.gameObject.AddComponent<Button>();
                    // 設定 Button 的過場顏色為不改變（避免自動高亮改變外觀）
                    var colors = _returnButton.colors;
                    colors.highlightedColor = colors.normalColor;
                    colors.pressedColor = colors.normalColor;
                    colors.selectedColor = colors.normalColor;
                    _returnButton.colors = colors;
                }
            }
        }

        private void SubscribeReturn()
        {
            if (_returnButton == null || _subscribedToReturn) return;
            _returnButton.onClick.AddListener(OnReturnClicked);
            _subscribedToReturn = true;
        }

        private void UnsubscribeReturn()
        {
            if (_returnButton == null || !_subscribedToReturn) return;
            _returnButton.onClick.RemoveListener(OnReturnClicked);
            _subscribedToReturn = false;
        }

        private void OnReturnClicked()
        {
            // 當 return 被點擊時，關閉剛開啟的 panel，並可選擇重新開啟 panelToClose（若有）
            if (panelToOpen != null)
                panelToOpen.SetActive(false);

            if (panelToClose != null)
                panelToClose.SetActive(true);

            // 隱藏 returnImage 並還原色彩
            if (returnImage != null && returnImage.gameObject.activeSelf)
            {
                returnImage.gameObject.SetActive(false);
                RestoreReturnImageColor();
            }

            // 取消訂閱，避免重複註冊或殘留 listener
            UnsubscribeReturn();

            // 解除全局輸入阻擋
            IsBlockingInput = false;
        }

        void Update()
        {
            // 當 returnImage 顯示時，根據游標垂直距離調整 alpha（只考慮 Y 軸）
            if (returnImage != null && returnImage.gameObject.activeInHierarchy)
            {
                var rt = returnImage.rectTransform;

                // 取得 returnImage 在螢幕座標的中心
                Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(returnImage.canvas?.worldCamera, rt.position);

                Vector2 mouse = Input.mousePosition;

                // 只計算 Y 軸的絕對距離（避免游標偏離 X 軸造成影響）
                float distY = Mathf.Abs(mouse.y - screenCenter.y);

                // 把垂直距離映射到 0..1（0 = 遠、大於 revealRadius 為 0；1 = 在中心）
                float t = Mathf.Clamp01(1f - (distY / Mathf.Max(0.0001f, revealRadius)));
                float eval = proximityCurve != null ? proximityCurve.Evaluate(t) : t;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, eval);

                SetReturnImageAlpha(alpha);
            }
        }

        private void SetReturnImageAlpha(float a)
        {
            if (returnImage == null) return;
            Color c = returnImage.color;
            c.a = Mathf.Clamp01(a);
            returnImage.color = c;
        }

        private void RestoreReturnImageColor()
        {
            if (returnImage == null) return;
            if (_hasOrigColor)
                returnImage.color = _origReturnColor;
        }

        void OnDisable()
        {
            // 確保在物件被停用或場景切換時移除 listener 並隱藏 returnImage，並還原色彩
            UnsubscribeReturn();
            if (returnImage != null && returnImage.gameObject.activeSelf)
            {
                returnImage.gameObject.SetActive(false);
                RestoreReturnImageColor();
            }

            // 解除阻擋以防異常情況造成輸入鎖死
            IsBlockingInput = false;
        }

        void OnDestroy()
        {
            UnsubscribeReturn();
            IsBlockingInput = false;
        }
    }
}