using UnityEngine;

public class CameraFollowMouse : MonoBehaviour
{
    public static CameraFollowMouse Instance; // 建立單例供 Manager 存取

    public float moveAmount = 1.0f; // 可由 Slider 調整
    public float smoothTime = 0.2f;
    public bool isSwaying = true;   // 控制是否啟動晃動

    private Vector3 initialPosition;
    private Vector3 velocity = Vector3.zero;

    void Awake() => Instance = this;

    void Start()
    {
        initialPosition = transform.position;
    }

    void Update()
    {
        // 核心邏輯：若關閉晃動，目標位置即為初始位置，讓攝影機平滑回正
        Vector3 targetPos = initialPosition;

        if (isSwaying)
        {
            Vector2 mousePos = Input.mousePosition;
            float x = (mousePos.x / Screen.width - 0.5f) * 2f;
            float y = (mousePos.y / Screen.height - 0.5f) * 2f;
            targetPos = initialPosition + new Vector3(x * moveAmount, y * moveAmount, 0);
        }

        // 無論開關與否，都經過 SmoothDamp 確保視覺流暢度
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
    }

    // 提供給 Slider 或是 Manager 呼叫的介面
    public void SetSwayActive(bool active) => isSwaying = active;
    public void SetSwayAmount(float val) => moveAmount = val;
}