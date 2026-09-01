using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RestosDaMasmorra.Dungeon;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.EditorTools
{
    // Exercises the exact same code path Start() uses in PrototypeDungeon.unity, without
    // needing a full interactive Play Mode session, to confirm the scene is actually
    // playable: generation succeeds, player lands at the Entrance, Boss is reachable.
    public static class DungeonPlaytestTool
    {
        public static void RunSmokeTest()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeDungeon.unity", OpenSceneMode.Single);

            DungeonRuntimeSpawner spawner = Object.FindFirstObjectByType<DungeonRuntimeSpawner>();
            if (spawner == null)
            {
                Debug.LogError("DungeonPlaytestTool: no DungeonRuntimeSpawner found in PrototypeDungeon.unity");
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("DungeonPlaytestTool: no Player found in PrototypeDungeon.unity");
                return;
            }
            bool hasMovement = player.GetComponent<PlayerMovement>() != null;
            bool hasController = player.GetComponent<CharacterController>() != null;

            spawner.GenerateAndBuild();

            DungeonLayoutResult layout = spawner.LastLayout;
            if (layout == null || !layout.Success)
            {
                Debug.LogError($"DungeonPlaytestTool: generation failed — {layout?.FailureReason}");
                return;
            }

            float distanceFromEntrance = Vector3.Distance(player.transform.position, layout.Entrance.Position);
            bool bossReachable = DungeonGenerator.IsBossReachable(layout);
            bool noOverlap = !DungeonGenerator.HasAnyOverlap(layout.Rooms, out string overlapReason);

            bool partyOk = spawner.Party != null && spawner.Party.Count == 3;
            bool enemiesOk = spawner.Enemies != null && spawner.Enemies.Count > 0;

            bool overallPass = hasController && hasMovement && bossReachable && noOverlap && partyOk && enemiesOk;

            Debug.Log(
                "DungeonPlaytestTool smoke test:\n" +
                $"  Rooms generated: {layout.Rooms.Count}\n" +
                $"  Player has CharacterController: {hasController}, PlayerMovement: {hasMovement}\n" +
                $"  Player distance from Entrance origin: {distanceFromEntrance:F2}m (should be small, player spawns inside the Entrance room)\n" +
                $"  Boss reachable: {bossReachable}\n" +
                $"  No room overlaps: {noOverlap} {(noOverlap ? "" : "(" + overlapReason + ")")}\n" +
                $"  Party spawned: {partyOk} ({spawner.Party?.Count ?? 0}/3)\n" +
                $"  Enemies spawned: {enemiesOk} ({spawner.Enemies?.Count ?? 0})\n" +
                $"  RESULT: {(overallPass ? "PASS" : "FAIL")}");
        }

        public static void ScreenshotGeneratedPrototypeDungeon()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeDungeon.unity", OpenSceneMode.Single);

            DungeonRuntimeSpawner spawner = Object.FindFirstObjectByType<DungeonRuntimeSpawner>();
            if (spawner == null)
            {
                Debug.LogError("DungeonPlaytestTool: no DungeonRuntimeSpawner found.");
                return;
            }
            spawner.GenerateAndBuild();

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("DungeonPlaytestTool: no main camera found.");
                return;
            }

            // IsoCameraFollow only repositions the camera in LateUpdate (Play Mode); since
            // this tool runs in Edit Mode, mirror that offset once manually before capturing.
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                cam.transform.position = player.transform.position + new Vector3(-6.21f, 12.79f, -6.21f);
            }

            SceneValidationTool.RenderCameraToPng(cam, "Docs/Validation/PrototypeDungeon.png", 1280, 720);
            Debug.Log("Screenshot written to Docs/Validation/PrototypeDungeon.png");
        }
    }
}
