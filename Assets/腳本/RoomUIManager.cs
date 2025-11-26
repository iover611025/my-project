using UnityEngine;

namespace X
{
    public class RoomUIManager : MonoBehaviour
    {
        [Header("房間UI Panel（依序排列）")]
        public RectTransform[] roomPanels; // 拖入所有房間Panel
        private int currentRoomIndex = 0;

        void Start()
        {
            ShowRoom(currentRoomIndex);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
                SwitchRoom(-1);
            if (Input.GetKeyDown(KeyCode.D))
                SwitchRoom(1);
        }

        public void SwitchRoom(int direction)
        {
            if (roomPanels == null || roomPanels.Length == 0) return;
            int newIndex = Mathf.Clamp(currentRoomIndex + direction, 0, roomPanels.Length - 1);
            if (newIndex != currentRoomIndex)
            {
                currentRoomIndex = newIndex;
                ShowRoom(currentRoomIndex);
            }
        }

        void ShowRoom(int index)
        {
            for (int i = 0; i < roomPanels.Length; i++)
                if (roomPanels[i] != null)
                    roomPanels[i].gameObject.SetActive(i == index);
        }

        // 進入新房子時可呼叫此方法重設房間
        public void EnterHouse(RectTransform[] newPanels)
        {
            roomPanels = newPanels;
            currentRoomIndex = 0;
            ShowRoom(currentRoomIndex);
        }
    }
}