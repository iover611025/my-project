using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class SwipeUpToSwitchObject : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("切換設定")]
    [Tooltip("要依序切換的遊戲物件陣列")]
    public GameObject[] targetObjects;
    
    [Tooltip("上滑判斷的最小距離（單位：螢幕高度的比例，0.05 = 5%，適用所有解析度）")]
    [Range(0.01f, 0.5f)]
    public float swipeThresholdRatio = 0.05f;
    
    [Tooltip("淡入淡出的總時間 (秒)")]
    public float fadeDuration = 1.0f;

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
                // 尋找場景中目前已經是顯示狀態的物件，當作起始索引
                if (!foundActive && targetObjects[i].activeSelf)
                {
                    currentIndex = i;
                    foundActive = true;
                }
                
                // 動態加入 CanvasGroup 讓 UI 物件可以調整透明度
                if (targetObjects[i].GetComponent<CanvasGroup>() == null)
                {
                    targetObjects[i].AddComponent<CanvasGroup>();
                }
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 統一使用螢幕座標（eventData.position 永遠是螢幕像素，與 Canvas 模式無關）
        pointerDownPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isFading || targetObjects.Length <= 1) return;

        Vector2 pointerUpPosition = eventData.position;
        float deltaY = pointerUpPosition.y - pointerDownPosition.y;

        // 使用螢幕高度的比例作為閾值，不受解析度與 Canvas Scale 影響
        float threshold = Screen.height * swipeThresholdRatio;

        if (deltaY > threshold)
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

        // 1. 淡出當前物件
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
        
        // 隱藏當前物件並顯示下一個物件
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
