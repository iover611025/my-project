using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace X
{
    public class UICoverManager : MonoBehaviour
    {
        public GameObject mainMenuPanel;
        public GameObject settingsPanel;
        public GameObject gamePanel;
        public GameObject overlaySettingsButton; // 設定按鈕（獨立，不會被隱藏）
        public Slider volumeSlider;

        [Header("黑幕效果")]
        public Image blackFadeImage; // 拖入全螢幕Image，預設alpha=0
        public float fadeDuration = 0.5f; // 淡入/淡出時間
        public float blackStayDuration = 1.0f; // 黑幕停留時間

        [Header("黑幕文字（可選）")]
        public Text blackFadeText; // 黑幕上顯示的文字（可為 null）
        public float textFadeDuration = 0.5f; // 文字淡入/淡出時間

        [Header("互動設定")]
        public bool blockInputDuringFade = true; // true：淡入時阻擋互動；false：淡入期間仍允許穿透點擊

        private GameObject lastActivePanel; // 記錄上一個顯示的Panel
        private GameObject panelBeforeSettings; // 記錄進入設定前的Panel

        // 黑幕 Canvas 原始設定（用於還原）
        private Canvas _blackCanvas;
        private bool _blackCanvasPrevOverride;
        private int _blackCanvasPrevOrder;
        private bool _isFading = false;

        // 全域旗標：讓其他系統能檢查是否在任何 instance 正在過場
        private static bool _globalIsFading = false;

        // 用來控制 Raycast 的 CanvasGroup（或利用 Image.raycastTarget）
        private CanvasGroup _blackCanvasGroup;

        // 對外可讀取的狀態
        public bool IsFading { get { return _isFading; } }
        public static bool GlobalIsFading { get { return _globalIsFading; } }

        void Start()
        {
            // 初始化黑幕（確保可見性與 alpha=0）
            if (blackFadeImage != null)
            {
                blackFadeImage.gameObject.SetActive(true);
                Color col = blackFadeImage.color;
                blackFadeImage.color = new Color(col.r, col.g, col.b, 0f);

                // 若沒有 Sprite，嘗試指定內建 UI sprite（避免 Image 不渲染）
                if (blackFadeImage.sprite == null)
                {
                    var builtin = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
                    if (builtin != null)
                        blackFadeImage.sprite = builtin;
                }

                // 預設讓 Image 不接收 Graphic Raycast（避免攔截）
                blackFadeImage.raycastTarget = false;

                // 取得或新增 CanvasGroup，用於切換 blocksRaycasts
                _blackCanvasGroup = blackFadeImage.GetComponent<CanvasGroup>();
                if (_blackCanvasGroup == null)
                    _blackCanvasGroup = blackFadeImage.gameObject.AddComponent<CanvasGroup>();

                // 預設不阻擋射線
                _blackCanvasGroup.interactable = false;
                _blackCanvasGroup.blocksRaycasts = false;

                _blackCanvas = blackFadeImage.canvas;
                if (_blackCanvas != null)
                {
                    _blackCanvasPrevOverride = _blackCanvas.overrideSorting;
                    _blackCanvasPrevOrder = _blackCanvas.sortingOrder;
                }
            }

            // 初始化黑幕文字（若有）
            if (blackFadeText != null)
            {
                blackFadeText.gameObject.SetActive(true);
                Color tcol = blackFadeText.color;
                blackFadeText.color = new Color(tcol.r, tcol.g, tcol.b, 0f);
            }

            // 初始化 UI 狀態：不要在 Start 自動觸發過場（直接設定初始面板）
            InitializePanels();

            if (volumeSlider != null)
                volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        // 初始化面板（不使用過場）
        private void InitializePanels()
        {
            SetPanelOrder(mainMenuPanel, 2);
            SetPanelOrder(settingsPanel, 1);
            SetPanelOrder(gamePanel, 1);

            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (gamePanel != null) gamePanel.SetActive(false);

            lastActivePanel = mainMenuPanel;
            if (overlaySettingsButton != null)
                overlaySettingsButton.SetActive(true);
        }

        // 黑幕切換介面（公開入口）
        public void FadeSwitchPanel(GameObject fromPanel, GameObject toPanel)
        {
            if (_isFading) return;
            StartCoroutine(FadeAndSwitchPanel(fromPanel, toPanel));
        }

        // 新增：黑幕 + 文字 轉場（外部可傳入文字）
        public void FadeSwitchPanelWithText(GameObject fromPanel, GameObject toPanel, string message)
        {
            if (_isFading) return;
            StartCoroutine(FadeAndSwitchPanelWithText(fromPanel, toPanel, message));
        }

        // 提供給外部查詢目前被顯示的 Panel（公開 wrapper）
        public GameObject GetCurrentlyActivePanel()
        {
            return GetCurrentlyActivePanel_Internal();
        }

        // 兼容不同命名的呼叫
        public GameObject GetCurrentActivePanel()
        {
            return GetCurrentlyActivePanel_Internal();
        }

        // 內部實作（原 private 名稱）
        private GameObject GetCurrentlyActivePanel_Internal()
        {
            if (settingsPanel != null && settingsPanel.activeSelf) return settingsPanel;
            if (mainMenuPanel != null && mainMenuPanel.activeSelf) return mainMenuPanel;
            if (gamePanel != null && gamePanel.activeSelf) return gamePanel;
            return null;
        }

        private IEnumerator FadeAndSwitchPanel(GameObject fromPanel, GameObject toPanel)
        {
            if (blackFadeImage == null)
            {
                // 若沒設黑幕Image則直接切換
                if (fromPanel != null) fromPanel.SetActive(false);
                if (toPanel != null) toPanel.SetActive(true);
                // 更新 lastActivePanel
                if (toPanel != null) lastActivePanel = toPanel;
                yield break;
            }

            _isFading = true;
            _globalIsFading = true;

            // 暫時提升黑幕 Canvas 到最上層
            if (_blackCanvas != null)
            {
                _blackCanvas.overrideSorting = true;
                _blackCanvas.sortingOrder = 1000;
            }

            blackFadeImage.gameObject.SetActive(true);

            // 控制是否阻擋射線
            if (_blackCanvasGroup != null)
            {
                _blackCanvasGroup.blocksRaycasts = blockInputDuringFade;
                blackFadeImage.raycastTarget = blockInputDuringFade;
            }

            // 淡入
            yield return StartCoroutine(FadeBlack(0f, 1f, fadeDuration));

            // 切換面板
            if (fromPanel != null) fromPanel.SetActive(false);
            if (toPanel != null) toPanel.SetActive(true);

            if (toPanel != null) lastActivePanel = toPanel;

            // 停留（使用 real-time）
            yield return new WaitForSecondsRealtime(blackStayDuration);

            // 淡出
            yield return StartCoroutine(FadeBlack(1f, 0f, fadeDuration));

            // 淡出後取消阻擋
            if (_blackCanvasGroup != null)
            {
                _blackCanvasGroup.blocksRaycasts = false;
                blackFadeImage.raycastTarget = false;
            }

            // 還原 Canvas 排序設定
            if (_blackCanvas != null)
            {
                _blackCanvas.overrideSorting = _blackCanvasPrevOverride;
                _blackCanvas.sortingOrder = _blackCanvasPrevOrder;
            }

            // 保持 blackFadeImage active 但 alpha=0
            Color c = blackFadeImage.color;
            blackFadeImage.color = new Color(c.r, c.g, c.b, 0f);

            _isFading = false;
            _globalIsFading = false;
        }

        private IEnumerator FadeAndSwitchPanelWithText(GameObject fromPanel, GameObject toPanel, string message)
        {
            if (blackFadeImage == null)
            {
                // fallback: 直接切換並設定文字（若有）
                if (blackFadeText != null)
                {
                    blackFadeText.text = message;
                    blackFadeText.color = new Color(blackFadeText.color.r, blackFadeText.color.g, blackFadeText.color.b, 1f);
                }
                if (fromPanel != null) fromPanel.SetActive(false);
                if (toPanel != null) toPanel.SetActive(true);
                yield break;
            }

            _isFading = true;
            _globalIsFading = true;

            // 暫時提升黑幕 Canvas 到最上層
            if (_blackCanvas != null)
            {
                _blackCanvas.overrideSorting = true;
                _blackCanvas.sortingOrder = 1000;
            }

            blackFadeImage.gameObject.SetActive(true);

            // 控制是否阻擋射線
            if (_blackCanvasGroup != null)
            {
                _blackCanvasGroup.blocksRaycasts = blockInputDuringFade;
                blackFadeImage.raycastTarget = blockInputDuringFade;
            }

            // 確保文字存在時初始化文字 alpha = 0 並設定訊息
            if (blackFadeText != null)
            {
                blackFadeText.gameObject.SetActive(true);
                blackFadeText.text = message ?? "";
                Color tc = blackFadeText.color;
                blackFadeText.color = new Color(tc.r, tc.g, tc.b, 0f);
            }

            // 1) 先淡入黑幕（完全不透明）
            yield return StartCoroutine(FadeBlack(0f, 1f, fadeDuration));

            // 2) 黑幕完全不透明後，再讓文字淡入到可見
            if (blackFadeText != null)
                yield return StartCoroutine(FadeText(0f, 1f, textFadeDuration));

            // 3) 切換面板（在文字已經顯示的狀態下切換）
            if (fromPanel != null) fromPanel.SetActive(false);
            if (toPanel != null) toPanel.SetActive(true);
            if (toPanel != null) lastActivePanel = toPanel;

            // 4) 停留（使用 real-time）
            yield return new WaitForSecondsRealtime(blackStayDuration);

            // 5) 先淡出文字（文字完全淡出後才淡出黑幕）
            if (blackFadeText != null)
                yield return StartCoroutine(FadeText(1f, 0f, textFadeDuration));

            // 6) 文字淡出完成後再淡出黑幕
            yield return StartCoroutine(FadeBlack(1f, 0f, fadeDuration));

            // 淡出後取消阻擋
            if (_blackCanvasGroup != null)
            {
                _blackCanvasGroup.blocksRaycasts = false;
                blackFadeImage.raycastTarget = false;
            }

            // 還原 Canvas 排序設定
            if (_blackCanvas != null)
            {
                _blackCanvas.overrideSorting = _blackCanvasPrevOverride;
                _blackCanvas.sortingOrder = _blackCanvasPrevOrder;
            }

            // 保持 blackFadeImage active 但 alpha=0；文字同樣設為 alpha=0
            Color c = blackFadeImage.color;
            blackFadeImage.color = new Color(c.r, c.g, c.b, 0f);
            if (blackFadeText != null)
            {
                Color tc = blackFadeText.color;
                blackFadeText.color = new Color(tc.r, tc.g, tc.b, 0f);
            }

            _isFading = false;
            _globalIsFading = false;
        }

        // 幫助：同時啟動兩個 IEnumerator（其中一個可為 null）
        private IEnumerator RunParallel(IEnumerator a, IEnumerator b)
        {
            if (a == null && b == null) yield break;
            if (a == null)
            {
                yield return StartCoroutine(b);
                yield break;
            }
            if (b == null)
            {
                yield return StartCoroutine(a);
                yield break;
            }

            // 以兩個 coroutine 並行推進
            bool aDone = false, bDone = false;
            var aEnum = a;
            var bEnum = b;
            // 啟動 both
            var aRoutine = StartCoroutine(Advance(aEnum, () => aDone = true));
            var bRoutine = StartCoroutine(Advance(bEnum, () => bDone = true));
            // 等待兩者完成
            while (!aDone || !bDone)
                yield return null;
        }

        // 進階：執行一個 IEnumerator 並在完成時呼叫 callback
        private IEnumerator Advance(IEnumerator enumerator, System.Action onDone)
        {
            yield return StartCoroutine(enumerator);
            onDone?.Invoke();
        }

        private IEnumerator FadeBlack(float from, float to, float duration)
        {
            if (blackFadeImage == null) yield break;

            float t = 0f;
            Color baseColor = new Color(blackFadeImage.color.r, blackFadeImage.color.g, blackFadeImage.color.b, 0f);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                blackFadeImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }
            blackFadeImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, to);
        }

        private IEnumerator FadeText(float from, float to, float duration)
        {
            if (blackFadeText == null) yield break;

            float t = 0f;
            Color baseColor = new Color(blackFadeText.color.r, blackFadeText.color.g, blackFadeText.color.b, 0f);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                blackFadeText.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }
            blackFadeText.color = new Color(baseColor.r, baseColor.g, baseColor.b, to);
        }

        // -------- 新增：立即切換版本（無轉場） --------
        public void ShowGameImmediate()
        {
            SetPanelOrder(gamePanel, 2);
            SetPanelOrder(mainMenuPanel, 1);
            SetPanelOrder(settingsPanel, 1);

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (gamePanel != null) gamePanel.SetActive(true);

            lastActivePanel = gamePanel;
            if (overlaySettingsButton != null)
                overlaySettingsButton.SetActive(true);
        }

        public void ShowSettingsImmediate()
        {
            SetPanelOrder(settingsPanel, 2);
            SetPanelOrder(mainMenuPanel, 1);
            SetPanelOrder(gamePanel, 1);

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
            if (gamePanel != null) gamePanel.SetActive(false);

            if (overlaySettingsButton != null)
                overlaySettingsButton.SetActive(true);
        }

        public void BackFromSettingsImmediate()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (panelBeforeSettings == mainMenuPanel)
            {
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                SetPanelOrder(mainMenuPanel, 2);
                lastActivePanel = mainMenuPanel;
            }
            else if (panelBeforeSettings == gamePanel)
            {
                if (gamePanel != null) gamePanel.SetActive(true);
                SetPanelOrder(gamePanel, 2);
                lastActivePanel = gamePanel;
            }

            if (overlaySettingsButton != null)
                overlaySettingsButton.SetActive(true);
        }

        // -------- 新增：帶轉場版本（有轉場） --------
        public void ShowGameWithFade()
        {
            var from = GetCurrentlyActivePanel() ?? lastActivePanel ?? mainMenuPanel;
            FadeSwitchPanel(from, gamePanel);
        }

        public void ShowSettingsWithFade()
        {
            var from = GetCurrentlyActivePanel() ?? lastActivePanel ?? mainMenuPanel;
            panelBeforeSettings = from;
            FadeSwitchPanel(from, settingsPanel);
        }

        public void BackFromSettingsWithFade()
        {
            if (panelBeforeSettings != null)
            {
                FadeSwitchPanel(settingsPanel, panelBeforeSettings);
                lastActivePanel = panelBeforeSettings;
            }
            else
            {
                // fallback
                ShowMainMenu();
            }
        }

        // 原有方法：依 blackFadeImage 存在與否選擇（保留相容）
        public void ShowGame()
        {
            if (blackFadeImage != null) ShowGameWithFade();
            else ShowGameImmediate();
        }

        public void ShowSettings()
        {
            if (blackFadeImage != null) ShowSettingsWithFade();
            else ShowSettingsImmediate();
        }

        public void BackFromSettings()
        {
            if (blackFadeImage != null) BackFromSettingsWithFade();
            else BackFromSettingsImmediate();
        }

        // 新增：ShowMainMenu（供 fallback 與外部呼叫）
        public void ShowMainMenu()
        {
            var from = GetCurrentlyActivePanel() ?? lastActivePanel ?? mainMenuPanel;
            if (blackFadeImage != null)
                FadeSwitchPanel(from, mainMenuPanel);
            else
                InitializePanels();
        }

        // 主選單-開始遊戲（按鈕綁定）
        public void OnClickStartGame()
        {
            FadeSwitchPanel(GetCurrentlyActivePanel() ?? mainMenuPanel, gamePanel);
            lastActivePanel = gamePanel;
            if (overlaySettingsButton != null)
                overlaySettingsButton.SetActive(true);
        }

        // 主選單-設定（按鈕綁定）
        public void OnClickMainMenuSettings()
        {
            panelBeforeSettings = GetCurrentlyActivePanel() ?? mainMenuPanel;
            FadeSwitchPanel(panelBeforeSettings, settingsPanel);
            if (overlaySettingsButton != null)
                overlaySettingsButton.SetActive(true);
        }

        // 遊戲畫面-設定（按鈕綁定）
        public void OnClickGameSettings()
        {
            panelBeforeSettings = GetCurrentlyActivePanel() ?? gamePanel;
            FadeSwitchPanel(panelBeforeSettings, settingsPanel);
            if (overlaySettingsButton != null)
                overlaySettingsButton.SetActive(true);
        }

        // 工具：設定Panel的Canvas排序
        private void SetPanelOrder(GameObject panel, int order)
        {
            if (panel == null) return;
            var canvas = panel.GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = order;
        }

        // 閉設定介面（返回遊戲）
        public void BackToGame()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (gamePanel != null)
            {
                gamePanel.SetActive(true);
                SetPanelOrder(gamePanel, 2); // 確保遊戲介面在最上層
            }

            lastActivePanel = gamePanel;

            if (overlaySettingsButton != null)
                overlaySettingsButton.SetActive(true);
        }

        // 退出遊戲
        public void ExitGame()
        {
            Application.Quit();
        }

        // 音量調整
        public void SetVolume(float value)
        {
            AudioListener.volume = value;
        }
    }
}