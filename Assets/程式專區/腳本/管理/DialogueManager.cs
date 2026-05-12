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

        [Header("固定位置設定")]
        // 在 Inspector 設定對話框出現的固定座標 (例如 0, -400)
        public Vector2 fixedPosition = new Vector2(0, -400);

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

            Button btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(ForceCloseDialogue);

            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;

            // 初始化時就位移到固定位置
            dialogRT.anchoredPosition = fixedPosition;
        }

        // 修改：讓原本的 ShowDialogue 支援「不傳入座標就使用固定位置」
        public void ShowDialogue(string message, float duration, Vector2? position = null)
        {
            if (currentFlow != null) StopCoroutine(currentFlow);

            // 如果有傳座標就用傳入的，沒有就用 fixedPosition
            if (dialogRT != null)
                dialogRT.anchoredPosition = position ?? fixedPosition;

            textDisplay.text = message;
            currentFlow = StartCoroutine(DialogueProcess(duration));
        }

        private IEnumerator DialogueProcess(float duration)
        {
            canvasGroup.blocksRaycasts = true;
            yield return Fade(1f, 0.2f);
            yield return new WaitForSeconds(duration);
            yield return Fade(0f, 0.4f);
            canvasGroup.blocksRaycasts = false;
        }

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