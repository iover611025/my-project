using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class EffectPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    
    /// <summary>
    /// 是否在開始時播放
    /// </summary>
    [SerializeField] private bool playOnAwake = true;
    /// <summary>
    /// 是否循環播放
    /// </summary>
    [SerializeField] private bool loop = true;

    private Coroutine _currentCoroutine;

    /// <summary>
    /// 每次播放的間隔
    /// </summary>
    [SerializeField, Min(0f)] private float delayTime;
    /// <summary>
    /// 每次播放的間隔的隨機範圍
    /// </summary>
    [SerializeField, Min(0f)] private float randomRangeTime;
    private void Start()
    {
        if (playOnAwake)
            _currentCoroutine = StartCoroutine(PlayCoroutine());
    }
    
    private IEnumerator PlayCoroutine()
    {
        if(audioSource.isPlaying)
            audioSource.Stop();
        
        audioSource.Play();
        while (loop)
        {
            float waitTime = Mathf.Max(delayTime + Random.Range(0f, randomRangeTime), 0f);
            yield return new WaitForSecondsRealtime(waitTime);
            
            if(audioSource.isPlaying)
                audioSource.Stop();
        
            audioSource.Play();
        }

        _currentCoroutine = null;
    }

    private void Reset()
    {
        if(audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
    }

    /// <summary>
    /// 預留的外部播放方法，如果要透過其他程式控制此音效播放，可以呼叫此方法
    /// </summary>
    public void Play()
    {
        if (_currentCoroutine != null)
        {
            audioSource.Stop();
            StopCoroutine(_currentCoroutine);
        }

        _currentCoroutine = StartCoroutine(PlayCoroutine());
    }
}