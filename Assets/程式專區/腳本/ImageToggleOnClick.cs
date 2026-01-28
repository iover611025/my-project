using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace X
{
    /// <summary>
    /// 點擊時把目標 Image 換成 inspector 指定的另一張 sprite（可單向或可切換回原始 sprite）。
    /// 若未在 Inspector 指定 targetImage，會嘗試使用此 GameObject 上的 Image 元件。
    /// </summary>
    public class ImageToggleOnClick : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("要切換的目標 Image（若留空，會自動取得此 GameObject 的 Image）")]
        public Image targetImage;

        [Tooltip("點擊後要套用的第二張圖（拖入）")]
        public Sprite sprite2;

        [Tooltip("是否允許再次點擊切回原始圖（toggle 行為）。若 false，點擊後僅替換一次。")]
        public bool toggleBack = false;

        private Sprite _originalSprite;
        private bool _isUsingSecond = false;

        void Awake()
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();

            if (targetImage != null)
                _originalSprite = targetImage.sprite;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (targetImage == null || sprite2 == null) return;

            if (toggleBack)
            {
                // 切換回/去第二張圖
                if (_isUsingSecond)
                {
                    targetImage.sprite = _originalSprite;
                    _isUsingSecond = false;
                }
                else
                {
                    targetImage.sprite = sprite2;
                    _isUsingSecond = true;
                }
            }
            else
            {
                // 單向替換：若已是第二張則不做事
                if (!_isUsingSecond)
                {
                    targetImage.sprite = sprite2;
                    _isUsingSecond = true;
                }
            }
        }
    }
}