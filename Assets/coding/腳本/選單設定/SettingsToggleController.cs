using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace X
{
    public class SettingsToggleController : MonoBehaviour
    {
        [Header("Toggle 組件")]
        [SerializeField] private Toggle _toggle;
        [SerializeField] private RectTransform _handle; // 開關的小圓球
        [SerializeField] private Image _backgroundImage; // 開關的背景圖

        [Header("連動 Slider")]
        [SerializeField] private Slider _linkedSlider;
        [SerializeField] private CanvasGroup _sliderCanvasGroup; // 用來控制透明度

        [Header("動畫設定")]
        [SerializeField] private float _animationDuration = 0.2f;
        [SerializeField] private Color _onColor = new Color(0.3f, 0.8f, 0.3f); // 綠色
        [SerializeField] private Color _offColor = new Color(0.8f, 0.8f, 0.8f); // 灰色
        
        private Vector2 _handleOnPos;
        private Vector2 _handleOffPos;
        private Coroutine _toggleCoroutine;
        private Coroutine _sliderCoroutine;

        private void Awake()
        {
            // 自動計算小球該滑動的距離（假設初始位置在左邊）
            float toggleWidth = _toggle.GetComponent<RectTransform>().rect.width;
            float handleWidth = _handle.rect.width;
            float moveDistance = (toggleWidth - handleWidth) * 0.5f - 5f; // 稍微扣掉邊距

            _handleOffPos = new Vector2(-moveDistance, 0);
            _handleOnPos = new Vector2(moveDistance, 0);
        }

        private void OnEnable()
        {
            _toggle.onValueChanged.AddListener(OnToggleChanged);
            // 初始化狀態
            ApplyState(_toggle.isOn, false); 
        }

        private void OnToggleChanged(bool isOn) => ApplyState(isOn, true);

        private void ApplyState(bool isOn, bool animate)
        {
            if (_toggleCoroutine != null) StopCoroutine(_toggleCoroutine);
            if (_sliderCoroutine != null) StopCoroutine(_sliderCoroutine);

            if (animate)
            {
                _toggleCoroutine = StartCoroutine(AnimateToggle(isOn));
                _sliderCoroutine = StartCoroutine(AnimateSlider(isOn));
            }
            else
            {
                // 立即更新（初始用）
                _handle.anchoredPosition = isOn ? _handleOnPos : _handleOffPos;
                _backgroundImage.color = isOn ? _onColor : _offColor;
                _linkedSlider.interactable = isOn;
                if (!isOn) _linkedSlider.value = _linkedSlider.minValue;
                _sliderCanvasGroup.alpha = isOn ? 1f : 0.5f;
            }
        }

        IEnumerator AnimateToggle(bool isOn)
        {
            float elapsed = 0;
            Vector2 startPos = _handle.anchoredPosition;
            Vector2 endPos = isOn ? _handleOnPos : _handleOffPos;
            Color startColor = _backgroundImage.color;
            Color endColor = isOn ? _onColor : _offColor;

            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _animationDuration;
                _handle.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                _backgroundImage.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }
        }

        IEnumerator AnimateSlider(bool isOn)
        {
            _linkedSlider.interactable = isOn;
            float elapsed = 0;
            float startAlpha = _sliderCanvasGroup.alpha;
            float endAlpha = isOn ? 1f : 0.5f;
            float startVal = _linkedSlider.value;
            float endVal = isOn ? startVal : _linkedSlider.minValue;

            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _animationDuration;
                _sliderCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                if (!isOn) _linkedSlider.value = Mathf.Lerp(startVal, endVal, t);
                yield return null;
            }
        }
    }
}