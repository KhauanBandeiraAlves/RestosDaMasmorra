using UnityEngine;

namespace RestosDaMasmorra.Player
{
    // Applies a fixed room Bounds to the camera on enable -- for static single-room scenes
    // (PrototypeBase) that have no room-to-room transition to track. The procedural dungeon
    // uses RoomCameraTracker instead, which calls IsoCameraFollow.SetRoomBounds directly as
    // the player moves between rooms.
    [ExecuteAlways]
    [RequireComponent(typeof(IsoCameraFollow))]
    public class FixedRoomBounds : MonoBehaviour
    {
        [SerializeField] Vector3 center;
        [SerializeField] Vector3 size;

        void OnEnable() => Apply();

        public void Configure(Bounds bounds)
        {
            center = bounds.center;
            size = bounds.size;
            Apply();
        }

        void Apply() => GetComponent<IsoCameraFollow>().SetRoomBounds(new Bounds(center, size));
    }
}
