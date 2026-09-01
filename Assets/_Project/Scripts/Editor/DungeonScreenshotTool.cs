using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RestosDaMasmorra.Dungeon;

namespace RestosDaMasmorra.EditorTools
{
    public static class DungeonScreenshotTool
    {
        const string DefinitionPath = "Assets/_Project/ScriptableObjects/Dungeon/PrototypeDungeonDefinition.asset";

        public static void CaptureThreeSeeds()
        {
            CaptureSeed(11, "Docs/Validation/Dungeon_Seed_A.png", false);
            CaptureSeed(47, "Docs/Validation/Dungeon_Seed_B.png", false);
            CaptureSeed(205, "Docs/Validation/Dungeon_Seed_C.png", false);
            CaptureSeed(47, "Docs/Validation/Dungeon_Overview.png", true);
        }

        public static void CaptureBranchExamples()
        {
            DungeonDefinition definition = AssetDatabase.LoadAssetAtPath<DungeonDefinition>(DefinitionPath);
            if (definition == null)
            {
                Debug.LogError($"DungeonScreenshotTool: could not load DungeonDefinition at {DefinitionPath}");
                return;
            }

            int? seedNoBranch = null, seedOneBranch = null, seedTwoBranches = null;
            int bestOverviewSeed = -1;
            int bestOverviewRooms = -1;

            for (int seed = 1; seed <= 3000; seed++)
            {
                DungeonLayoutResult r = DungeonGenerator.Generate(definition, seed);
                if (!r.Success) continue;

                if (seedNoBranch == null && r.BranchCount == 0) seedNoBranch = seed;
                if (seedOneBranch == null && r.BranchCount == 1) seedOneBranch = seed;
                if (seedTwoBranches == null && r.BranchCount == 2) seedTwoBranches = seed;

                if (r.BranchCount == 2 && r.Rooms.Count > bestOverviewRooms)
                {
                    bestOverviewRooms = r.Rooms.Count;
                    bestOverviewSeed = seed;
                }

                if (seedNoBranch != null && seedOneBranch != null && seedTwoBranches != null && bestOverviewRooms >= 9) break;
            }

            if (seedNoBranch != null) CaptureSeed(seedNoBranch.Value, "Docs/Validation/Dungeon_NoBranch.png", false);
            if (seedOneBranch != null) CaptureSeed(seedOneBranch.Value, "Docs/Validation/Dungeon_OneBranch.png", false);
            if (seedTwoBranches != null) CaptureSeed(seedTwoBranches.Value, "Docs/Validation/Dungeon_TwoBranches.png", false);
            if (bestOverviewSeed >= 0) CaptureSeed(bestOverviewSeed, "Docs/Validation/Dungeon_Overview.png", true);

            Debug.Log($"CaptureBranchExamples: 0-branch seed={seedNoBranch}, 1-branch seed={seedOneBranch}, 2-branch seed={seedTwoBranches}, overview seed={bestOverviewSeed} ({bestOverviewRooms} rooms).");
        }

        static void CaptureSeed(int seed, string outputPath, bool wideOverview)
        {
            DungeonDefinition definition = AssetDatabase.LoadAssetAtPath<DungeonDefinition>(DefinitionPath);
            if (definition == null)
            {
                Debug.LogError($"DungeonScreenshotTool: could not load DungeonDefinition at {DefinitionPath}");
                return;
            }

            DungeonLayoutResult layout = DungeonGenerator.Generate(definition, seed);
            if (!layout.Success)
            {
                Debug.LogError($"DungeonScreenshotTool: seed {seed} failed to generate — {layout.FailureReason}");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            DungeonBuildResult build = DungeonSceneBuilder.Build(layout, null);

            GameObject dirLightGO = new GameObject("Directional Light");
            Light dirLight = dirLightGO.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1f, 0.96f, 0.88f);
            dirLight.intensity = 1.1f;
            dirLightGO.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.4f);

            Bounds bounds = ComputeLayoutBounds(layout);

            GameObject camGO = new GameObject("Screenshot Camera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            camGO.transform.rotation = Quaternion.Euler(50f, 45f, 0f);

            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * (wideOverview ? 1.5f : 1.15f);
            cam.orthographicSize = Mathf.Max(radius, 6f);
            cam.farClipPlane = 500f;

            float distance = radius * 3f + 20f;
            camGO.transform.position = bounds.center - camGO.transform.forward * distance;

            SceneValidationTool.RenderCameraToPng(cam, outputPath, 1600, 900);
            Debug.Log($"DungeonScreenshotTool: seed {seed} -> {outputPath} ({layout.Rooms.Count} rooms).");
        }

        static Bounds ComputeLayoutBounds(DungeonLayoutResult layout)
        {
            bool has = false;
            Bounds b = new Bounds();
            foreach (PlacedRoom room in layout.Rooms)
            {
                Rect rect = room.WorldRect();
                Bounds roomBounds = new Bounds(
                    new Vector3(rect.center.x, 2f, rect.center.y),
                    new Vector3(rect.width, 4f, rect.height));
                if (!has) { b = roomBounds; has = true; }
                else b.Encapsulate(roomBounds);
            }
            return b;
        }
    }
}
