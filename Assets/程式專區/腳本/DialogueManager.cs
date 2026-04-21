using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

namespace X
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Prefab 配置")]
        public GameObject dialoguePrefab;
        public Transform uiParent;

        private RectTransform dialogRT;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI textDisplay;
        private Coroutine currentFlow;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            InitPrefab();
        }

        private void InitPrefab()
        {
            if (dialoguePrefab == null || uiParent == null) return;

            GameObject go = Instantiate(dialoguePrefab, uiParent);
            dialogRT = go.GetComponent<RectTransform>();
            canvasGroup = go.GetComponent<CanvasGroup>();
            textDisplay = go.GetComponentInChildren<TextMeshProUGUI>();

            // 自動加上 Button 功能，讓玩家點擊對話框本身就能關閉
            Button btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None; // 鏽湖風格通常不需要按鈕跳色
            btn.onClick.AddListener(ForceCloseDialogue);

            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
        }

        // 核心顯示方法：接收文字、時間、以及座標
        public void ShowDialogue(string message, float duration, Vector2 position)
        {
            if (currentFlow != null) StopCoroutine(currentFlow);

            // 設定位置 (相對於 UI 錨點)
            if (dialogRT != null) dialogRT.anchoredPosition = position;

            textDisplay.text = message;
            currentFlow = StartCoroutine(DialogueProcess(duration));
        }

        private IEnumerator DialogueProcess(float duration)
        {
            canvasGroup.blocksRaycasts = true;
            yield return Fade(1f, 0.2f);

            // 這裡等待自訂的時間長度
            yield return new WaitForSeconds(duration);

            yield return Fade(0f, 0.4f);
            canvasGroup.blocksRaycasts = false;
        }

        // 當切換場景、按下返回、或點擊對話框時呼叫
        public void ForceCloseDialogue()
        {
            if (currentFlow != null) StopCoroutine(currentFlow);
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
        }

        private IEnumerator Fade(float target, float time)
        {
            float start = canvasGroup.alpha;
            float elapsed = 0;
            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / time);
                yield return null;
            }
            canvasGroup.alpha = target;
        }
    }
}