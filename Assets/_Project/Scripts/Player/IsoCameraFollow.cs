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
        [SerializeField] float pitch = 58f;
        [SerializeField] float yaw = 0f;
        [SerializeField] float distance = 14f;
        [SerializeField] float orthographicSize = 6.5f;
        [SerializeField] float followSmoothTime = 0.12f;

        Vector3 followVelocity;
        Camera cam;

        bool hasRoomBounds;
        Bounds roomBounds;

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

        // Constrains the visible ground area to a room. The camera keeps following the
        // player, but the look-at focus point gets clamped so the orthographic frame never
        // reveals empty space outside the room (see ClampFocus). Callers own the transition
        // between rooms (e.g. RoomCameraTracker for the procedural dungeon, or a fixed call
        // for a static single-room scene) -- this class only ever knows about "the current
        // room bounds", not room-to-room navigation.
        public void SetRoomBounds(Bounds bounds)
        {
            hasRoomBounds = true;
            roomBounds = bounds;
        }

        public void ClearRoomBounds() => hasRoomBounds = false;

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

        Vector3 TargetPosition(Vector3 focus) => ClampFocusToRoom(focus) - (transform.rotation * Vector3.forward) * distance;

        // Approximates the ground rectangle this orthographic camera can see (half-extents
        // in world X/Z) and clamps the focus point so that rectangle never crosses the room
        // bounds. The Z half-extent divides by sin(pitch) because a tilted camera's vertical
        // screen extent maps to a taller world-space strip along the view direction than the
        // flat orthographicSize would suggest -- exact only at pitch 90, a deliberately
        // simple approximation everywhere else (good enough for a prototype clamp, not meant
        // to be pixel-perfect). If the room is smaller than the camera's view on an axis, that
        // axis just locks to the room's center instead of clamping to a degenerate range.
        Vector3 ClampFocusToRoom(Vector3 focus)
        {
            if (!hasRoomBounds) return focus;

            float aspect = cam != null && cam.pixelHeight > 0 ? cam.aspect : 16f / 9f;
            float halfX = orthographicSize * aspect;
            float halfZ = orthographicSize / Mathf.Max(0.35f, Mathf.Sin(pitch * Mathf.Deg2Rad));

            float minX = roomBounds.min.x + halfX;
            float maxX = roomBounds.max.x - halfX;
            float minZ = roomBounds.min.z + halfZ;
            float maxZ = roomBounds.max.z - halfZ;

            float x = minX <= maxX ? Mathf.Clamp(focus.x, minX, maxX) : roomBounds.center.x;
            float z = minZ <= maxZ ? Mathf.Clamp(focus.z, minZ, maxZ) : roomBounds.center.z;
            return new Vector3(x, focus.y, z);
        }

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
