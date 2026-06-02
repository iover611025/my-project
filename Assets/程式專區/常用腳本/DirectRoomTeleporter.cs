using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace X
{
    public class DirectRoomTeleporter : MonoBehaviour
    {
        public static DirectRoomTeleporter Instance { get; private set; }

        public enum TransitionType { Simple, WithText }

        [Header("轉場類型設定")]
        public TransitionType transitionType = TransitionType.Simple;
        [TextArea(2, 3)] public string transitionMessage = "正在移動中...";

        [Header("黑幕動畫元件設定")]
        public Image blackFadeImage;
        public float fadeDuration = 0.5f;
        public float blackStayDuration = 0.8f;

        [Header("黑幕文字元件（選填）")]
        public Text blackFadeText;
        public float textFadeDuration = 0.4f;

        [Header("目標傳送設定")]
        [SerializeField] private int targetBigSceneId;    // 畫面上現有的欄位
        [SerializeField] private int targetRoomIndex;     // 畫面上現有的欄位

        private bool _isSwitching = false;

        void Awake()
        {
            if (Instance == null) { Instance = this; }
            else { Destroy(gameObject); return; }

            if (blackFadeImage != null)
            {
                blackFadeImage.gameObject.SetActive(false);
                Color c = blackFadeImage.color; c.a = 0f; blackFadeImage.color = c;
            }
            if (blackFadeText != null)
            {
                blackFadeText.gameObject.SetActive(false);
                Color tc = blackFadeText.color; tc.a = 0f; blackFadeText.color = tc;
            }
        }

        public static void Execute(int targetBigSceneId, int targetRoomIndex)
        {
            if (Instance != null) { Instance.StartTeleport(targetBigSceneId, targetRoomIndex); }
        }

        public void StartTeleport(int targetBigSceneId, int targetRoomIndex)
        {
            if (_isSwitching) return;
            if (RoomUIManager.Instance == null) return;
            StartCoroutine(TeleportRoutine(targetBigSceneId, targetRoomIndex));
        }

        private IEnumerator TeleportRoutine(int sceneId, int roomIndex)
        {
            _isSwitching = true;
            if (blackFadeImage != null)
            {
                blackFadeImage.gameObject.SetActive(true);
                yield return StartCoroutine(FadeImageAlpha(blackFadeImage, 0f, 1f, fadeDuration));
            }
            if (transitionType == TransitionType.WithText && blackFadeText != null)
            {
                blackFadeText.text = transitionMessage;
                blackFadeText.gameObject.SetActive(true);
                yield return StartCoroutine(FadeTextAlpha(blackFadeText, 0f, 1f, textFadeDuration));
            }

            // 核心切換
            RoomUIManager.Instance.TransitionToBigScene(sceneId, roomIndex);

            yield return new WaitForSeconds(blackStayDuration);

            if (blackFadeText != null && blackFadeText.gameObject.activeSelf)
            {
                yield return StartCoroutine(FadeTextAlpha(blackFadeText, 1f, 0f, textFadeDuration));
                blackFadeText.gameObject.SetActive(false);
            }
            if (blackFadeImage != null)
            {
                yield return StartCoroutine(FadeImageAlpha(blackFadeImage, 1f, 0f, fadeDuration));
                blackFadeImage.gameObject.SetActive(false);
            }
            _isSwitching = false;
        }

        private IEnumerator FadeImageAlpha(Image img, float startAlpha, float targetAlpha, float duration)
        {
            float elapsed = 0f; Color c = img.color;
            while (elapsed < duration) { elapsed += Time.deltaTime; c.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration); img.color = c; yield return null; }
            c.a = targetAlpha; img.color = c;
        }

        private IEnumerator FadeTextAlpha(Text txt, float startAlpha, float targetAlpha, float duration)
        {
            float elapsed = 0f; Color c = txt.color;
            while (elapsed < duration) { elapsed += Time.deltaTime; c.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration); txt.color = c; yield return null; }
            c.a = targetAlpha; txt.color = c;
        }

        // ==================== 修改重點在下方 ====================
        /// <summary>
        /// 提供給 Unity UI Button (OnClick) 綁定的無參數方法
        /// </summary>
        public void Teleport()
        {
            // 這樣就會完美直接讀取你畫面上原本填好的 1 和 2 囉！
            StartTeleport(targetBigSceneId, targetRoomIndex);
        }
    }
}