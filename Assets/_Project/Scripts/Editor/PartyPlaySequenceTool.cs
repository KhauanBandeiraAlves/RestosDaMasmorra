using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RestosDaMasmorra.Characters;
using RestosDaMasmorra.Dungeon;
using RestosDaMasmorra.Items;

namespace RestosDaMasmorra.EditorTools
{
    // Runs the actual PrototypeDungeon scene in real Play Mode (not just edit-mode
    // generation) so Update()-driven AI genuinely moves/fights, and grabs real
    // Unity-rendered screenshots at the moments that matter.
    //
    // Entering Play Mode triggers a domain reload (this project's Enter Play Mode
    // Options don't disable it), which wipes plain static fields and event subscriptions.
    // State is therefore kept in SessionState (survives domain reload within the same
    // Editor session) and the update subscription is re-established unconditionally by the
    // static constructor on every load.
    [InitializeOnLoad]
    public static class PartyPlaySequenceTool
    {
        const string ActiveKey = "PPST_Active";
        const string StageKey = "PPST_Stage";
        const string TickKey = "PPST_Tick";

        static PartyPlaySequenceTool()
        {
            EditorApplication.update += OnUpdate;
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeDungeon.unity", OpenSceneMode.Single);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetInt(TickKey, 0);
            EditorApplication.isPlaying = true;
        }

        const string TotalTickKey = "PPST_TotalTick";
        const int SafetyTotalTickLimit = 30000;

        static void OnUpdate()
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;

            Time.timeScale = 8f;

            int totalTick = SessionState.GetInt(TotalTickKey, 0) + 1;
            SessionState.SetInt(TotalTickKey, totalTick);
            if (totalTick > SafetyTotalTickLimit)
            {
                Debug.LogWarning("PartyPlaySequenceTool: safety tick limit reached, aborting sequence.");
                SessionState.SetBool(ActiveKey, false);
                EditorApplication.Exit(1);
                return;
            }

            int tick = SessionState.GetInt(TickKey, 0) + 1;
            SessionState.SetInt(TickKey, tick);
            int stage = SessionState.GetInt(StageKey, 0);

            DungeonRuntimeSpawner spawner = Object.FindFirstObjectByType<DungeonRuntimeSpawner>();
            if (spawner == null || spawner.Party == null || spawner.Party.Count == 0) return;

            FrameCameraOnParty(spawner);

            switch (stage)
            {
                case 0: // waiting to start
                    if (tick > 40)
                    {
                        Capture("Docs/Validation/Party_Exploring.png", tick);
                        SessionState.SetInt(StageKey, 1);
                        SessionState.SetInt(TickKey, 0);
                    }
                    break;

                case 1: // waiting for combat
                    bool anyDamaged = spawner.Enemies != null && spawner.Enemies.Any(e => e != null && (!e.IsAlive || e.CombatHealth.Current < e.CombatHealth.MaxHealth));
                    if (anyDamaged || tick > 4000)
                    {
                        Capture("Docs/Validation/Party_Combat.png", tick);
                        SessionState.SetInt(StageKey, 2);
                        SessionState.SetInt(TickKey, 0);
                    }
                    break;

                case 2: // waiting for loot
                    bool anyLoot = Object.FindObjectsByType<WorldItem>(FindObjectsSortMode.None).Length > 0;
                    if (anyLoot || tick > 2500)
                    {
                        Capture("Docs/Validation/Loot_After_Combat.png", tick);
                        SessionState.SetInt(StageKey, 3);
                    }
                    break;

                case 3: // done
                    SessionState.SetBool(ActiveKey, false);
                    Time.timeScale = 1f;
                    EditorApplication.isPlaying = false;
                    Debug.Log("PartyPlaySequenceTool: sequence complete.");
                    EditorApplication.Exit(0);
                    break;
            }
        }

        static void FrameCameraOnParty(DungeonRuntimeSpawner spawner)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 avg = Vector3.zero;
            int count = 0;
            foreach (AdventurerController a in spawner.Party)
            {
                if (a == null) continue;
                avg += a.transform.position;
                count++;
            }
            if (count == 0) return;
            avg /= count;

            cam.transform.position = avg + new Vector3(-6.21f, 12.79f, -6.21f);
        }

        static void Capture(string path, int tick)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            SceneValidationTool.RenderCameraToPng(cam, path, 1280, 720);
            Debug.Log($"PartyPlaySequenceTool: captured {path} at tick {tick}.");
        }
    }
}
