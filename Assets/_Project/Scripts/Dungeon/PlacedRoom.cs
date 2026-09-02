using System.Collections.Generic;
using UnityEngine;

namespace RestosDaMasmorra.Dungeon
{
    // A room placed by the generator. Purely logical (no scene GameObjects involved) so
    // layout computation stays cheap enough to run thousands of times for validation.
    public class PlacedRoom
    {
        public GameObject Prefab;
        public RoomDefinition Definition;
        public Vector3 Position;
        public float YawDegrees;
        public int Depth;
        public bool IsMainPath;

        // (own socket index in Definition.GetSockets(), other room, other socket index)
        public List<(int localSocketIndex, PlacedRoom other, int otherSocketIndex)> Connections =
            new List<(int, PlacedRoom, int)>();

        public HashSet<int> ConnectedSocketIndices = new HashSet<int>();

        public Vector2 EffectiveSize()
        {
            Vector2 size = Definition.Size;
            int steps = Mathf.RoundToInt(Mathf.Repeat(YawDegrees, 360f) / 90f);
            bool swapped = steps == 1 || steps == 3;
            return swapped ? new Vector2(size.y, size.x) : size;
        }

        public Rect WorldRect()
        {
            Vector2 size = EffectiveSize();
            return new Rect(Position.x - size.x * 0.5f, Position.z - size.y * 0.5f, size.x, size.y);
        }

        // 3D bounds for camera room-clamping (see IsoCameraFollow.SetRoomBounds). Height
        // isn't tracked per-room today, so this takes the same wall height DungeonSceneBuilder
        // builds with as a default.
        public Bounds WorldBounds(float height = 4f)
        {
            Rect rect = WorldRect();
            return new Bounds(
                new Vector3(rect.center.x, height * 0.5f, rect.center.y),
                new Vector3(rect.width, height, rect.height));
        }

        public Vector3 SocketWorldPosition(RoomSocket socket)
        {
            Vector3 rotatedLocal = Quaternion.AngleAxis(YawDegrees, Vector3.up) * socket.transform.localPosition;
            return Position + rotatedLocal;
        }

        public SocketDirection SocketWorldDirection(RoomSocket socket)
        {
            return socket.Direction.RotatedBy(YawDegrees);
        }
    }
}
