using UnityEngine;
using UnityEngine.Video;

namespace X
{
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoLoopTest : MonoBehaviour
    {
        private VideoPlayer _videoPlayer;

        [Header("測試設定")]
        public bool autoPlay = true;
        [Tooltip("是否開啟循環播放")]
        public bool isLooping = true;

        void Start()
        {
            _videoPlayer = GetComponent<VideoPlayer>();
            
            // 設定影片是否要循環播放
            _videoPlayer.isLooping = isLooping;

            // 註冊事件，這樣當影片播到底（或重新循環）時我們會在 Console 看到通知
            _videoPlayer.loopPointReached += OnLoopPointReached;
            _videoPlayer.errorReceived += OnVideoError;

            if (autoPlay)
            {
                PlayVideo();
            }
        }

        public void PlayVideo()
        {
            if (_videoPlayer.clip != null || !string.IsNullOrEmpty(_videoPlayer.url))
            {
                Debug.Log($"[VideoLoopTest] Starting to play video...");
                _videoPlayer.Play();
            }
            else
            {
                Debug.LogWarning("[VideoLoopTest] VideoPlayer has no Video Clip or URL set!");
            }
        }

        private void OnLoopPointReached(VideoPlayer vp)
        {
            Debug.Log($"[VideoLoopTest] Video reached the end! (Current looping setting: {vp.isLooping})");
        }

        private void OnVideoError(VideoPlayer vp, string message)
        {
            Debug.LogError($"[VideoLoopTest] Video playback error: {message}");
        }

        void OnDestroy()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.loopPointReached -= OnLoopPointReached;
                _videoPlayer.errorReceived -= OnVideoError;
            }
        }
    }
}
