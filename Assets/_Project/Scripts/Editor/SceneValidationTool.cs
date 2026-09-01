using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RestosDaMasmorra.EditorTools
{
    // Batch-mode diagnostic + screenshot tool. Not gameplay code — safe to remove once
    // scene validation is no longer needed as a standalone step.
    public static class SceneValidationTool
    {
        public static void ValidateAndScreenshotArtTestScene()
        {
            string scenePath = "Assets/_Project/Scenes/ArtTestScene.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var report = new StringBuilder();
            report.AppendLine("=== ArtTestScene diagnostic ===");

            GameObject[] roots = scene.GetRootGameObjects();
            report.AppendLine($"Root objects: {roots.Length}");

            int rendererCount = 0;
            int nullMeshCount = 0;
            int inactiveCount = 0;
            bool hasBounds = false;
            Bounds sceneBounds = new Bounds();

            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                rendererCount++;
                if (!r.gameObject.activeInHierarchy) inactiveCount++;

                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh == null) nullMeshCount++;

                if (r.gameObject.activeInHierarchy && r.enabled)
                {
                    if (!hasBounds) { sceneBounds = r.bounds; hasBounds = true; }
                    else sceneBounds.Encapsulate(r.bounds);
                }
            }

            report.AppendLine($"Renderers found: {rendererCount}");
            report.AppendLine($"Renderers with null mesh: {nullMeshCount}");
            report.AppendLine($"Inactive renderer objects: {inactiveCount}");
            report.AppendLine($"Combined renderer bounds: {(hasBounds ? sceneBounds.ToString() : "NONE")}");

            Camera cam = Camera.main;
            if (cam == null)
            {
                Camera[] allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (allCams.Length > 0) cam = allCams[0];
            }

            if (cam == null)
            {
                report.AppendLine("ERROR: no Camera found in scene.");
            }
            else
            {
                report.AppendLine($"Camera GameObject: {cam.gameObject.name}, active={cam.gameObject.activeInHierarchy}, tag={cam.tag}");
                report.AppendLine($"Camera position: {cam.transform.position}, rotation euler: {cam.transform.rotation.eulerAngles}");
                report.AppendLine($"Camera orthographic: {cam.orthographic}, size: {cam.orthographicSize}");
                report.AppendLine($"Camera culling mask: {cam.cullingMask}");

                if (hasBounds)
                {
                    Vector3 toCenter = sceneBounds.center - cam.transform.position;
                    float dot = Vector3.Dot(toCenter.normalized, cam.transform.forward);
                    report.AppendLine($"Dot(cameraForward, dirToContentCenter) = {dot:F3} (should be > 0, ideally close to 1 if aimed well)");
                }

                RenderCameraToPng(cam, "Docs/Validation/ArtTestScene.png", 1280, 720);
                report.AppendLine("Screenshot written to Docs/Validation/ArtTestScene.png");
            }

            string reportPath = "Docs/Validation/ArtTestScene_Diagnostic.txt";
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log(report.ToString());
        }

        public static void ScreenshotPrototypeBase() => ScreenshotScene("Assets/_Project/Scenes/PrototypeBase.unity", "Docs/Validation/PrototypeBase.png");

        public static void ScreenshotPrototypeDungeon() => ScreenshotScene("Assets/_Project/Scenes/PrototypeDungeon.unity", "Docs/Validation/PrototypeDungeon.png");

        static void ScreenshotScene(string scenePath, string outputPath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError($"SceneValidationTool: no main camera found in {scenePath}");
                return;
            }
            RenderCameraToPng(cam, outputPath, 1280, 720);
            Debug.Log($"Screenshot written to {outputPath}");
        }

        public static void RenderCameraToPng(Camera cam, string relativeOutputPath, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevTarget = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(rt);

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativeOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, png);
        }
    }
}
