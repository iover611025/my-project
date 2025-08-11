using UnityEngine;

namespace X
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class Trapezoid2DFake3D : MonoBehaviour
    {
        [Header("梯形設定")]
        public float topWidth = 2f;       // 上寬
        public float bottomWidth = 4f;    // 下寬
        public float height = 3f;         // 高度
        public string sortingLayerName = "Default";
        public int sortingOrder = 0;
        [Range(-1f, 1f)] public float perspective = 0.3f; // 偽3D視差

        [Header("圖片貼圖")]
        public Texture2D texture;

        void Start()
        {
            Mesh mesh = new Mesh();

            // 讓梯形中心在 (0, 0)，法線方向正確（逆時針）
            float xOffset = height * perspective;

            // 頂點順序：左下、右下、右上、左上（逆時針）
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(-bottomWidth / 2 - xOffset, -height / 2, 0);           // 左下
            vertices[1] = new Vector3(bottomWidth / 2 - xOffset, -height / 2, 0);            // 右下
            vertices[2] = new Vector3(topWidth / 2 + xOffset, height / 2, 0);                // 右上
            vertices[3] = new Vector3(-topWidth / 2 + xOffset, height / 2, 0);               // 左上

            int[] triangles = new int[]
            {
                0, 1, 2,
                0, 2, 3
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