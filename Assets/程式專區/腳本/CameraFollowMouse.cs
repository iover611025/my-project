using UnityEngine;

namespace X
{
    using UnityEngine;

    public class CameraFollowMouse : MonoBehaviour
    {
        public static CameraFollowMouse Instance;

        public float moveAmount = 1.0f;
        public float smoothTime = 0.2f;
        public bool isSwaying = true;

        private Vector3 initialPosition;
        private Vector3 velocity = Vector3.zero;

        void Awake() { Instance = this; } // 確保初始化

        void Start() { initialPosition = transform.position; }

        void Update()
        {
            // 即使 isSwaying 為 false，也要跑 SmoothDamp 讓它平滑回正
            Vector3 targetPos = initialPosition;

            if (isSwaying)
            {
                Vector2 mousePos = Input.mousePosition;
                float x = (mousePos.x / Screen.width - 0.5f) * 2f;
                float y = (mousePos.y / Screen.height - 0.5f) * 2f;
                targetPos = initialPosition + new Vector3(x * moveAmount, y * moveAmount, 0);
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
        }

        public void SetSwayActive(bool active) => isSwaying = active;
    }
}