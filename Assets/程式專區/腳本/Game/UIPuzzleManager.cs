using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace X
{
public class UIPuzzleManager : MonoBehaviour
{
    [Header("謎題清單")]
    public List<UIDragHandler> puzzleItems; // 將所有拼圖碎片拖入此清單

    [Header("成功事件")]
    public UnityEvent OnAllSolved; // 當全部完成時觸發（例如開啟暗門、播放過場）

    [Header("音效設定")]
    public AudioSource audioSource;
    public AudioClip snapSound;    // 單個成功的聲音
    public AudioClip successSound; // 全部完成的聲音

    // 被 UIDragHandler 調用
    public void CheckPuzzleStatus()
    {
        int solvedCount = 0;

        foreach (var item in puzzleItems)
        {
            if (item.IsSolved) solvedCount++;
        }

        // 播放單次成功的音效
        if (audioSource && snapSound) audioSource.PlayOneShot(snapSound);

        Debug.Log($"進度：{solvedCount} / {puzzleItems.Count}");

        // 檢查是否全部完成
        if (solvedCount >= puzzleItems.Count)
        {
            CompletePuzzle();
        }
    }

    private void CompletePuzzle()
    {
        Debug.Log("恭喜！所有物件已歸位。");

        // 播放最終成功的音效
        if (audioSource && successSound) audioSource.PlayOneShot(successSound);

        // 執行你在 Inspector 設定的所有動作
        OnAllSolved?.Invoke();
    }
}
}