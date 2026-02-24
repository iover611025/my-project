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


        public void SwitchRoom(int direction)
        {
            if (rooms == null || rooms.Length == 0) return;

            int n = rooms.Length;
            int newIndex = (currentRoomIndex + direction) % n;
            if (newIndex < 0) newIndex += n;

            if (newIndex != currentRoomIndex)
            {
                currentRoomIndex = newIndex;
                ShowRoom(currentRoomIndex);
            }
        }

        // 新增：允許外部直接指定索引切換（RoomUIManager 會呼叫它以同步實際房間）
        public void SetRoomIndex(int index)
        {
            if (rooms == null || rooms.Length == 0) return;
            int newIndex = Mathf.Clamp(index, 0, rooms.Length - 1);
            if (newIndex != currentRoomIndex)
            {
                currentRoomIndex = newIndex;
                ShowRoom(currentRoomIndex);
            }
        }

        void ShowRoom(int index)
        {
            RoomHelper.ActivateOnly(rooms, index);
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