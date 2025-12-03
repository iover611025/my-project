using UnityEngine;

public class CameraFollowMouse : MonoBehaviour
{
    public float moveAmount = 1.0f; // 最大偏移量
    public float smoothTime = 0.2f; // 平滑時間

    private Vector3 initialPosition;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        initialPosition = transform.position;
    }

    void Update()
    {
        // 滑鼠在螢幕上的歸一化座標（-1~1）
        Vector2 mousePos = Input.mousePosition;
        float x = (mousePos.x / Screen.width - 0.5f) * 2f;
        float y = (mousePos.y / Screen.height - 0.5f) * 2f;

        // 計算目標偏移
        Vector3 targetOffset = new Vector3(x * moveAmount, y * moveAmount, 0);

        // 平滑移動
        transform.position = Vector3.SmoothDamp(transform.position, initialPosition + targetOffset, ref velocity, smoothTime);
    }
}