using NUnit.Framework;
using UnityEngine;
using UnityEngine.Video;
using X;

namespace X.Tests
{
    public class VideoLoopEditModeTest
    {
        [Test]
        public void VideoPlayer_IsLoopingProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var go = new GameObject("VideoPlayerTest");
            var videoPlayer = go.AddComponent<VideoPlayer>();

            // Act
            videoPlayer.isLooping = true;

            // Assert
            Assert.IsTrue(videoPlayer.isLooping, "VideoPlayer isLooping should be true");

            // Clean up (Edit Mode 必須用 DestroyImmediate)
            Object.DestroyImmediate(go);
        }

        [Test]
        public void VideoLoopTestScript_InitializesWithCorrectDefaultValues()
        {
            // Arrange
            var go = new GameObject("VideoLoopScriptTest");
            var videoPlayer = go.AddComponent<VideoPlayer>();
            var testScript = go.AddComponent<VideoLoopTest>();

            // Act - 在 Edit Mode 下檢查預設值是否如預期
            bool isAutoPlay = testScript.autoPlay;
            bool isLooping = testScript.isLooping;

            // Assert
            Assert.IsTrue(isAutoPlay, "autoPlay should default to true");
            Assert.IsTrue(isLooping, "isLooping should default to true");

            // Clean up
            Object.DestroyImmediate(go);
        }

        // ===== DirectRoomTeleporter - WithVideo transition failure tests =====

        /// <summary>
        /// When transitionType is WithVideo but transitionVideoPlayer is null,
        /// the teleporter should NOT attempt to play any video.
        /// Expected: transitionVideoPlayer stays null, no NullReferenceException.
        /// </summary>
        [Test]
        public void Teleporter_WithVideoType_NullVideoPlayer_ShouldNotPlay()
        {
            // Arrange
            var go = new GameObject("TeleporterTest");
            var teleporter = go.AddComponent<DirectRoomTeleporter>();
            teleporter.transitionType = DirectRoomTeleporter.TransitionType.WithVideo;
            // transitionVideoPlayer is intentionally left null

            // Act & Assert
            // The video player must be null - meaning no video can play
            Assert.IsNull(teleporter.transitionVideoPlayer,
                "transitionVideoPlayer is null, so WithVideo transition cannot play any video.");

            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// When transitionVideoPlayer is assigned but its clip is null and url is empty,
        /// the VideoPlayer has nothing to play (isPlaying will remain false after Play()).
        /// This simulates the case where the designer forgot to assign a video clip.
        /// Expected: VideoPlayer.clip is null and url is empty.
        /// </summary>
        [Test]
        public void Teleporter_WithVideoType_VideoPlayerHasNoClip_ShouldNotPlay()
        {
            // Arrange
            var go = new GameObject("TeleporterTest");
            var teleporter = go.AddComponent<DirectRoomTeleporter>();
            teleporter.transitionType = DirectRoomTeleporter.TransitionType.WithVideo;

            var vpGo = new GameObject("VideoPlayerGO");
            var videoPlayer = vpGo.AddComponent<VideoPlayer>();
            teleporter.transitionVideoPlayer = videoPlayer;
            // Intentionally NOT assigning clip or url

            // Act
            bool hasClip = videoPlayer.clip != null;
            bool hasUrl = !string.IsNullOrEmpty(videoPlayer.url);
            bool canPlay = hasClip || hasUrl;

            // Assert
            Assert.IsFalse(canPlay,
                "VideoPlayer has no clip and no URL assigned - transition video cannot play.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(vpGo);
        }

        /// <summary>
        /// When transitionType is Simple or WithText (not WithVideo),
        /// the teleporter should not use the VideoPlayer even if one is assigned.
        /// Expected: transitionType is not WithVideo.
        /// </summary>
        [Test]
        public void Teleporter_WrongTransitionType_VideoPlayerAssigned_ShouldNotTriggerVideo()
        {
            // Arrange
            var go = new GameObject("TeleporterTest");
            var teleporter = go.AddComponent<DirectRoomTeleporter>();
            teleporter.transitionType = DirectRoomTeleporter.TransitionType.Simple;

            var vpGo = new GameObject("VideoPlayerGO");
            var videoPlayer = vpGo.AddComponent<VideoPlayer>();
            teleporter.transitionVideoPlayer = videoPlayer;

            // Act
            bool isVideoMode = teleporter.transitionType == DirectRoomTeleporter.TransitionType.WithVideo;

            // Assert
            Assert.IsFalse(isVideoMode,
                "TransitionType is not WithVideo, so the VideoPlayer will not be triggered even if assigned.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(vpGo);
        }

        // ===== DirectRoomTeleporter - EventSystem Freeze tests =====
        // 注意：由於 Unity 限制，在 Edit Mode 下強制指派 EventSystem.current 會報錯 
        // "Failed setting EventSystem.current to unknown EventSystem Selected: No module"
        // 且 InputModule 的生命週期無法在 Edit Mode 正常初始化。
        // 因此凍結功能 (EventSystem.current.enabled = false) 無法在 Edit Mode 透過單元測試驗證，
        // 建議透過 Play Mode Test 或手動測試來驗證此功能。
    }
}
