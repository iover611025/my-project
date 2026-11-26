using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// CanvasGroup 呼吸效果
/// 完整流程：淡入 → 顯示停留 → 淡出 → 透明停留 → 循環
/// 可在指定完整週期次數後觸發外部事件（最後一步為透明停留）
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class BreathingCanvasGroup : MonoBehaviour
{
    private enum State { Hold, Fade }

    [Header("時間設定（秒，支援小數）")]
    [Tooltip("一次漸變的秒數（顯示→透明 或 透明→顯示）")]
    [Min(0.01f)] public float fadeDuration = 1f;
    [Tooltip("顯示/透明時的正常停留秒數")]
    [Min(0f)] public float holdDuration = 1f;

    [Header("Alpha 範圍")]
    [Range(0f, 1f)] public float minAlpha = 0f;
    [Range(0f, 1f)] public float maxAlpha = 1f;

    [Header("次數回呼")]
    [Tooltip("完成幾個完整週期後觸發事件（0 = 不觸發）\n一個週期 = 淡入→顯示停留→淡出→透明停留")]
    [Min(0)] public int triggerAfterCycles = 0;

    [Tooltip("達到指定週期後是否停止呼吸")]
    public bool stopAfterTrigger = false;

    [Tooltip("達到指定週期次數後呼叫的外部事件")]
    public UnityEvent onCycleTargetReached;

    [Header("其他")]
    [Tooltip("啟動時立即開始")]
    public bool playOnAwake = true;

    // ── 內部狀態 ──────────────────────────────────────────────
    private CanvasGroup _canvasGroup;
    private State _state;
    private float _stateElapsed;
    private bool _isPlaying;
    private bool _fadingOut = false; // 目前漸變方向（false = 淡入, true = 淡出）
    private int _completedCycles;    // 已完成的完整週期數

    // ── Unity 生命週期 ────────────────────────────────────────
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        if (playOnAwake)
            Play();
        else
            _canvasGroup.alpha = minAlpha;
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _stateElapsed += Time.deltaTime;

        switch (_state)
        {
            case State.Hold:
                if (_stateElapsed >= holdDuration)
                {
                    // _fadingOut == false 代表目前是「透明停留」結束（1 個完整週期完成）
                    if (!_fadingOut)
                    {
                        _completedCycles++;
                        CheckCycleTrigger();
                        if (!_isPlaying) return;
                    }

                    EnterState(State.Fade);
                }
                break;

            case State.Fade:
                {
                    float t = Mathf.Clamp01(_stateElapsed / fadeDuration);
                    _canvasGroup.alpha = _fadingOut
                        ? Mathf.Lerp(maxAlpha, minAlpha, t)
                        : Mathf.Lerp(minAlpha, maxAlpha, t);

                    if (_stateElapsed >= fadeDuration)
                    {
                        _canvasGroup.alpha = _fadingOut ? minAlpha : maxAlpha;
                        _fadingOut = !_fadingOut;
                        EnterState(State.Hold);
                    }
                }
                break;
        }
    }

    // ── 週期回呼檢查 ─────────────────────────────────────────
    private void CheckCycleTrigger()
    {
        if (triggerAfterCycles <= 0) return;
        if (_completedCycles != triggerAfterCycles) return;

        onCycleTargetReached?.Invoke();

        if (stopAfterTrigger)
            Stop();
    }

    // ── 狀態切換 ──────────────────────────────────────────────
    private void EnterState(State next)
    {
        _state = next;
        _stateElapsed = 0f;
    }

    // ── 公開控制介面 ──────────────────────────────────────────
    /// <summary>從「淡入」開始播放，重置週期計數</summary>
    public void Play()
    {
        _fadingOut = false;
        _completedCycles = 0;
        _canvasGroup.alpha = minAlpha;
        EnterState(State.Fade);
        _isPlaying = true;
    }

    /// <summary>暫停（保留目前 alpha）</summary>
    public void Pause() => _isPlaying = false;

    /// <summary>繼續（從暫停處接續）</summary>
    public void Resume() => _isPlaying = true;

    /// <summary>停止並停留在透明狀態 (minAlpha)</summary>
    public void Stop()
    {
        _isPlaying = false;
        if (_canvasGroup != null)
            _canvasGroup.alpha = minAlpha;
    }

    /// <summary>重新從頭播放（週期計數歸零）</summary>
    public void Restart() => Play();

    /// <summary>取得目前已完成的週期數</summary>
    public int CompletedCycles => _completedCycles;
}
