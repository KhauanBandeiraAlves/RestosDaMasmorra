using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RestosDaMasmorra.EditorTools
{
    public static class ProtoBuildTool
    {
        public static void BuildCoreLoopWindows()
        {
            string[] scenes =
            {
                "Assets/_Project/Scenes/Bootstrap.unity",
                "Assets/_Project/Scenes/PrototypeBase.unity",
                "Assets/_Project/Scenes/PrototypeDungeon.unity",
            };

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/Prototype_CoreLoop/RestosDaMasmorra.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            Debug.Log($"ProtoBuildTool: build result = {report.summary.result}, " +
                      $"totalErrors = {report.summary.totalErrors}, " +
                      $"totalWarnings = {report.summary.totalWarnings}, " +
                      $"size = {report.summary.totalSize} bytes, " +
                      $"output = {report.summary.outputPath}");
        }
    }
}
