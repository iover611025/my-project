using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 滑動方向設定
/// </summary>
public enum SwipeDirection
{
    /// <summary>由下往上滑（預設）</summary>
    BottomToTop,
    /// <summary>由上往下滑</summary>
    TopToBottom,
    /// <summary>由左往右滑</summary>
    LeftToRight,
    /// <summary>由右往左滑</summary>
    RightToLeft
}

public class SwipeUpToSwitchObject : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("設定")]
    [Tooltip("要輪播切換的物件陣列")]
    public GameObject[] targetObjects;
    
    [Tooltip("滑動判斷最小距離（比例，相對於螢幕解析度，0.05 = 5%，不受解析度影響）")]
    [Range(0.01f, 0.5f)]
    public float swipeThresholdRatio = 0.05f;
    
    [Tooltip("淡入淡出總時間 (秒)")]
    public float fadeDuration = 1.0f;

    [Tooltip("觸發切換所需的滑動方向")]
    public SwipeDirection swipeDirection = SwipeDirection.BottomToTop;

    private int currentIndex = 0;
    private Vector2 pointerDownPosition;
    private bool isFading = false;

    private void Start()
    {
        bool foundActive = false;
        
        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] != null)
            {
                // 記錄當前已經是啟用的，一起初始
                if (!foundActive && targetObjects[i].activeSelf)
                {
                    currentIndex = i;
                    foundActive = true;
                }
                
                // 動態加入 CanvasGroup 以讓 UI 可以淡入淡出
                if (targetObjects[i].GetComponent<CanvasGroup>() == null)
                {
                    targetObjects[i].AddComponent<CanvasGroup>();
                }
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 記錄第一個按下的位置（eventData.position 單位是像素，與 Canvas 縮放無關）
        pointerDownPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isFading || targetObjects.Length <= 1) return;

        Vector2 pointerUpPosition = eventData.position;
        Vector2 delta = pointerUpPosition - pointerDownPosition;

        // 根據方向取對應軸的差值，並決定需要大於還是小於零
        float signedDelta;
        float screenReference;

        switch (swipeDirection)
        {
            case SwipeDirection.BottomToTop:
                // 手指由下往上：Y 增加
                signedDelta = delta.y;
                screenReference = Screen.height;
                break;
            case SwipeDirection.TopToBottom:
                // 手指由上往下：Y 減少 → 取負值使其為正
                signedDelta = -delta.y;
                screenReference = Screen.height;
                break;
            case SwipeDirection.LeftToRight:
                // 手指由左往右：X 增加
                signedDelta = delta.x;
                screenReference = Screen.width;
                break;
            case SwipeDirection.RightToLeft:
                // 手指由右往左：X 減少 → 取負值使其為正
                signedDelta = -delta.x;
                screenReference = Screen.width;
                break;
            default:
                signedDelta = delta.y;
                screenReference = Screen.height;
                break;
        }

        // 使用螢幕解析度比例作為閾值，不受縮放影響
        float threshold = screenReference * swipeThresholdRatio;

        if (signedDelta > threshold)
        {
            SwitchToNextObject();
        }
    }

    private void SwitchToNextObject()
    {
        int nextIndex = (currentIndex + 1) % targetObjects.Length;
        StartCoroutine(FadeToNextObject(targetObjects[currentIndex], targetObjects[nextIndex]));
        currentIndex = nextIndex;
    }

    private IEnumerator FadeToNextObject(GameObject currentObj, GameObject nextObj)
    {
        isFading = true;
        float halfDuration = fadeDuration / 2f;

        CanvasGroup currentCanvasGroup = currentObj.GetComponent<CanvasGroup>();
        CanvasGroup nextCanvasGroup = nextObj.GetComponent<CanvasGroup>();

        // 1. 淡出前景
        if (currentCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                currentCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / halfDuration);
                yield return null;
            }
            currentCanvasGroup.alpha = 0f;
        }
        
        // 切換到下一個物件
        currentObj.SetActive(false);
        nextObj.SetActive(true);

        // 2. 淡入下一個物件
        if (nextCanvasGroup != null)
        {
            nextCanvasGroup.alpha = 0f;
            float timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                nextCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / halfDuration);
                yield return null;
            }
            nextCanvasGroup.alpha = 1f;
        }

        isFading = false;
    }
}
