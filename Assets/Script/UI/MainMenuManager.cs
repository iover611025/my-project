using UnityEngine;
using UnityEngine.UI;

namespace X
{
    public class MainMenuManager : MonoBehaviour
    {
        public GameObject mainMenuPanel;
        public GameObject settingsPanel;
        public Slider volumeSlider;

        void Start()
        {
            ShowMainMenu();
            if (volumeSlider != null)
                volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        public void ShowMainMenu()
        {
            mainMenuPanel.SetActive(true);
            settingsPanel.SetActive(false);
        }

        public void ShowSettings()
        {
            mainMenuPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public void SetVolume(float value)
        {
            AudioListener.volume = value;
        }

        public void BackToGame()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            // 若有主介面或遊戲介面，將其 Canvas 排序提升
            var mainCanvas = mainMenuPanel != null ? mainMenuPanel.GetComponent<Canvas>() : null;
            var gameCanvas = GameObject.Find("GamePanel")?.GetComponent<Canvas>();

            if (mainCanvas != null)
                mainCanvas.sortingOrder = 1; // 主介面在上層
            if (gameCanvas != null)
                gameCanvas.sortingOrder = 1; // 遊戲介面在上層

            // 你可根據需求選擇顯示哪個介面
        }
    }
}   