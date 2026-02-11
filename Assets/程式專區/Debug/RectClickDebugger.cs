using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RectClickDebugger : MonoBehaviour, IPointerClickHandler
{
    public Image targetImage;

    void Reset()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    void Update()
    {
        if (targetImage == null) return;
        var rt = targetImage.rectTransform;
        // rect in screen space: corners
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        // world to screen
        Vector2 bl = RectTransformUtility.WorldToScreenPoint(targetImage.canvas?.worldCamera, corners[0]);
        Vector2 tr = RectTransformUtility.WorldToScreenPoint(targetImage.canvas?.worldCamera, corners[2]);
        Vector2 size = tr - bl;
        Vector2 center = bl + size * 0.5f;
        // 每秒���出一次（避免刷太多 log）
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[RectClickDebugger] screen center={center}, size={size}, rectBl={bl}, rectTr={tr}");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
        var rt = targetImage.rectTransform;
        bool contains = RectTransformUtility.RectangleContainsScreenPoint(rt, eventData.position, targetImage.canvas?.worldCamera);
        Debug.Log($"[RectClickDebugger] OnPointerClick pos={eventData.position} containsRect={contains} pointerEnter={eventData.pointerEnter?.name}");
    }
}