using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RestosDaMasmorra.Dungeon;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.EditorTools
{
    // Real screenshots of the room-framing visual pass: the rebuilt PrototypeBase workshop,
    // and a few representative generated dungeon rooms (Entrance, a Combat room on the main
    // path, and a branch room off it), each captured with the room's own camera bounds
    // applied so the shot matches what the player actually sees in-room.
    public static class VisualPassScreenshotTool
    {
        public static void CaptureAll()
        {
            CaptureBaseRoom();
            CaptureDungeonRooms();
            Debug.Log("VisualPassScreenshotTool: capture complete.");
        }

        static void CaptureBaseRoom()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeBase.unity", OpenSceneMode.Single);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Camera cam = Camera.main;
            if (player == null || cam == null)
            {
                Debug.LogError("VisualPassScreenshotTool: missing Player/Camera in PrototypeBase.unity");
                return;
            }
            IsoCameraFollow follow = cam.GetComponent<IsoCameraFollow>();
            follow.SetTarget(player.transform);
            follow.ApplyRotationAndZoom();
            SceneValidationTool.RenderCameraToPng(cam, "Docs/Validation/Base_Room_Final.png", 1280, 720);
            Debug.Log("VisualPassScreenshotTool: captured Base_Room_Final.png");
        }

        static void CaptureDungeonRooms()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeDungeon.unity", OpenSceneMode.Single);

            DungeonRuntimeSpawner spawner = Object.FindFirstObjectByType<DungeonRuntimeSpawner>();
            if (spawner == null)
            {
                Debug.LogError("VisualPassScreenshotTool: no DungeonRuntimeSpawner in PrototypeDungeon.unity");
                return;
            }
            spawner.GenerateAndBuild();

            DungeonLayoutResult layout = spawner.LastLayout;
            if (layout == null || !layout.Success)
            {
                Debug.LogError($"VisualPassScreenshotTool: generation failed -- {layout?.FailureReason}");
                return;
            }

            Camera cam = Camera.main;
            IsoCameraFollow follow = cam != null ? cam.GetComponent<IsoCameraFollow>() : null;
            if (cam == null || follow == null)
            {
                Debug.LogError("VisualPassScreenshotTool: missing Camera/IsoCameraFollow in PrototypeDungeon.unity");
                return;
            }

            CaptureRoom(follow, cam, layout.Entrance, "Docs/Validation/Dungeon_Entrance_Final.png");

            PlacedRoom combatRoom = layout.MainPath.Find(r => r.Definition.RoomType == RoomType.Combat);
            if (combatRoom != null) CaptureRoom(follow, cam, combatRoom, "Docs/Validation/Dungeon_CombatRoom_Final.png");
            else Debug.LogWarning("VisualPassScreenshotTool: no Combat room found on main path for this seed.");

            PlacedRoom branchRoom = layout.Rooms.Find(r => !r.IsMainPath);
            if (branchRoom != null) CaptureRoom(follow, cam, branchRoom, "Docs/Validation/Dungeon_Branch_Final.png");
            else Debug.LogWarning("VisualPassScreenshotTool: no branch room found for this seed.");
        }

        static void CaptureRoom(IsoCameraFollow follow, Camera cam, PlacedRoom room, string path)
        {
            GameObject focusGO = new GameObject("__CaptureFocus");
            focusGO.transform.position = room.Position;

            follow.SetRoomBounds(room.WorldBounds());
            follow.SetTarget(focusGO.transform);
            follow.ApplyRotationAndZoom();

            SceneValidationTool.RenderCameraToPng(cam, path, 1280, 720);
            Debug.Log($"VisualPassScreenshotTool: captured {room.Definition.RoomType} room -> {path}");

            Object.DestroyImmediate(focusGO);
        }
    }
}
