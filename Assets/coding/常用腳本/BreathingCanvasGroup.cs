using UnityEngine;

/// <summary>
/// CanvasGroup 呼吸效果
/// 完整流程：正常停留 → 漸變（顯示↔透明）→ 正常停留 → 漸變 → 循環
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class BreathingCanvasGroup : MonoBehaviour
{
    private enum State { Hold, Fade }

    [Header("時間設定（秒，支援小數）")]
    [Tooltip("顯示/透明時的正常停留秒數")]
    [Min(0f)] public float holdDuration = 1f;

    [Tooltip("一次漸變的秒數（顯示→透明 或 透明→顯示）")]
    [Min(0.01f)] public float fadeDuration = 1f;

    [Header("Alpha 範圍")]
    [Range(0f, 1f)] public float minAlpha = 0f;
    [Range(0f, 1f)] public float maxAlpha = 1f;

    [Header("其他")]
    [Tooltip("啟動時立即開始")]
    public bool playOnAwake = true;

    // ── 內部狀態 ──────────────────────────────────────────────
    private CanvasGroup _canvasGroup;
    private State _state;
    private float _stateElapsed;
    private bool _isPlaying;

    // 追蹤目前漸變方向（顯示→透明 或 透明→顯示）
    private bool _fadingOut = true;

    // ── Unity 生命週期 ────────────────────────────────────────
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        if (playOnAwake)
            Play();
        else
            _canvasGroup.alpha = maxAlpha;
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _stateElapsed += Time.deltaTime;

        switch (_state)
        {
            case State.Hold:
                if (_stateElapsed >= holdDuration)
                    EnterState(State.Fade);
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
                        _fadingOut = !_fadingOut; // 下次反向
                        EnterState(State.Hold);
                    }
                }
                break;
        }
    }

    // ── 狀態切換 ──────────────────────────────────────────────
    private void EnterState(State next)
    {
        _state = next;
        _stateElapsed = 0f;
    }

    // ── 公開控制介面 ──────────────────────────────────────────
    /// <summary>從「顯示停留」開始播放</summary>
    public void Play()
    {
        _fadingOut = true;
        _canvasGroup.alpha = maxAlpha;
        EnterState(State.Hold);
        _isPlaying = true;
    }

    /// <summary>暫停（保留目前 alpha）</summary>
    public void Pause() => _isPlaying = false;

    /// <summary>繼續（從暫停處接續）</summary>
    public void Resume() => _isPlaying = true;

    /// <summary>停止並重置為 maxAlpha</summary>
    public void Stop()
    {
        _isPlaying = false;
        if (_canvasGroup != null)
            _canvasGroup.alpha = maxAlpha;
    }

    /// <summary>重新從頭播放</summary>
    public void Restart() => Play();
}
