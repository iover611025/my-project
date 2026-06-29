using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

namespace X
{
    /// <summary>
    /// 使用預製影片進行 2D 場景轉場的管理器 (適用於 Unity 6)
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        [Header("UI 與影片元件")]
        [SerializeField] private CanvasGroup transitionCanvasGroup; // 用於阻擋點擊
        [SerializeField] private RawImage videoRawImage;         // 顯示影片的 UI
        [SerializeField] private VideoPlayer videoPlayer;           // 播放器元件

        [Header("影片時間點設定")]
        [Tooltip("影片播放到第幾秒時畫面會完全變黑/斑駁？此時會進行場景載入。")]
        [SerializeField] private float sceneSwitchTime = 1.0f;

        private bool _isTransitioning = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // 初始化狀態：隱藏 UI，關閉阻擋
            SetVideoUIActive(false);
        }

        /// <summary>
        /// 外部呼叫：切換至指定場景
        /// </summary>
        public void SwitchScene(string sceneName)
        {
            if (_isTransitioning) return;
            StartCoroutine(VideoTransitionRoutine(sceneName));
        }

        private IEnumerator VideoTransitionRoutine(string sceneName)
        {
            _isTransitioning = true;
            transitionCanvasGroup.blocksRaycasts = true; // 轉場期間阻擋玩家操作

            // 1. 準備影片 (非同步準備，避免大影片造成瞬間卡頓)
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }

            // 顯示影片 UI 並開始播放
            SetVideoUIActive(true);
            videoPlayer.Play();

            // 2. 等待影片播放到「最黑/最斑駁」的時間點
            yield return new WaitForSeconds(sceneSwitchTime);

            // 效能最佳化：強制垃圾回收
            System.GC.Collect();

            // 3. 背景非同步載入新場景
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                if (asyncLoad.progress >= 0.9f)
                {
                    asyncLoad.allowSceneActivation = true;
                }
                yield return null;
            }

            // 4. 等待整支影片播放完畢
            // 這裡利用影片剩餘時間來等待，比監聽事件在協程中更直觀
            while (videoPlayer.isPlaying)
            {
                yield return null;
            }

            // 5. 轉場結束，恢復原狀
            SetVideoUIActive(false);
            transitionCanvasGroup.blocksRaycasts = false;
            _isTransitioning = false;
        }

        private void SetVideoUIActive(bool active)
        {
            if (videoRawImage != null)
            {
                videoRawImage.enabled = active;
            }
            // 隱藏時將 CanvasGroup 的 alpha 歸零，顯示時設為 1
            if (transitionCanvasGroup != null)
            {
                transitionCanvasGroup.alpha = active ? 1f : 0f;
            }
        }
    }
}