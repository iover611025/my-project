using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;

namespace X.Tests.PlayMode
{
    public class TeleporterPlayModeTest
    {
        [UnityTest]
        public IEnumerator Teleporter_DuringTransition_ShouldDisableEventSystem_FreezeInteraction()
        {
            // ==========================================
            // Arrange - 準備測試環境
            // ==========================================
            var go = new GameObject("TeleporterTest");
            var teleporter = go.AddComponent<DirectRoomTeleporter>();
            // 縮短轉場時間以加速測試
            teleporter.fadeDuration = 0.1f;
            teleporter.blackStayDuration = 0.1f;

            // 建立 Play Mode 下合法的 EventSystem (含 InputModule)
            var eventSystemGo = new GameObject("EventSystem");
            var eventSystem = eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();

            // 建立假的 RoomUIManager 避免 NullReferenceException
            var uiManagerGo = new GameObject("RoomUIManager");
            uiManagerGo.AddComponent<RoomUIManager>();

            // Play Mode 測試中，必須等待一幀讓 Unity 完成 Awake/OnEnable 的生命週期初始化
            yield return null;

            // 確保 EventSystem.current 有成功註冊，且預設是開啟的
            Assert.IsNotNull(EventSystem.current, "Play Mode 下 EventSystem.current 必須存在");
            Assert.IsTrue(EventSystem.current.enabled, "轉場前，EventSystem.current 應該是啟用的");

            // ==========================================
            // Act - 觸發轉場
            // ==========================================
            // 因為測試環境中的 RoomUIManager 是空的，一定會找不到場景並印出 Error
            // 我們必須告訴測試框架「預期會收到這個 Error」，這樣測試才不會被判定為失敗
            LogAssert.Expect(LogType.Error, "[RoomUIManager] 找不到 ID 為 1 的大場景");

            // 透過公開方法或反射啟動轉場
            teleporter.StartTeleport(1, 1);

            // ==========================================
            // Assert 1 - 轉場剛開始 (凍結測試)
            // ==========================================
            // 推進一幀，讓 TeleportRoutine 的第一行邏輯 (停用 EventSystem) 有時間執行
            yield return null;
            
            // 驗證核心凍結功能：EventSystem 應該已被關閉
            // 這裡必須檢查 eventSystem 實例，因為 EventSystem 關閉時，EventSystem.current 會變成 null！
            Assert.IsFalse(eventSystem.enabled, "轉場期間，EventSystem 應該被停用以鎖定玩家點擊");

            // ==========================================
            // Assert 2 - 等待轉場結束 (解凍測試)
            // ==========================================
            // 等待足夠長的時間讓轉場協程跑完 (淡出 0.1s + 停留 0.1s + 淡入 0.1s)
            yield return new WaitForSeconds(0.4f);

            // 驗證轉場結束後，EventSystem 是否成功重新開啟
            Assert.IsTrue(eventSystem.enabled, "轉場結束後，EventSystem 應該自動重新啟用");

            // ==========================================
            // Clean up - 清理場景
            // ==========================================
            Object.Destroy(go);
            Object.Destroy(eventSystemGo);
            Object.Destroy(uiManagerGo);
        }
    }
}
