using UnityEngine;

namespace RestosDaMasmorra.Dungeon
{
    public enum SocketDirection
    {
        North,
        South,
        East,
        West
    }

    public static class SocketDirectionExtensions
    {
        // Y-rotation angle (degrees) matching each cardinal direction, clockwise from North.
        public static float ToYawDegrees(this SocketDirection dir)
        {
            switch (dir)
            {
                case SocketDirection.North: return 0f;
                case SocketDirection.East: return 90f;
                case SocketDirection.South: return 180f;
                case SocketDirection.West: return 270f;
                default: return 0f;
            }
        }

        public static SocketDirection Opposite(this SocketDirection dir)
        {
            switch (dir)
            {
                case SocketDirection.North: return SocketDirection.South;
                case SocketDirection.South: return SocketDirection.North;
                case SocketDirection.East: return SocketDirection.West;
                case SocketDirection.West: return SocketDirection.East;
                default: return dir;
            }
        }

        public static Vector3 ToLocalVector(this SocketDirection dir)
        {
            switch (dir)
            {
                case SocketDirection.North: return new Vector3(0f, 0f, 1f);
                case SocketDirection.South: return new Vector3(0f, 0f, -1f);
                case SocketDirection.East: return new Vector3(1f, 0f, 0f);
                case SocketDirection.West: return new Vector3(-1f, 0f, 0f);
                default: return Vector3.forward;
            }
        }

        // Rotates a cardinal direction by a yaw that must be a multiple of 90 degrees.
        public static SocketDirection RotatedBy(this SocketDirection dir, float yawDegrees)
        {
            int steps = Mathf.RoundToInt(Mathf.Repeat(yawDegrees, 360f) / 90f);
            SocketDirection[] order = { SocketDirection.North, SocketDirection.East, SocketDirection.South, SocketDirection.West };
            int index = System.Array.IndexOf(order, dir);
            return order[(index + steps) % 4];
        }
    }
}
