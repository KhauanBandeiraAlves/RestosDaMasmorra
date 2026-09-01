using UnityEngine;

namespace RestosDaMasmorra.Dungeon
{
    // Marks a connection point on a room prefab. Lives as a child transform of the room
    // root; its local position/rotation (as authored on the prefab) are read directly by
    // the generator without ever instantiating the prefab into a scene.
    public class RoomSocket : MonoBehaviour
    {
        [SerializeField] SocketDirection direction;

        public SocketDirection Direction => direction;

        // Build-time/runtime only flag used by the scene builder to know which sockets to cap.
        [System.NonSerialized] public bool ConnectedAtBuildTime;

        void OnDrawGizmos()
        {
            Gizmos.color = ConnectedAtBuildTime ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.95f, 0.25f, 0.2f);
            Gizmos.DrawSphere(transform.position, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + direction.ToLocalVector() * 1.5f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.35f);
        }
    }
}
