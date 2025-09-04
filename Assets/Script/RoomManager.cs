using UnityEngine;

namespace X
{
    public class RoomManager : MonoBehaviour
    {
        [Header("房間物件（依序排列）")]
        public GameObject[] rooms; // 拖入所有房間物件
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
            if (rooms == null || rooms.Length == 0) return;
            int newIndex = Mathf.Clamp(currentRoomIndex + direction, 0, rooms.Length - 1);
            if (newIndex != currentRoomIndex)
            {
                currentRoomIndex = newIndex;
                ShowRoom(currentRoomIndex);
            }
        }

        void ShowRoom(int index)
        {
            for (int i = 0; i < rooms.Length; i++)
                if (rooms[i] != null)
                    rooms[i].SetActive(i == index);
        }

        // 進入新房子時可呼叫此方法重設房間
        public void EnterHouse(GameObject[] newRooms)
        {
            rooms = newRooms;
            currentRoomIndex = 0;
            ShowRoom(currentRoomIndex);
        }
    }
}