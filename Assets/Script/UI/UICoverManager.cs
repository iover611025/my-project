using UnityEngine;
using UnityEngine.UI;

namespace X
{
    public class UICoverManager : MonoBehaviour
    {
        public GameObject mainMenuPanel;
        public GameObject settingsPanel;
        public GameObject gamePanel;
        public GameObject overlaySettingsButton; // 設定按鈕（獨立，不會被隱藏）
        public Slider volumeSlider;

        private GameObject lastActivePanel; // 新增：記錄上一個顯示的Panel
        private GameObject panelBeforeSettings; // 新增：記錄進入設定前的Panel

        void Start()
        {
            ShowMainMenu();
            if (volumeSlider != null)
                volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        // 顯示主介面（覆蓋其他）
        public void ShowMainMenu()
        {
            SetPanelOrder(mainMenuPanel, 2);
            SetPanelOrder(settingsPanel, 1);
            SetPanelOrder(gamePanel, 1);

            mainMenuPanel.SetActive(true);
            settingsPanel.SetActive(false);
            gamePanel.SetActive(false);
            lastActivePanel = mainMenuPanel;
            if (overlaySettingsButton != null)
                overlaySettingsButton.SetActive(true);
        }

        // 顯示遊戲介面
        public void ShowGame()
        {
            SetPanelOrder(gamePanel, 2);
            SetPanelOrder(mainMenuPanel, 1);
            SetPanelOrder(settingsPanel, 1);

            mainMenuPanel.SetActive(false);
            settingsPanel.SetActive(false);
            gamePanel.SetActive(true);
            lastActivePanel = gamePanel;
            if (overlaySettingsButton != null)
                overlaySettingsButton.SetActive(true);
        }

        // 顯示設定介面，並記錄來源Panel
        public void ShowSettings()
        {
            if (mainMenuPanel.activeSelf)
                panelBeforeSettings = mainMenuPanel;
            else if (gamePanel.activeSelf)
                panelBeforeSettings = gamePanel;
            else
                panelBeforeSettings = null;

            SetPanelOrder(settingsPanel, 2);
            SetPanelOrder(mainMenuPanel, 1);
            SetPanelOrder(gamePanel, 1);

            mainMenuPanel.SetActive(false);
            settingsPanel.SetActive(true);
            gamePanel.SetActive(false);
            if (overlaySettingsButton != null)
                overlaySettingsButton.SetActive(true);
        }

        // 返回設定前的介面
        public void BackFromSettings()
        {
            settingsPanel.SetActive(false);

            if (panelBeforeSettings == mainMenuPanel)
            {
                mainMenuPanel.SetActive(true);
                SetPanelOrder(mainMenuPanel, 2);
                lastActivePanel = mainMenuPanel;
            }
            else if (panelBeforeSettings == gamePanel)
            {
                gamePanel.SetActive(true);
                SetPanelOrder(gamePanel, 2);
                lastActivePanel = gamePanel;
            }

            if (overlaySettingsButton != null)
                overlaySettingsButton.SetActive(true);
        }

        // 降低設定介面優先級（上一個介面取代setting位置）
        public void LowerSettingsPriority()
        {
            var settingsCanvas = settingsPanel.GetComponent<Canvas>();
            if (settingsCanvas != null)
                settingsCanvas.sortingOrder = 1; // 降低設定Panel順位

            if (lastActivePanel != null)
            {
                var lastCanvas = lastActivePanel.GetComponent<Canvas>();
                if (lastCanvas != null)
                    lastCanvas.sortingOrder = 2; // 提升上一個Panel順位
            }
        }

        // 工具：設定Panel的Canvas排序
        private void SetPanelOrder(GameObject panel, int order)
        {
            if (panel == null) return;
            var canvas = panel.GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = order;
        }

        // 關閉設定介面（返回遊戲）
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