using UnityEngine;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.Dungeon
{
    // Keeps the camera's room bounds in sync with whichever room the player currently
    // stands in, so the dungeon camera never reveals neighboring rooms or the empty space
    // between them. Polls PlacedRoom.WorldRect() each frame instead of wiring a trigger per
    // room -- layout size is small (a handful of rooms), so this stays cheap.
    public class RoomCameraTracker : MonoBehaviour
    {
        DungeonRuntimeSpawner spawner;
        Transform player;
        IsoCameraFollow cameraFollow;
        PlacedRoom currentRoom;

        public void Configure(DungeonRuntimeSpawner dungeonSpawner, Transform playerTransform, IsoCameraFollow follow)
        {
            spawner = dungeonSpawner;
            player = playerTransform;
            cameraFollow = follow;
            currentRoom = null;
        }

        void Update()
        {
            if (spawner == null || spawner.LastLayout == null || !spawner.LastLayout.Success) return;
            if (player == null || cameraFollow == null) return;

            Vector2 playerXZ = new Vector2(player.position.x, player.position.z);
            if (currentRoom != null && currentRoom.WorldRect().Contains(playerXZ)) return;

            foreach (PlacedRoom room in spawner.LastLayout.Rooms)
            {
                if (!room.WorldRect().Contains(playerXZ)) continue;
                currentRoom = room;
                cameraFollow.SetRoomBounds(room.WorldBounds());
                return;
            }
        }
    }
}
