using System.Linq;
using UnityEditor;
using UnityEngine;
using RestosDaMasmorra.Dungeon;

namespace RestosDaMasmorra.EditorTools
{
    public class DungeonBuilderWindow : EditorWindow
    {
        const string PreviewRootName = "GeneratedDungeon_EditorPreview";

        DungeonDefinition definition;
        int seed = 12345;
        string statusMessage = "";
        DungeonLayoutResult lastLayout;

        [MenuItem("Window/Restos da Masmorra/Dungeon Builder")]
        public static void Open()
        {
            GetWindow<DungeonBuilderWindow>("Dungeon Builder");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Restos da Masmorra — Dungeon Builder", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            definition = (DungeonDefinition)EditorGUILayout.ObjectField("Dungeon Definition", definition, typeof(DungeonDefinition), false);
            seed = EditorGUILayout.IntField("Seed", seed);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate")) Generate();
                if (GUILayout.Button("Generate Random Seed"))
                {
                    seed = Random.Range(1, int.MaxValue);
                    Generate();
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear Generated")) ClearGenerated();
                if (GUILayout.Button("Validate Rooms")) ValidateRooms();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(string.IsNullOrEmpty(statusMessage) ? "No generation run yet." : statusMessage, MessageType.Info);

            if (lastLayout != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Last Generation", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Seed", lastLayout.Seed.ToString());
                EditorGUILayout.LabelField("Success", lastLayout.Success.ToString());
                EditorGUILayout.LabelField("Rooms", lastLayout.Rooms.Count.ToString());
                EditorGUILayout.LabelField("Main path length", lastLayout.MainPath.Count.ToString());
                EditorGUILayout.LabelField("Branches", lastLayout.BranchCount.ToString());
                if (!lastLayout.Success)
                {
                    EditorGUILayout.HelpBox("Failure reason: " + lastLayout.FailureReason, MessageType.Error);
                }
            }
        }

        void Generate()
        {
            ClearGenerated();

            if (definition == null)
            {
                statusMessage = "Assign a Dungeon Definition first.";
                return;
            }

            lastLayout = DungeonGenerator.Generate(definition, seed);
            if (!lastLayout.Success)
            {
                statusMessage = $"Generation FAILED: {lastLayout.FailureReason}";
                return;
            }

            GameObject previewRoot = new GameObject(PreviewRootName);
            DungeonSceneBuilder.Build(lastLayout, previewRoot.transform);

            statusMessage = $"Generated {lastLayout.Rooms.Count} rooms (main path {lastLayout.MainPath.Count}, branches {lastLayout.BranchCount}) with seed {seed}.";
        }

        void ClearGenerated()
        {
            GameObject existing = GameObject.Find(PreviewRootName);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        void ValidateRooms()
        {
            if (definition == null)
            {
                statusMessage = "Assign a Dungeon Definition first.";
                return;
            }

            var issues = new System.Collections.Generic.List<string>();

            if (definition.EntrancePrefab == null) issues.Add("EntrancePrefab is not assigned.");
            else if (definition.EntrancePrefab.GetComponent<RoomDefinition>() == null) issues.Add("EntrancePrefab has no RoomDefinition.");
            else if (definition.EntrancePrefab.GetComponent<RoomDefinition>().GetSockets().Length == 0) issues.Add("EntrancePrefab has no sockets.");

            if (definition.BossPrefab == null) issues.Add("BossPrefab is not assigned.");
            else if (definition.BossPrefab.GetComponent<RoomDefinition>() == null) issues.Add("BossPrefab has no RoomDefinition.");
            else if (definition.BossPrefab.GetComponent<RoomDefinition>().GetSockets().Length == 0) issues.Add("BossPrefab has no sockets.");

            foreach (GameObject prefab in definition.RoomPool)
            {
                if (prefab == null) { issues.Add("RoomPool contains a null entry."); continue; }
                RoomDefinition def = prefab.GetComponent<RoomDefinition>();
                if (def == null) { issues.Add($"{prefab.name}: missing RoomDefinition."); continue; }

                RoomSocket[] sockets = def.GetSockets();
                if (sockets.Length == 0) issues.Add($"{prefab.name}: has no sockets.");

                foreach (RoomSocket s in sockets)
                {
                    if (s.transform.parent == null) issues.Add($"{prefab.name}: socket '{s.name}' has no parent transform.");
                }

                var directions = sockets.Select(s => s.Direction).ToList();
                if (directions.Count != directions.Distinct().Count())
                    issues.Add($"{prefab.name}: has more than one socket on the same side (unsupported by the current build step).");
            }

            statusMessage = issues.Count == 0
                ? "Validate Rooms: OK — no issues found."
                : "Validate Rooms found issues:\n- " + string.Join("\n- ", issues);
        }
    }
}
