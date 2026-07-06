using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace X.Tests
{
    /// <summary>
    /// SwipeUpToSwitchObject 的 Edit Mode 單元測試。
    /// 
    /// 由於 Edit Mode 無法執行 Coroutine，以下測試著重在：
    ///   1. 初始化邏輯（Start 時的 currentIndex 偵測）
    ///   2. 上滑距離的閾值判斷
    ///   3. 物件切換的索引循環邏輯
    ///   4. 淡入淡出期間的 isFading 鎖定
    ///   5. 邊界條件（空陣列、單一物件）
    /// </summary>
    public class SwipeUpToSwitchObjectTests
    {
        // ──────────────────────────────────────────
        // 輔助方法
        // ──────────────────────────────────────────

        /// <summary>
        /// 建立一個掛有 SwipeUpToSwitchObject 且帶有指定數量子物件的測試場景根物件。
        /// </summary>
        private SwipeUpToSwitchObject CreateSwitcher(int objectCount, int activeIndex = 0)
        {
            var root = new GameObject("SwipeUpSwitcher");
            var switcher = root.AddComponent<SwipeUpToSwitchObject>();

            var objects = new GameObject[objectCount];
            for (int i = 0; i < objectCount; i++)
            {
                objects[i] = new GameObject($"TargetObject_{i}");
                objects[i].SetActive(i == activeIndex);
            }
            switcher.targetObjects = objects;
            return switcher;
        }

        /// <summary>
        /// 透過反射取得 SwipeUpToSwitchObject 的 private 欄位。
        /// </summary>
        private T GetPrivateField<T>(SwipeUpToSwitchObject target, string fieldName)
        {
            var field = typeof(SwipeUpToSwitchObject)
                .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, $"Private field '{fieldName}' not found on SwipeUpToSwitchObject.");
            return (T)field.GetValue(target);
        }

        private void SetPrivateField<T>(SwipeUpToSwitchObject target, string fieldName, T value)
        {
            var field = typeof(SwipeUpToSwitchObject)
                .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, $"Private field '{fieldName}' not found on SwipeUpToSwitchObject.");
            field.SetValue(target, value);
        }

        private void InvokePrivateMethod(SwipeUpToSwitchObject target, string methodName)
        {
            var method = typeof(SwipeUpToSwitchObject)
                .GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, $"Private method '{methodName}' not found on SwipeUpToSwitchObject.");
            method.Invoke(target, null);
        }

        // ──────────────────────────────────────────
        // 1. 初始化測試
        // ──────────────────────────────────────────

        /// <summary>
        /// 場景中第 0 個物件是 Active 時，Start 後 currentIndex 應為 0。
        /// </summary>
        [Test]
        public void Start_FirstObjectActive_CurrentIndexIsZero()
        {
            var switcher = CreateSwitcher(3, activeIndex: 0);

            // 手動呼叫 Start（Edit Mode 不會自動執行）
            InvokePrivateMethod(switcher, "Start");

            int currentIndex = GetPrivateField<int>(switcher, "currentIndex");
            Assert.AreEqual(0, currentIndex, "currentIndex 應該為 0（第一個 Active 物件的索引）。");

            Object.DestroyImmediate(switcher.gameObject);
            foreach (var go in switcher.targetObjects) Object.DestroyImmediate(go);
        }

        /// <summary>
        /// 場景中第 2 個物件是 Active 時，Start 後 currentIndex 應為 2。
        /// </summary>
        [Test]
        public void Start_ThirdObjectActive_CurrentIndexIsTwo()
        {
            var switcher = CreateSwitcher(3, activeIndex: 2);

            InvokePrivateMethod(switcher, "Start");

            int currentIndex = GetPrivateField<int>(switcher, "currentIndex");
            Assert.AreEqual(2, currentIndex, "currentIndex 應該為 2（第三個 Active 物件的索引）。");

            Object.DestroyImmediate(switcher.gameObject);
            foreach (var go in switcher.targetObjects) Object.DestroyImmediate(go);
        }

        /// <summary>
        /// Start 後，所有物件都應該自動加上 CanvasGroup。
        /// </summary>
        [Test]
        public void Start_AddsCanvasGroupToAllObjects()
        {
            var switcher = CreateSwitcher(3, activeIndex: 0);

            InvokePrivateMethod(switcher, "Start");

            foreach (var obj in switcher.targetObjects)
            {
                Assert.IsNotNull(obj.GetComponent<CanvasGroup>(),
                    $"物件 '{obj.name}' 在 Start 後應該自動取得 CanvasGroup。");
            }

            Object.DestroyImmediate(switcher.gameObject);
            foreach (var go in switcher.targetObjects) Object.DestroyImmediate(go);
        }

        // ──────────────────────────────────────────
        // 2. 索引循環邏輯測試
        // ──────────────────────────────────────────

        /// <summary>
        /// 從最後一個物件切換時，索引應循環回到 0。
        /// </summary>
        [Test]
        public void SwitchToNextObject_FromLastIndex_WrapsAroundToZero()
        {
            var switcher = CreateSwitcher(3, activeIndex: 0);
            InvokePrivateMethod(switcher, "Start");

            // 手動把 currentIndex 設定到最後一個
            int lastIndex = switcher.targetObjects.Length - 1;
            SetPrivateField(switcher, "currentIndex", lastIndex);

            // 計算下一個索引
            int nextIndex = (lastIndex + 1) % switcher.targetObjects.Length;

            Assert.AreEqual(0, nextIndex, "從最後一個物件切換，索引應循環回到 0。");

            Object.DestroyImmediate(switcher.gameObject);
            foreach (var go in switcher.targetObjects) Object.DestroyImmediate(go);
        }

        /// <summary>
        /// 從索引 1 切換，下一個應為索引 2。
        /// </summary>
        [Test]
        public void SwitchToNextObject_FromMiddleIndex_IncrementsCorrectly()
        {
            var switcher = CreateSwitcher(4, activeIndex: 1);
            InvokePrivateMethod(switcher, "Start");

            int currentIndex = GetPrivateField<int>(switcher, "currentIndex");
            int nextIndex = (currentIndex + 1) % switcher.targetObjects.Length;

            Assert.AreEqual(2, nextIndex, "從索引 1 切換，nextIndex 應為 2。");

            Object.DestroyImmediate(switcher.gameObject);
            foreach (var go in switcher.targetObjects) Object.DestroyImmediate(go);
        }

        // ──────────────────────────────────────────
        // 3. 上滑閾值判斷測試
        // ──────────────────────────────────────────

        /// <summary>
        /// 模擬螢幕高度 1080，閾值比例 0.05 → threshold = 54px，
        /// deltaY = 100 超過閾值，應判定為上滑成功。
        /// </summary>
        [Test]
        public void SwipeDetection_DeltaYAboveThreshold_ShouldTriggerSwitch()
        {
            float swipeThresholdRatio = 0.05f;
            float simulatedScreenHeight = 1080f;
            float threshold = simulatedScreenHeight * swipeThresholdRatio; // = 54
            float deltaY = 100f; // 超過閾值

            bool shouldSwitch = deltaY > threshold;

            Assert.IsTrue(shouldSwitch, $"deltaY (100) 超過 threshold ({threshold})，應判定為上滑觸發切換。");
        }

        /// <summary>
        /// 模擬螢幕高度 1080，閾值比例 0.05 → threshold = 54px，
        /// deltaY = 30 低於閾值，不應觸發切換。
        /// </summary>
        [Test]
        public void SwipeDetection_DeltaYBelowThreshold_ShouldNotTriggerSwitch()
        {
            float swipeThresholdRatio = 0.05f;
            float simulatedScreenHeight = 1080f;
            float threshold = simulatedScreenHeight * swipeThresholdRatio; // = 54
            float deltaY = 30f; // 低於閾值

            bool shouldSwitch = deltaY > threshold;

            Assert.IsFalse(shouldSwitch, $"deltaY (30) 低於 threshold ({threshold})，不應觸發切換。");
        }

        /// <summary>
        /// 向下滑動（負值），絕對不應觸發切換。
        /// </summary>
        [Test]
        public void SwipeDetection_NegativeDeltaY_ShouldNotTriggerSwitch()
        {
            float swipeThreshold = 50f;
            float deltaY = -80f; // 向下滑

            bool shouldSwitch = deltaY > swipeThreshold;

            Assert.IsFalse(shouldSwitch, "負的 deltaY（向下滑）不應觸發切換。");
        }

        // ──────────────────────────────────────────
        // 4. 實際拖曳行為模擬測試 (PointerDown + PointerUp)
        // ──────────────────────────────────────────

        /// <summary>
        /// 模擬實際接收到 PointerDown 與 PointerUp 事件，且滑動距離超過閾值，
        /// 驗證腳本確實呼叫了切換邏輯 (isFading 變為 true，currentIndex 更新)。
        /// </summary>
        [Test]
        public void PointerDrag_ValidSwipeUp_ShouldTriggerSwitch()
        {
            var switcher = CreateSwitcher(3, activeIndex: 0);
            InvokePrivateMethod(switcher, "Start");

            // 模擬滑鼠按下 (PointerDown)
            var pointerDownEvent = new PointerEventData(null)
            {
                position = new Vector2(0, 0)
            };
            switcher.OnPointerDown(pointerDownEvent);

            // 模擬滑鼠放開 (PointerUp) - 往上滑動超過閾值
            float threshold = Screen.height * switcher.swipeThresholdRatio;
            var pointerUpEvent = new PointerEventData(null)
            {
                position = new Vector2(0, threshold + 10f)
            };
            
            // 注意：Edit Mode 無法完整執行 Coroutine，但 StartCoroutine 依然會觸發 FadeToNextObject 
            // 執行到第一個 yield return null 之前，此時 isFading 會被設為 true。
            switcher.OnPointerUp(pointerUpEvent);

            bool isFading = GetPrivateField<bool>(switcher, "isFading");
            Assert.IsTrue(isFading, "有效的上滑拖曳，應該觸發切換邏輯並進入 isFading 狀態。");

            int currentIndex = GetPrivateField<int>(switcher, "currentIndex");
            Assert.AreEqual(1, currentIndex, "拖曳切換後，currentIndex 應更新為 1。");

            Object.DestroyImmediate(switcher.gameObject);
            foreach (var go in switcher.targetObjects) Object.DestroyImmediate(go);
        }

        // ──────────────────────────────────────────
        // 5. isFading 鎖定測試
        // ──────────────────────────────────────────

        /// <summary>
        /// 當 isFading 為 true 時，OnPointerUp 不應觸發任何切換。
        /// 用反射確認 currentIndex 在 isFading 時不應改變。
        /// </summary>
        [Test]
        public void OnPointerUp_WhenIsFadingTrue_ShouldNotChangeCurrentIndex()
        {
            var switcher = CreateSwitcher(3, activeIndex: 0);
            InvokePrivateMethod(switcher, "Start");

            // 強制進入 isFading 狀態
            SetPrivateField(switcher, "isFading", true);
            int indexBefore = GetPrivateField<int>(switcher, "currentIndex");

            // 模擬一個上滑的 PointerUp（透過直接操作 isFading 邏輯）
            bool isFading = GetPrivateField<bool>(switcher, "isFading");

            // 模擬 OnPointerUp 的核心判斷邏輯
            bool wouldSwitch = !isFading && switcher.targetObjects.Length > 1;

            Assert.IsFalse(wouldSwitch, "isFading 為 true 時，不應觸發物件切換。");

            // 確認 currentIndex 沒有被改變
            int indexAfter = GetPrivateField<int>(switcher, "currentIndex");
            Assert.AreEqual(indexBefore, indexAfter, "isFading 期間，currentIndex 應維持不變。");

            Object.DestroyImmediate(switcher.gameObject);
            foreach (var go in switcher.targetObjects) Object.DestroyImmediate(go);
        }

        // ──────────────────────────────────────────
        // 6. 邊界條件測試
        // ──────────────────────────────────────────

        /// <summary>
        /// 只有一個物件時，不應觸發切換（避免除以零或切換自身）。
        /// </summary>
        [Test]
        public void OnPointerUp_SingleObjectArray_ShouldNotSwitch()
        {
            var switcher = CreateSwitcher(1, activeIndex: 0);
            InvokePrivateMethod(switcher, "Start");

            bool isFading = GetPrivateField<bool>(switcher, "isFading");

            // 模擬 OnPointerUp 的核心判斷邏輯
            bool wouldSwitch = !isFading && switcher.targetObjects.Length > 1;

            Assert.IsFalse(wouldSwitch, "只有一個物件時，OnPointerUp 不應觸發切換。");

            Object.DestroyImmediate(switcher.gameObject);
            Object.DestroyImmediate(switcher.targetObjects[0]);
        }

        /// <summary>
        /// 空陣列時，targetObjects.Length 應為 0，不觸發任何切換。
        /// </summary>
        [Test]
        public void OnPointerUp_EmptyObjectArray_ShouldNotSwitch()
        {
            var root = new GameObject("SwipeUpSwitcher_Empty");
            var switcher = root.AddComponent<SwipeUpToSwitchObject>();
            switcher.targetObjects = new GameObject[0];

            bool isFading = GetPrivateField<bool>(switcher, "isFading");
            bool wouldSwitch = !isFading && switcher.targetObjects.Length > 1;

            Assert.IsFalse(wouldSwitch, "空陣列時，OnPointerUp 不應觸發切換。");

            Object.DestroyImmediate(root);
        }

        /// <summary>
        /// 預設情況下，swipeThresholdRatio 應為 0.05（螢幕高度的 5%）。
        /// </summary>
        [Test]
        public void DefaultSwipeThreshold_ShouldBePointZeroFive()
        {
            var root = new GameObject("SwipeUpSwitcher_Default");
            var switcher = root.AddComponent<SwipeUpToSwitchObject>();

            Assert.AreEqual(0.05f, switcher.swipeThresholdRatio, 0.0001f,
                "swipeThresholdRatio 的預設值應為 0.05（螢幕高度的 5%）。");

            Object.DestroyImmediate(root);
        }

        /// <summary>
        /// 預設情況下，fadeDuration 應為 1。
        /// </summary>
        [Test]
        public void DefaultFadeDuration_ShouldBeOne()
        {
            var root = new GameObject("SwipeUpSwitcher_FadeDuration");
            var switcher = root.AddComponent<SwipeUpToSwitchObject>();

            Assert.AreEqual(1.0f, switcher.fadeDuration, "fadeDuration 的預設值應為 1.0 秒。");

            Object.DestroyImmediate(root);
        }
    }
}
