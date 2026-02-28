using UnityEngine;

// 定義 BigScene 類型，確保 RoomUIManager 能正確引用
[System.Serializable]
public class BigScene
{
    public int id;
    public string sceneName;
    public RectTransform[] roomPanels;
}