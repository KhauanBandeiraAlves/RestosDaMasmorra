using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RestosDaMasmorra.Characters.Combat;
using RestosDaMasmorra.Core;
using RestosDaMasmorra.Dungeon;
using RestosDaMasmorra.Economy;
using RestosDaMasmorra.Items;
using RestosDaMasmorra.Player;
using RestosDaMasmorra.UI;

namespace RestosDaMasmorra.EditorTools
{
    // Screenshot + smoke-test tool for Phase D. Runs entirely in Edit Mode (no real Play
    // Mode / scene loads involved), so it can't hit the instability that PlayMode
    // SceneManager.LoadScene calls caused — see SceneLoadGate.
    public static class PhaseDValidationTool
    {
        public static void CaptureBaseWorkshop()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeBase.unity", OpenSceneMode.Single);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Camera cam = Camera.main;
            if (player == null || cam == null)
            {
                Debug.LogError("PhaseDValidationTool: missing player or camera in PrototypeBase.");
                return;
            }

            Object.FindFirstObjectByType<PlayerHUD>()?.RefreshNow();
            cam.transform.position = player.transform.position + new Vector3(-9f, 15f, -9f);
            SceneValidationTool.RenderCameraToPng(cam, "Docs/Validation/Base_Workshop.png", 1280, 720);
            Debug.Log("Captured Docs/Validation/Base_Workshop.png");
        }

        public static void CaptureDungeonHudAndExtraction()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeDungeon.unity", OpenSceneMode.Single);

            DungeonRuntimeSpawner spawner = Object.FindFirstObjectByType<DungeonRuntimeSpawner>();
            if (spawner == null)
            {
                Debug.LogError("PhaseDValidationTool: no DungeonRuntimeSpawner in PrototypeDungeon.");
                return;
            }
            spawner.GenerateAndBuild();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Camera cam = Camera.main;
            if (player == null || cam == null)
            {
                Debug.LogError("PhaseDValidationTool: missing player or camera after generation.");
                return;
            }

            PlayerHUD hud = Object.FindFirstObjectByType<PlayerHUD>();
            hud?.RefreshNow();
            cam.transform.position = player.transform.position + new Vector3(-6.21f, 12.79f, -6.21f);
            SceneValidationTool.RenderCameraToPng(cam, "Docs/Validation/Dungeon_HUD.png", 1280, 720);
            Debug.Log("Captured Docs/Validation/Dungeon_HUD.png");

            ExtractionPoint extraction = Object.FindFirstObjectByType<ExtractionPoint>();
            if (extraction != null)
            {
                extraction.Interact(player); // arms the confirmation prompt, doesn't extract yet
                hud?.RefreshNow();
                cam.transform.position = extraction.transform.position + new Vector3(-4f, 6f, -4f);
                SceneValidationTool.RenderCameraToPng(cam, "Docs/Validation/Extraction.png", 1280, 720);
                Debug.Log("Captured Docs/Validation/Extraction.png");
            }
            else
            {
                Debug.LogError("PhaseDValidationTool: no ExtractionPoint found after generation.");
            }
        }

        public static void CaptureDismantling()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeBase.unity", OpenSceneMode.Single);

            if (GameSession.Instance == null)
            {
                GameObject sessionGO = new GameObject("GameSession", typeof(GameSession));
                sessionGO.GetComponent<GameSession>().Initialize();
            }

            ItemDefinition brokenSword = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/ScriptableObjects/Items/Broken_Sword.asset");
            if (brokenSword != null) GameSession.Instance.Storage.AddStack(brokenSword, 3);
            ItemDefinition bone = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/ScriptableObjects/Items/Bone.asset");
            if (bone != null) GameSession.Instance.Storage.AddStack(bone, 8);

            DismantlingBench bench = Object.FindFirstObjectByType<DismantlingBench>();
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Camera cam = Camera.main;
            if (bench == null || player == null || cam == null)
            {
                Debug.LogError("PhaseDValidationTool: missing bench/player/camera in PrototypeBase.");
                return;
            }

            bench.Interact(player);

            cam.transform.position = bench.transform.position + new Vector3(-3f, 5f, -3f);
            SceneValidationTool.RenderCameraToPng(cam, "Docs/Validation/Dismantling.png", 1280, 720);
            Debug.Log("Captured Docs/Validation/Dismantling.png");
        }

        public static void CapturePlayerDefeated()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeDungeon.unity", OpenSceneMode.Single);

            DungeonRuntimeSpawner spawner = Object.FindFirstObjectByType<DungeonRuntimeSpawner>();
            spawner?.GenerateAndBuild();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Camera cam = Camera.main;
            if (player == null || cam == null)
            {
                Debug.LogError("PhaseDValidationTool: missing player or camera.");
                return;
            }

            Health health = player.GetComponent<Health>();
            health.TakeDamage(health.MaxHealth); // Awake/OnEnable haven't run in Edit Mode,
                                                  // so this only drives the HUD's health
                                                  // reading to zero — it does not trigger a
                                                  // real scene load here.

            Object.FindFirstObjectByType<PlayerHUD>()?.RefreshNow();
            cam.transform.position = player.transform.position + new Vector3(-4f, 6f, -4f);
            SceneValidationTool.RenderCameraToPng(cam, "Docs/Validation/Player_Defeated.png", 1280, 720);
            Debug.Log("Captured Docs/Validation/Player_Defeated.png");
        }

        public static void RunSmokeTest()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeDungeon.unity", OpenSceneMode.Single);
            DungeonRuntimeSpawner spawner = Object.FindFirstObjectByType<DungeonRuntimeSpawner>();
            if (spawner == null)
            {
                Debug.LogError("PhaseDValidationTool: no DungeonRuntimeSpawner.");
                return;
            }
            spawner.GenerateAndBuild();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            bool hasHealth = player != null && player.GetComponent<Health>() != null;
            bool hasCombatant = player != null && player.GetComponent<PlayerCombatant>() != null;
            bool hasLifeController = player != null && player.GetComponent<PlayerLifeController>() != null;
            ExtractionPoint extraction = Object.FindFirstObjectByType<ExtractionPoint>();

            bool partyOk = spawner.Party != null && spawner.Party.Count == 3;
            bool enemiesOk = spawner.Enemies != null && spawner.Enemies.Count > 0;
            bool extractionOk = extraction != null;

            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeBase.unity", OpenSceneMode.Single);
            bool hasStorage = Object.FindFirstObjectByType<StorageInteractable>() != null;
            bool hasBench = Object.FindFirstObjectByType<DismantlingBench>() != null;

            bool overall = hasHealth && hasCombatant && hasLifeController && partyOk && enemiesOk && extractionOk && hasStorage && hasBench;

            Debug.Log(
                "PhaseDValidationTool smoke test:\n" +
                $"  Player Health/Combatant/LifeController: {hasHealth}/{hasCombatant}/{hasLifeController}\n" +
                $"  Party spawned: {partyOk} ({spawner.Party?.Count ?? 0}/3)\n" +
                $"  Enemies spawned: {enemiesOk} ({spawner.Enemies?.Count ?? 0})\n" +
                $"  ExtractionPoint present: {extractionOk}\n" +
                $"  Base Storage present: {hasStorage}\n" +
                $"  Base DismantlingBench present: {hasBench}\n" +
                $"  RESULT: {(overall ? "PASS" : "FAIL")}");
        }
    }
}
