using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace X
{
    [RequireComponent(typeof(Image))]
    public class UIFrameAnimator : MonoBehaviour
    {
        [Header("動畫設定")]
        [SerializeField] private List<Sprite> animationFrames; // 存放所有的圖片幀
        [SerializeField] private int frameInterval = 45;      // 每 45 幀換一張圖
        [SerializeField] private bool loop = true;            // 是否循環播放

        private Image _uiImage;
        private int _currentFrameCounter = 0;
        private int _currentIndex = 0;

        void Awake()
        {
            // 取得當前物件上的 Image 組件
            _uiImage = GetComponent<Image>();
            
            if (animationFrames == null || animationFrames.Count == 0)
            {
                Debug.LogWarning("未分配動畫幀圖片！");
                enabled = false;
            }
        }

        void Update()
        {
            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            _currentFrameCounter++;

            // 當計數器達到設定的 45 幀時觸發換圖
            if (_currentFrameCounter >= frameInterval)
            {
                _currentFrameCounter = 0; // 重置計數器
                _currentIndex++;          // 指向下一張圖片

                if (_currentIndex >= animationFrames.Count)
                {
                    if (loop)
                    {
                        _currentIndex = 0; // 循環回到第一張
                    }
                    else
                    {
                        _currentIndex = animationFrames.Count - 1; // 停在最後一張
                        enabled = false; // 停止 Update 節省效能
                    }
                }

                // 更新 UI 顯示的圖片
                _uiImage.sprite = animationFrames[_currentIndex];
            }
        }
        
        // 提供外部呼叫：手動重置動畫（適用於解謎成功時的特效）
        public void PlayFromStart()
        {
            _currentIndex = 0;
            _currentFrameCounter = 0;
            enabled = true;
        }
    }
}