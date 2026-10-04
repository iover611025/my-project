using UnityEngine;

namespace X
{
    /// <summary>
    /// 掛在 Vertical Layout Group 容器上。
    /// 自動管理分隔線 Image：只在相鄰兩個可見線索區塊之間顯示，
    /// 上下沒有鄰居時自動隱藏，不受解鎖順序影響。
    ///
    /// 使用方式：
    ///   1. 將此腳本掛到 VLG 容器 (Content) 上。
    ///   2. 將每條分隔線 Image 的 Tag 設為「Divider」。
    ///   3. 在每個線索區塊的 onPageUnlocked 事件後呼叫 RefreshDividers()。
    /// </summary>
    public class ClueListDividerManager : MonoBehaviour
    {
        [Tooltip("分隔線 GameObject 的 Tag，用來識別哪些子物件是分隔線")]
        [SerializeField] private string dividerTag = "Divider";

        private void OnEnable() => RefreshDividers();

        /// <summary>
        /// 重新計算所有分隔線的顯示狀態。
        /// 呼叫時機：任何線索區塊被 SetActive(true/false) 之後。
        /// </summary>
        public void RefreshDividers()
        {
            int childCount = transform.childCount;

            // 先把所有分隔線隱藏
            for (int i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.CompareTag(dividerTag))
                    child.gameObject.SetActive(false);
            }

            // 找出每對相鄰的可見內容區塊之間的分隔線，顯示它
            int lastVisibleContentIndex = -1;

            for (int i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);

                if (child.CompareTag(dividerTag))
                    continue;

                if (!child.gameObject.activeInHierarchy)
                    continue;

                // 這是一個可見的內容區塊
                if (lastVisibleContentIndex >= 0)
                {
                    // 在上一個可見內容和這個之間找分隔線並顯示
                    for (int j = lastVisibleContentIndex + 1; j < i; j++)
                    {
                        var between = transform.GetChild(j);
                        if (between.CompareTag(dividerTag))
                        {
                            between.gameObject.SetActive(true);
                            break;
                        }
                    }
                }

                lastVisibleContentIndex = i;
            }
        }
    }
}