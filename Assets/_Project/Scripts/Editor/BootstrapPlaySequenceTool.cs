using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RestosDaMasmorra.Economy;

namespace RestosDaMasmorra.EditorTools
{
    // Runs Bootstrap.unity in real Play Mode (not by opening PrototypeBase directly) so the
    // resulting screenshot is genuine evidence of the Bootstrap -> PrototypeBase flow.
    // Same SessionState pattern as PartyPlaySequenceTool: Play Mode causes a domain reload
    // that wipes static fields, so state must survive it via SessionState, and the update
    // subscription is re-attached unconditionally by the static constructor on every load.
    [InitializeOnLoad]
    public static class BootstrapPlaySequenceTool
    {
        const string ActiveKey = "BPST_Active";
        const string TickKey = "BPST_Tick";
        const int SafetyTickLimit = 3000;

        static BootstrapPlaySequenceTool()
        {
            EditorApplication.update += OnUpdate;
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Bootstrap.unity", OpenSceneMode.Single);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(TickKey, 0);
            EditorApplication.isPlaying = true;
        }

        static void OnUpdate()
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;

            int tick = SessionState.GetInt(TickKey, 0) + 1;
            SessionState.SetInt(TickKey, tick);

            if (tick > SafetyTickLimit)
            {
                Debug.LogError("BootstrapPlaySequenceTool: safety tick limit reached without reaching PrototypeBase.");
                SessionState.SetBool(ActiveKey, false);
                EditorApplication.Exit(1);
                return;
            }

            if (SceneManager.GetActiveScene().name != "PrototypeBase") return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Camera cam = Camera.main;
            if (player == null || cam == null) return;

            // Give the player/camera a couple more frames to settle (camera-follow, etc.)
            // before capturing.
            if (tick < 5) return;

            cam.transform.LookAt(player.transform.position + Vector3.up);
            SceneValidationTool.RenderCameraToPng(cam, "Docs/Validation/Boot_To_Base.png", 1280, 720);
            Debug.Log("BootstrapPlaySequenceTool: captured Docs/Validation/Boot_To_Base.png after real Bootstrap -> PrototypeBase flow.");

            SessionState.SetBool(ActiveKey, false);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }
}
