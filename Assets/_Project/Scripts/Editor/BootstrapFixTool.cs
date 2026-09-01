using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RestosDaMasmorra.Core;

namespace RestosDaMasmorra.EditorTools
{
    // One-off fix: Bootstrap.unity never actually had the Bootstrap component placed in
    // it — only the script existed. Also wires PrototypeBase.unity with a SceneBootLogger
    // and sets Bootstrap.unity as the Editor's "Play from here" scene, so pressing Play
    // from any dev scene still starts the real flow.
    public static class BootstrapFixTool
    {
        public static void FixAll()
        {
            FixBootstrapScene();
            FixPrototypeBaseScene();
            ClearPlayModeStartScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("BootstrapFixTool: FixAll complete.");
        }

        static void FixBootstrapScene()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Bootstrap.unity", OpenSceneMode.Single);

            Bootstrap existing = Object.FindFirstObjectByType<Bootstrap>();
            if (existing != null)
            {
                Debug.Log("BootstrapFixTool: Bootstrap component already present, nothing to add.");
                return;
            }

            GameObject root = new GameObject("BootstrapRoot");
            root.AddComponent<Bootstrap>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("BootstrapFixTool: added BootstrapRoot (Bootstrap component) to Bootstrap.unity.");
        }

        static void FixPrototypeBaseScene()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeBase.unity", OpenSceneMode.Single);

            SceneBootLogger existing = Object.FindFirstObjectByType<SceneBootLogger>();
            if (existing == null)
            {
                GameObject loggerGO = new GameObject("SceneBootLogger");
                SceneBootLogger logger = loggerGO.AddComponent<SceneBootLogger>();
                SerializedObject so = new SerializedObject(logger);
                so.FindProperty("sceneLabel").stringValue = "PROTOTYPE BASE";
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("BootstrapFixTool: added SceneBootLogger to PrototypeBase.unity.");
            }
        }

        // EditorSceneManager.playModeStartScene also hijacks the automated Test Runner's
        // Play Mode sessions (every PlayMode test would boot through Bootstrap ->
        // PrototypeBase first, polluting the test scene with KayKit props whose meshes
        // aren't Read/Write enabled and breaking NavMesh bakes in unrelated tests). Bootstrap
        // is already first in Build Settings, which is the safe "default scene" fallback, so
        // we deliberately do not use playModeStartScene.
        static void ClearPlayModeStartScene()
        {
            if (EditorSceneManager.playModeStartScene != null)
            {
                EditorSceneManager.playModeStartScene = null;
                Debug.Log("BootstrapFixTool: cleared Editor Play Mode Start Scene (would have broken automated PlayMode tests).");
            }
        }
    }
}
