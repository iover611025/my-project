using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace X.Tests.PlayMode
{
    /// <summary>
    /// SwipeUpToSwitchObject 的 Play Mode 單元測試。
    ///
    /// Play Mode 可以真實執行 Coroutine，因此這裡可以測試：
    ///   1. 拖曳觸發切換後，物件的 Active 狀態是否正確切換
    ///   2. 淡入淡出的 isFading 鎖定是否在 Coroutine 執行期間保持 true
    ///   3. Coroutine 結束後，isFading 是否正確重置為 false
    ///   4. 連續上滑時，isFading 鎖定是否確實阻擋了第二次切換
    /// </summary>
    public class SwipeUpToSwitchObjectPlayModeTest
    {
        // ──────────────────────────────────────────
        // 輔助方法
        // ──────────────────────────────────────────

        /// <summary>
        /// 建立一個帶有 SwipeUpToSwitchObject 組件的測試環境。
        /// 為了讓 PointerEvent 能被 UI 系統偵測到，掛載在 Canvas 底下。
        /// </summary>
        private (SwipeUpToSwitchObject switcher, GameObject[] targets) CreatePlayModeSwitcher(int objectCount, int activeIndex = 0)
        {
            // 建立一個假的 Canvas 環境
            var canvasGo = new GameObject("TestCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();

            var switcherGo = new GameObject("Switcher");
            switcherGo.transform.SetParent(canvasGo.transform);
            var switcher = switcherGo.AddComponent<SwipeUpToSwitchObject>();
            // 縮短淡入淡出時間，加速測試
            switcher.fadeDuration = 0.2f;

            var targets = new GameObject[objectCount];
            for (int i = 0; i < objectCount; i++)
            {
                targets[i] = new GameObject($"Target_{i}");
                targets[i].transform.SetParent(canvasGo.transform);
                targets[i].SetActive(i == activeIndex);
            }
            switcher.targetObjects = targets;
            return (switcher, targets);
        }

        private T GetPrivateField<T>(SwipeUpToSwitchObject target, string fieldName)
        {
            var field = typeof(SwipeUpToSwitchObject)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"找不到私有欄位 '{fieldName}'");
            return (T)field.GetValue(target);
        }

        // ──────────────────────────────────────────
        // 測試 1：拖曳後，物件 Active 狀態確實切換
        // ──────────────────────────────────────────

        /// <summary>
        /// 模擬一次有效上滑後，等待淡入淡出 Coroutine 跑完，
        /// 驗證舊物件已關閉、新物件已開啟。
        /// </summary>
        [UnityTest]
        public IEnumerator SwipeUp_AfterFadeComplete_ObjectsSwitchCorrectly()
        {
            // Arrange
            var (switcher, targets) = CreatePlayModeSwitcher(3, activeIndex: 0);

            // 等一幀讓 Start() 初始化完成
            yield return null;

            Assert.IsTrue(targets[0].activeSelf, "切換前：targets[0] 應為 Active");
            Assert.IsFalse(targets[1].activeSelf, "切換前：targets[1] 應為 Inactive");

            // Act：模擬上滑手勢
            float swipeDistance = Screen.height * switcher.swipeThresholdRatio + 10f;
            switcher.OnPointerDown(new PointerEventData(null) { position = new Vector2(0, 0) });
            switcher.OnPointerUp(new PointerEventData(null) { position = new Vector2(0, swipeDistance) });

            // 等待淡入淡出 Coroutine 完全結束 (fadeDuration = 0.2s，多等一點)
            yield return new WaitForSeconds(0.4f);

            // Assert：驗證物件切換結果
            Assert.IsFalse(targets[0].activeSelf, "淡出後：targets[0] 應該被關閉");
            Assert.IsTrue(targets[1].activeSelf, "淡入後：targets[1] 應該被開啟");

            // Clean up
            Object.Destroy(switcher.transform.parent.gameObject);
        }

        // ──────────────────────────────────────────
        // 測試 2：Coroutine 執行期間 isFading 應保持 true
        // ──────────────────────────────────────────

        /// <summary>
        /// 觸發切換後，在 Coroutine 執行期間的下一幀，
        /// 驗證 isFading 已被設為 true（鎖定中）。
        /// </summary>
        [UnityTest]
        public IEnumerator SwipeUp_DuringFade_IsFadingShouldBeTrue()
        {
            // Arrange
            var (switcher, targets) = CreatePlayModeSwitcher(3, activeIndex: 0);
            yield return null;

            // Act
            float swipeDistance = Screen.height * switcher.swipeThresholdRatio + 10f;
            switcher.OnPointerDown(new PointerEventData(null) { position = new Vector2(0, 0) });
            switcher.OnPointerUp(new PointerEventData(null) { position = new Vector2(0, swipeDistance) });

            // 僅推進一幀，Coroutine 才剛開始
            yield return null;

            // Assert：確認鎖定中
            bool isFading = GetPrivateField<bool>(switcher, "isFading");
            Assert.IsTrue(isFading, "Coroutine 執行期間，isFading 應該為 true");

            // Clean up
            Object.Destroy(switcher.transform.parent.gameObject);
        }

        // ──────────────────────────────────────────
        // 測試 3：Coroutine 結束後 isFading 重置為 false
        // ──────────────────────────────────────────

        /// <summary>
        /// 等待淡入淡出 Coroutine 完全結束後，
        /// 驗證 isFading 已重置回 false（解除鎖定）。
        /// </summary>
        [UnityTest]
        public IEnumerator SwipeUp_AfterFadeComplete_IsFadingShouldBeFalse()
        {
            // Arrange
            var (switcher, targets) = CreatePlayModeSwitcher(3, activeIndex: 0);
            yield return null;

            // Act
            float swipeDistance = Screen.height * switcher.swipeThresholdRatio + 10f;
            switcher.OnPointerDown(new PointerEventData(null) { position = new Vector2(0, 0) });
            switcher.OnPointerUp(new PointerEventData(null) { position = new Vector2(0, swipeDistance) });

            // 等待 Coroutine 完全跑完
            yield return new WaitForSeconds(0.4f);

            // Assert：確認解鎖
            bool isFading = GetPrivateField<bool>(switcher, "isFading");
            Assert.IsFalse(isFading, "淡入淡出結束後，isFading 應重置為 false");

            // Clean up
            Object.Destroy(switcher.transform.parent.gameObject);
        }

        // ──────────────────────────────────────────
        // 測試 4：isFading 期間，第二次上滑不應被觸發
        // ──────────────────────────────────────────

        /// <summary>
        /// 第一次上滑後，在 Coroutine 執行期間立刻再次上滑，
        /// 驗證第二次上滑因為 isFading 鎖定而被忽略（currentIndex 不繼續遞增）。
        /// </summary>
        [UnityTest]
        public IEnumerator SwipeUp_WhileIsFading_SecondSwipeShouldBeIgnored()
        {
            // Arrange
            var (switcher, targets) = CreatePlayModeSwitcher(3, activeIndex: 0);
            yield return null;

            float swipeDistance = Screen.height * switcher.swipeThresholdRatio + 10f;

            // Act：第一次上滑
            switcher.OnPointerDown(new PointerEventData(null) { position = new Vector2(0, 0) });
            switcher.OnPointerUp(new PointerEventData(null) { position = new Vector2(0, swipeDistance) });

            // 推進一幀（Coroutine 開始執行，isFading = true）
            yield return null;

            // 確認已進入 isFading
            Assert.IsTrue(GetPrivateField<bool>(switcher, "isFading"), "第一次上滑後應進入 isFading 狀態");

            // Act：在 isFading 期間，立刻再次上滑（應該被忽略）
            switcher.OnPointerDown(new PointerEventData(null) { position = new Vector2(0, 0) });
            switcher.OnPointerUp(new PointerEventData(null) { position = new Vector2(0, swipeDistance) });

            // 推進一幀
            yield return null;

            // Assert：currentIndex 應該只有 1（只觸發了一次切換），而不是 2
            int currentIndex = GetPrivateField<int>(switcher, "currentIndex");
            Assert.AreEqual(1, currentIndex, "isFading 期間的第二次上滑應被忽略，currentIndex 應仍為 1");

            // Clean up
            Object.Destroy(switcher.transform.parent.gameObject);
        }

        // ──────────────────────────────────────────
        // 測試 5：索引循環 - 最後一個物件切換後回到第一個
        // ──────────────────────────────────────────

        /// <summary>
        /// 當 currentIndex 為最後一個時，上滑後應循環回到第一個物件。
        /// </summary>
        [UnityTest]
        public IEnumerator SwipeUp_FromLastObject_WrapsAroundToFirst()
        {
            // Arrange：建立 2 個物件，讓初始 activeIndex = 1 (最後一個)
            var (switcher, targets) = CreatePlayModeSwitcher(2, activeIndex: 1);
            yield return null;

            // 確認初始狀態
            int initIndex = GetPrivateField<int>(switcher, "currentIndex");
            Assert.AreEqual(1, initIndex, "初始 currentIndex 應為 1");

            // Act：上滑
            float swipeDistance = Screen.height * switcher.swipeThresholdRatio + 10f;
            switcher.OnPointerDown(new PointerEventData(null) { position = new Vector2(0, 0) });
            switcher.OnPointerUp(new PointerEventData(null) { position = new Vector2(0, swipeDistance) });

            // 等待 Coroutine 完成
            yield return new WaitForSeconds(0.4f);

            // Assert：索引應循環回 0
            int finalIndex = GetPrivateField<int>(switcher, "currentIndex");
            Assert.AreEqual(0, finalIndex, "從最後一個物件上滑，currentIndex 應循環回到 0");
            Assert.IsTrue(targets[0].activeSelf, "循環後，targets[0] 應為 Active");
            Assert.IsFalse(targets[1].activeSelf, "循環後，targets[1] 應為 Inactive");

            // Clean up
            Object.Destroy(switcher.transform.parent.gameObject);
        }
    }
}
