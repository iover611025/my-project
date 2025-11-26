using UnityEngine;

namespace X
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class Trapezoid2DFake3D_Horizontal : MonoBehaviour
    {
        [Header("梯形設定")]
        public float leftHeight = 2f;      // 左邊高度
        public float rightHeight = 4f;     // 右邊高度
        public float width = 3f;           // 寬度（左右距離）
        public string sortingLayerName = "Default";
        public int sortingOrder = 0;
        [Range(-1f, 1f)] public float perspective = 0.3f; // 偽3D視差

        [Header("圖片貼圖")]
        public Texture2D texture;

        void Start()
        {
            Mesh mesh = new Mesh();

            // 讓橫向梯形中心在 (0,0)
            float yOffset = width * perspective;

            // 頂點順序：左下、右下、右上、左上（逆時針，法線朝+z，scale.z正即可顯示）
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(-width / 2, -leftHeight / 2 - yOffset, 0);    // 左下
            vertices[1] = new Vector3(width / 2, -rightHeight / 2 + yOffset, 0);    // 右下
            vertices[2] = new Vector3(width / 2, rightHeight / 2 + yOffset, 0);     // 右上
            vertices[3] = new Vector3(-width / 2, leftHeight / 2 - yOffset, 0);     // 左上

            // 逆時針三角形順序（避免背面剔除）
            int[] triangles = new int[]
            {
                0, 2, 1,
                0, 3, 2
            };

            Vector2[] uvs = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            GetComponent<MeshFilter>().mesh = mesh;

            var mr = GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Unlit/Texture"));
            mr.material.mainTexture = texture;

            mr.sortingLayerName = sortingLayerName;
            mr.sortingOrder = sortingOrder;
        }
    }
}