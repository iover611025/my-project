using UnityEngine;

namespace X
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class Trapezoid2DFake3D_Horizontal : MonoBehaviour
    {
        [Header("梯形設定")]
        public float leftHeight = 2f;     // 左側高度
        public float rightHeight = 4f;    // 右側高度
        public float width = 3f;          // 整體寬度
        public string sortingLayerName = "Default";
        public int sortingOrder = 0;
        [Range(-1f, 1f)] public float perspective = 0.3f; // 偽3D視差

        [Header("圖片貼圖")]
        public Texture2D texture;

        void Start()
        {
            Mesh mesh = new Mesh();

            // 偽3D橫向梯形：中心對齊
            // perspective 讓梯形有左右深度變化感
            float yOffset = width * perspective;

            // 頂點順序：左下、右下、右上、左上
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(-width / 2, -leftHeight / 2 - yOffset, 0);       // 左下
            vertices[1] = new Vector3(width / 2, -rightHeight / 2 + yOffset, 0);       // 右下
            vertices[2] = new Vector3(width / 2, rightHeight / 2 + yOffset, 0);        // 右上
            vertices[3] = new Vector3(-width / 2, leftHeight / 2 - yOffset, 0);        // 左上

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