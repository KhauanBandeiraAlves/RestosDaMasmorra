using UnityEngine;

namespace RestosDaMasmorra.Player
{
    // Fixed isometric camera: follows the target's XZ position with a constant offset
    // and rotation. The player can never rotate or free-look the camera.
    [RequireComponent(typeof(Camera))]
    public class IsoCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(-6.21f, 12.79f, -6.21f);

        void LateUpdate()
        {
            if (target == null) return;
            transform.position = target.position + offset;
        }

        public void SetTarget(Transform newTarget) => target = newTarget;
    }
}
