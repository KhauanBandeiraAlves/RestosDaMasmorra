using UnityEngine;

namespace RestosDaMasmorra.Player
{
    // Fixed-rotation "dungeon crawler" camera: rotation/zoom are derived from
    // pitch/yaw/distance/orthographicSize rather than a hand-placed offset, so editor
    // tooling can preview/compare presets (see CameraPresetTool) without entering Play
    // Mode. Only follows the target's XZ position — rotation never changes and there is
    // no orbit/zoom input, by design.
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class IsoCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float pitch = 70f;
        [SerializeField] float yaw = 0f;
        [SerializeField] float distance = 14f;
        [SerializeField] float orthographicSize = 7.0f;
        [SerializeField] float followSmoothTime = 0.12f;

        Vector3 followVelocity;
        Camera cam;

        void OnEnable()
        {
            if (cam == null) cam = GetComponent<Camera>();
            ApplyRotationAndZoom();
        }

        void LateUpdate()
        {
            if (cam == null) cam = GetComponent<Camera>();

            if (!Application.isPlaying)
            {
                ApplyRotationAndZoom();
                return;
            }

            if (target == null) return;

            Vector3 desired = TargetPosition(target.position);
            Vector3 current = transform.position;
            float x = Mathf.SmoothDamp(current.x, desired.x, ref followVelocity.x, followSmoothTime);
            float z = Mathf.SmoothDamp(current.z, desired.z, ref followVelocity.z, followSmoothTime);
            transform.position = new Vector3(x, desired.y, z);
        }

        public void SetTarget(Transform newTarget) => target = newTarget;

        // Recomputes the fixed rotation and orthographic zoom from the current preset
        // fields, and snaps position to the target (no smoothing). Runs every edit-mode
        // frame via ExecuteAlways so scene-view previews stay live, and is also the entry
        // point editor tooling uses to test presets deterministically.
        public void ApplyRotationAndZoom()
        {
            if (cam == null) cam = GetComponent<Camera>();
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;

            if (target != null) transform.position = TargetPosition(target.position);
        }

        Vector3 TargetPosition(Vector3 focus) => focus - (transform.rotation * Vector3.forward) * distance;

        public void ApplyPreset(float newPitch, float newYaw, float newDistance, float newOrthographicSize)
        {
            pitch = newPitch;
            yaw = newYaw;
            distance = newDistance;
            orthographicSize = newOrthographicSize;
            ApplyRotationAndZoom();
        }
    }
}
