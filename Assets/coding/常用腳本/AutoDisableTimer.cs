using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace X
{
    /// <summary>
    /// 自動/點擊切換或隱藏目標物件。
    /// 支援計時器、點擊觸發、小數點秒數、黑幕淡入/停留/淡出轉場。
    /// 黑幕 Coroutine 在獨立的 Canvas GameObject 上執行，
    /// 因此即使目標物件是自身也不會因 inactive 而中斷。
    /// </summary>
    public class AutoDisableTimer : MonoBehaviour, IPointerClickHandler
    {
        public enum TriggerMode
        {
            Timer,   // 僅時間到觸發
            Click,   // 僅點擊觸發
            Both     // 時間到或點擊都觸發
        }

        [Header("觸發設定")]
        [Tooltip("觸發此切換效果的方式：Timer (時間到自動觸發)、Click (點擊此物件觸發)、Both (兩者皆可)")]
        public TriggerMode triggerMode = TriggerMode.Timer;

        [Header("計時設定")]
        public float duration = 2f;
        public bool isRepeating = false;

        [Header("目標設定")]
        public GameObject targetObject;

        [Tooltip("計時結束或觸發時，要同時被開啟（SetActive true）並各自開啟的物件。")]
        public GameObject[] objectsToEnableOnEnd;

        [Header("黑幕轉場")]
        [Tooltip("是否在觸發時使用黑幕轉場效果")]
        public bool useBlackFade = false;
        [Tooltip("黑幕淡入時間（透明 → 全黑），單位：秒")]
        public float fadeInDuration = 0.3f;
        [Tooltip("全黑停留時間（切換物件發生在這段期間的中點），單位：秒")]
        public float blackStayDuration = 0.1f;
        [Tooltip("黑幕淡出時間（全黑 → 透明），單位：秒")]
        public float fadeOutDuration = 0.3f;

        [Header("計時暫停設定")]
        [Tooltip("計時期間要被暫停並隱藏的 CanvasGroup")]
        public CanvasGroup pausedDuringTimer;

        [Header("自訂事件輸出")]
        [Tooltip("計時/觸發開始時產生的事件（可用來呼叫 ShakeFeedback 或播放音效）")]
        public UnityEvent onTimerStart;

        // ── 黑幕用：獨立的 Canvas 與 Helper MonoBehaviour ──
        private FadeRunner _fadeRunner;

        // ── 狀態控制與協程 ──
        private WaitForSeconds _cachedWait;
        private Coroutine _timerCoroutine;
        private bool _isProcessing = false;

        private void Reset()
        {
            // Inspector 新增元件時，直接預設為自身 GameObject
            targetObject = gameObject;
        }

        private void Awake()
        {
            if (targetObject == null) targetObject = gameObject;
            _cachedWait = new WaitForSeconds(duration);

            if (useBlackFade)
                _fadeRunner = FadeRunner.GetOrCreate();
        }

        private void OnEnable()
        {
            SetCanvasGroupPaused(true);
            onTimerStart?.Invoke();
            _isProcessing = false;

            if (triggerMode == TriggerMode.Timer || triggerMode == TriggerMode.Both)
            {
                _timerCoroutine = StartCoroutine(TimerRoutine());
            }
        }

        private void OnDisable()
        {
            SetCanvasGroupPaused(false);
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        /// <summary>
        /// 點擊事件介面實作
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (triggerMode == TriggerMode.Timer) return;
            TriggerAction();
        }

        /// <summary>
        /// 主動觸發切換（點擊或外部呼叫）
        /// </summary>
        public void TriggerAction()
        {
            if (_isProcessing) return;
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(ExecuteSwitchRoutine(fromTimer: false));
        }

        private IEnumerator ExecuteSwitchRoutine(bool fromTimer = false)
        {
            _isProcessing = true;

            // 如果是點擊觸發，需中斷原本正在倒數的計時器
            if (!fromTimer && _timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            if (isRepeating)
            {
                if (useBlackFade)
                {
                    bool done = false;
                    _fadeRunner.RunFadeSequence(
                        fadeIn:    fadeInDuration,
                        stay:      blackStayDuration,
                        fadeOut:   fadeOutDuration,
                        onMidpoint: () => targetObject.SetActive(!targetObject.activeSelf),
                        onComplete: () => done = true
                    );
                    yield return new WaitUntil(() => done);
                }
                else
                {
                    targetObject.SetActive(!targetObject.activeSelf);
                }

                _isProcessing = false;

                // 如果是點擊觸發且有包含計時模式，重新啟動計時器以重新倒數
                if (!fromTimer && (triggerMode == TriggerMode.Timer || triggerMode == TriggerMode.Both))
                {
                    _timerCoroutine = StartCoroutine(TimerRoutine());
                }
            }
            else
            {
                SetCanvasGroupPaused(false);

                if (useBlackFade)
                {
                    bool done = false;
                    _fadeRunner.RunFadeSequence(
                        fadeIn:    fadeInDuration,
                        stay:      blackStayDuration,
                        fadeOut:   fadeOutDuration,
                        onMidpoint: () =>
                        {
                            targetObject.SetActive(false);
                            if (objectsToEnableOnEnd != null)
                                foreach (var obj in objectsToEnableOnEnd)
                                    if (obj != null) obj.SetActive(true);
                        },
                        onComplete: () => done = true
                    );
                    yield return new WaitUntil(() => done);
                }
                else
                {
                    targetObject.SetActive(false);
                    if (objectsToEnableOnEnd != null)
                        foreach (var obj in objectsToEnableOnEnd)
                            if (obj != null) obj.SetActive(true);
                }

                _isProcessing = false;
            }
        }

        private IEnumerator TimerRoutine()
        {
            while (true)
            {
                yield return _cachedWait;

                if (!_isProcessing)
                {
                    yield return StartCoroutine(ExecuteSwitchRoutine(fromTimer: true));
                }

                if (!isRepeating)
                {
                    yield break;
                }
            }
        }

        private void SetCanvasGroupPaused(bool paused)
        {
            if (pausedDuringTimer == null) return;
            pausedDuringTimer.alpha          = paused ? 0f : 1f;
            pausedDuringTimer.interactable   = !paused;
            pausedDuringTimer.blocksRaycasts = !paused;
        }
    }

    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// 永遠 active 的輔助 MonoBehaviour，負責執行黑幕 Coroutine。
    /// </summary>
    // ─────────────────────────────────────────────────────────────
    internal class FadeRunner : MonoBehaviour
    {
        private Image _fadeImage;

        public static FadeRunner GetOrCreate()
        {
            var existing = FindObjectOfType<FadeRunner>();
            if (existing != null) return existing;

            var canvasGO = new GameObject("[AutoDisableTimer] BlackFadeOverlay");
            DontDestroyOnLoad(canvasGO);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var imageGO = new GameObject("FadeImage");
            imageGO.transform.SetParent(canvasGO.transform, false);
            var img = imageGO.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = false;

            var rt = imageGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var runner = canvasGO.AddComponent<FadeRunner>();
            runner._fadeImage = img;

            return runner;
        }

        public void RunFadeSequence(
            float fadeIn,
            float stay,
            float fadeOut,
            System.Action onMidpoint,
            System.Action onComplete)
        {
            StartCoroutine(FadeSequenceRoutine(fadeIn, stay, fadeOut, onMidpoint, onComplete));
        }

        private IEnumerator FadeSequenceRoutine(
            float fadeIn,
            float stay,
            float fadeOut,
            System.Action onMidpoint,
            System.Action onComplete)
        {
            yield return FadeBlack(0f, 1f, fadeIn);

            if (stay > 0f)
            {
                yield return new WaitForSecondsRealtime(stay * 0.5f);
                onMidpoint?.Invoke();
                yield return new WaitForSecondsRealtime(stay * 0.5f);
            }
            else
            {
                onMidpoint?.Invoke();
            }

            yield return FadeBlack(1f, 0f, fadeOut);

            onComplete?.Invoke();
        }

        private IEnumerator FadeBlack(float from, float to, float duration)
        {
            if (_fadeImage == null) yield break;

            float t = 0f;
            _fadeImage.color = new Color(0f, 0f, 0f, from);

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                _fadeImage.color = new Color(0f, 0f, 0f, a);
                yield return null;
            }

            _fadeImage.color = new Color(0f, 0f, 0f, to);
        }
    }
}
