using UnityEngine;

[System.Serializable]
public class BigScene
{
    public int id;
    public string sceneName;
    public RectTransform[] roomPanels;
    // 新增：拖入與 roomPanels 一一對應的實體房間 GameObject
    public GameObject[] worldRooms;
}