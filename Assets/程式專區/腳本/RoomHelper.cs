using UnityEngine;

namespace X
{
    public static class RoomHelper
    {
        // 啟用陣列中只有 index 的項目 (GameObject)
        public static void ActivateOnly(GameObject[] arr, int index)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
            {
                var go = arr[i];
                if (go == null) continue;
                bool should = (i == index);
                if (go.activeSelf != should)
                    go.SetActive(should);
            }
        }

        // 啟用陣列中只有 index 的項目 (RectTransform / UI Panel)
        public static void ActivateOnly(RectTransform[] arr, int index)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
            {
                var rt = arr[i];
                if (rt == null) continue;
                bool should = (i == index);
                if (rt.gameObject.activeSelf != should)
                    rt.gameObject.SetActive(should);
            }
        }
    }
}