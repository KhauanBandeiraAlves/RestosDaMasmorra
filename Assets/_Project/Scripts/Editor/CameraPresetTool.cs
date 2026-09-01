using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.EditorTools
{
    // Tests high-angle "dungeon crawler" camera presets (Moonlighter-style composition
    // philosophy, not art) against the real PrototypeBase scene content and captures real
    // screenshots for visual comparison, then applies whichever preset is chosen as the
    // project default across every playable scene.
    public static class CameraPresetTool
    {
        public static void CaptureAllPresets()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeBase.unity", OpenSceneMode.Single);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Camera cam = Camera.main;
            IsoCameraFollow follow = cam != null ? cam.GetComponent<IsoCameraFollow>() : null;
            if (player == null || cam == null || follow == null)
            {
                Debug.LogError("CameraPresetTool: missing Player/Camera/IsoCameraFollow in PrototypeBase.unity");
                return;
            }
            follow.SetTarget(player.transform);

            Capture(follow, cam, "A", 55f, 45f, 14f, 5.5f, "Docs/Validation/Camera_Preset_A.png");
            Capture(follow, cam, "B", 60f, 45f, 14f, 5.5f, "Docs/Validation/Camera_Preset_B.png");
            Capture(follow, cam, "C", 65f, 45f, 14f, 5.5f, "Docs/Validation/Camera_Preset_C.png");

            Debug.Log("CameraPresetTool: captured presets A/B/C.");
        }

        // Straight top-down / 3-4 presets: yaw 0 so room walls stay axis-aligned on screen
        // instead of reading as a rotated isometric diamond.
        public static void CaptureTopDownPresets()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeBase.unity", OpenSceneMode.Single);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Camera cam = Camera.main;
            IsoCameraFollow follow = cam != null ? cam.GetComponent<IsoCameraFollow>() : null;
            if (player == null || cam == null || follow == null)
            {
                Debug.LogError("CameraPresetTool: missing Player/Camera/IsoCameraFollow in PrototypeBase.unity");
                return;
            }
            follow.SetTarget(player.transform);

            Capture(follow, cam, "TopDown_A", 55f, 0f, 14f, 6.0f, "Docs/Validation/Camera_TopDown_A.png");
            Capture(follow, cam, "TopDown_B", 60f, 0f, 14f, 6.5f, "Docs/Validation/Camera_TopDown_B.png");
            Capture(follow, cam, "TopDown_C", 65f, 0f, 14f, 7.0f, "Docs/Validation/Camera_TopDown_C.png");
            Capture(follow, cam, "TopDown_D", 70f, 0f, 14f, 7.0f, "Docs/Validation/Camera_TopDown_D.png");

            Debug.Log("CameraPresetTool: captured top-down presets A/B/C/D.");
        }

        // Pitch-only comparison at fixed yaw 0 / orthoSize: 70 read as near-bird's-eye (lost
        // the player's front/body, chest/table volume flattened), so re-testing a lower
        // pitch band with everything else held constant for a fair side-by-side.
        public static void CaptureStraightPitchPresets()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeBase.unity", OpenSceneMode.Single);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Camera cam = Camera.main;
            IsoCameraFollow follow = cam != null ? cam.GetComponent<IsoCameraFollow>() : null;
            if (player == null || cam == null || follow == null)
            {
                Debug.LogError("CameraPresetTool: missing Player/Camera/IsoCameraFollow in PrototypeBase.unity");
                return;
            }
            follow.SetTarget(player.transform);

            const float orthoSize = 6.5f;
            const float distance = 14f;
            Capture(follow, cam, "Straight_55", 55f, 0f, distance, orthoSize, "Docs/Validation/Camera_Straight_55.png");
            Capture(follow, cam, "Straight_58", 58f, 0f, distance, orthoSize, "Docs/Validation/Camera_Straight_58.png");
            Capture(follow, cam, "Straight_60", 60f, 0f, distance, orthoSize, "Docs/Validation/Camera_Straight_60.png");
            Capture(follow, cam, "Straight_62", 62f, 0f, distance, orthoSize, "Docs/Validation/Camera_Straight_62.png");

            Debug.Log("CameraPresetTool: captured straight top-down pitch presets 55/58/60/62.");
        }

        static void Capture(IsoCameraFollow follow, Camera cam, string label, float pitch, float yaw, float distance, float orthoSize, string path)
        {
            follow.ApplyPreset(pitch, yaw, distance, orthoSize);
            SceneValidationTool.RenderCameraToPng(cam, path, 1280, 720);
            Debug.Log($"CameraPresetTool: captured preset {label} (pitch={pitch}, yaw={yaw}, distance={distance}, orthoSize={orthoSize}) -> {path}");
        }

        public static void ApplyFinalPreset(float pitch, float yaw, float distance, float orthoSize)
        {
            ApplyToScene("Assets/_Project/Scenes/PrototypeBase.unity", pitch, yaw, distance, orthoSize);
            ApplyToScene("Assets/_Project/Scenes/PrototypeDungeon.unity", pitch, yaw, distance, orthoSize);
        }

        // Chosen preset (see Docs/Validation/Camera_Straight_*.png comparison): pitch 70 read
        // as near-bird's-eye -- lost the player's front/body, chest/table volume flattened.
        // Re-tested 55/58/60/62 at yaw 0, orthoSize 6.5, distance 14 (fixed for a fair
        // comparison); 58 keeps the clearest front-of-character read while still being high
        // enough to hide the horizon and read as a straight top-down room.
        public static void ApplyChosenFinal()
        {
            ApplyFinalPreset(58f, 0f, 14f, 6.5f);
            CaptureFinal();
        }

        static void ApplyToScene(string scenePath, float pitch, float yaw, float distance, float orthoSize)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Deliberately does NOT call DungeonRuntimeSpawner.GenerateAndBuild() here: that
            // would bake a specific procedural layout + spawned party/enemies permanently
            // into PrototypeDungeon.unity on save, instead of leaving generation to happen
            // at runtime as designed. The Player prefab instance is already placed in the
            // scene (it's the spawner's serialized playerTransform / camera target), so it
            // can be found by tag without generating anything.
            Camera cam = Camera.main;
            IsoCameraFollow follow = cam != null ? cam.GetComponent<IsoCameraFollow>() : null;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (cam == null || follow == null)
            {
                Debug.LogError($"CameraPresetTool: missing Camera/IsoCameraFollow in {scenePath}");
                return;
            }
            if (player != null) follow.SetTarget(player.transform);
            follow.ApplyPreset(pitch, yaw, distance, orthoSize);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"CameraPresetTool: applied final camera preset to {scenePath}.");
        }

        public static void CaptureFinal()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeBase.unity", OpenSceneMode.Single);
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("CameraPresetTool: no main camera for final capture.");
                return;
            }
            SceneValidationTool.RenderCameraToPng(cam, "Docs/Validation/Camera_Final.png", 1280, 720);
            Debug.Log("CameraPresetTool: captured Camera_Final.png");
        }
    }
}
