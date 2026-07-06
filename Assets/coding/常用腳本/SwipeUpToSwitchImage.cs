using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Image))]
public class SwipeUpToSwitchImage : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("切換設定")]
    [Tooltip("要依序切換的圖片陣列")]
    public Sprite[] sprites;
    
    [Tooltip("上滑判斷的最小距離 (像素)")]
    public float swipeThreshold = 50f;
    
    [Tooltip("淡入淡出的總時間 (秒)")]
    public float fadeDuration = 1.0f;

    private Image image;
    private int currentIndex = 0;
    private Vector2 pointerDownPosition;
    private bool isFading = false;

    private void Awake()
    {
        image = GetComponent<Image>();
        
        // 確保初始有圖片
        if (sprites.Length > 0 && image.sprite == null)
        {
            image.sprite = sprites[0];
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 紀錄按下的起始位置
        pointerDownPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 如果正在淡入淡出，或圖片數量不足以切換，則不處理
        if (isFading || sprites.Length <= 1) return;

        // 取得放開時的位置
        Vector2 pointerUpPosition = eventData.position;
        
        // 計算 Y 軸方向的差值 (正值代表往上滑)
        float deltaY = pointerUpPosition.y - pointerDownPosition.y;

        // 判斷是否為上滑，並且滑動距離超過閾值
        if (deltaY > swipeThreshold)
        {
            SwitchToNextImage();
        }
    }

    private void SwitchToNextImage()
    {
        // 計算下一張圖片的索引 (循環)
        currentIndex = (currentIndex + 1) % sprites.Length;
        StartCoroutine(FadeToNextImage(sprites[currentIndex]));
    }

    private IEnumerator FadeToNextImage(Sprite nextSprite)
    {
        isFading = true;
        Color color = image.color;
        float halfDuration = fadeDuration / 2f;

        // 1. 淡出階段 (Alpha 從 1 -> 0)
        float timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, timer / halfDuration);
            image.color = color;
            yield return null;
        }

        // 換上新的圖片
        image.sprite = nextSprite;

        // 2. 淡入階段 (Alpha 從 0 -> 1)
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, timer / halfDuration);
            image.color = color;
            yield return null;
        }

        // 確保最終 Alpha 值為 1
        color.a = 1f;
        image.color = color;

        isFading = false;
    }
}
