using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RestosDaMasmorra.Dungeon;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.EditorTools
{
    // One-off orchestration: builds the 8 prototype room prefabs, the prototype
    // DungeonDefinition asset, and rewires PrototypeDungeon.unity to generate its layout
    // procedurally via DungeonRuntimeSpawner instead of the Phase A hand-built room.
    public static class DungeonPrototypeBuilder
    {
        const string RoomsFolder = "Assets/_Project/Prefabs/Dungeon/";
        const string DungeonDefFolder = "Assets/_Project/ScriptableObjects/Dungeon/";
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";

        public static void BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Directory.CreateDirectory(RoomsFolder);
            Directory.CreateDirectory(DungeonDefFolder);

            GameObject entrance = RoomPrefabFactory.BuildRoom(RoomsFolder + "Room_Entrance.prefab",
                RoomType.Entrance, new Vector2(12, 12), new[] { SocketDirection.North });

            GameObject combatStraight = RoomPrefabFactory.BuildRoom(RoomsFolder + "Room_Combat_Straight.prefab",
                RoomType.Combat, new Vector2(16, 16), new[] { SocketDirection.South, SocketDirection.North }, weight: 1f);

            GameObject combatBranch = RoomPrefabFactory.BuildRoom(RoomsFolder + "Room_Combat_Branch.prefab",
                RoomType.Combat, new Vector2(16, 16), new[] { SocketDirection.South, SocketDirection.North, SocketDirection.East }, weight: 0.8f);

            GameObject combatNarrow = RoomPrefabFactory.BuildRoom(RoomsFolder + "Room_Combat_Narrow.prefab",
                RoomType.Combat, new Vector2(12, 16), new[] { SocketDirection.South, SocketDirection.North }, weight: 1f);

            GameObject corridor = RoomPrefabFactory.BuildRoom(RoomsFolder + "Room_Corridor.prefab",
                RoomType.Corridor, new Vector2(4, 12), new[] { SocketDirection.South, SocketDirection.North }, weight: 1.2f);

            GameObject resource = RoomPrefabFactory.BuildRoom(RoomsFolder + "Room_Resource.prefab",
                RoomType.Resource, new Vector2(12, 12), new[] { SocketDirection.South }, weight: 0.7f, minDepth: 1);

            GameObject deadEnd = RoomPrefabFactory.BuildRoom(RoomsFolder + "Room_DeadEnd.prefab",
                RoomType.DeadEnd, new Vector2(12, 12), new[] { SocketDirection.South }, weight: 0.6f);

            GameObject boss = RoomPrefabFactory.BuildRoom(RoomsFolder + "Room_Boss.prefab",
                RoomType.Boss, new Vector2(16, 20), new[] { SocketDirection.South }, canRepeat: false);

            List<GameObject> pool = new List<GameObject> { combatStraight, combatBranch, combatNarrow, corridor, resource, deadEnd };

            string defPath = DungeonDefFolder + "PrototypeDungeonDefinition.asset";
            DungeonDefinition definition = AssetDatabase.LoadAssetAtPath<DungeonDefinition>(defPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<DungeonDefinition>();
                AssetDatabase.CreateAsset(definition, defPath);
            }
            definition.EditorConfigure("prototype_dungeon", 6, 10, 2, "Dungeon", entrance, boss, pool);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();

            RebuildPrototypeDungeonScene(definition);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("DungeonPrototypeBuilder: BuildAll complete.");
        }

        static void RebuildPrototypeDungeonScene(DungeonDefinition definition)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.transform.position = Vector3.zero;

            GameObject dungeonRootGO = new GameObject("DungeonRoot");
            DungeonRuntimeSpawner spawner = dungeonRootGO.AddComponent<DungeonRuntimeSpawner>();
            spawner.EditorConfigure(definition, 12345, playerInstance.transform, "PrototypeBase");

            PrototypeSceneBuilder.SetupLightingAndCamera(playerInstance.transform, out Canvas canvas);
            PrototypeSceneBuilder.WirePlayerHud(canvas, playerInstance);

            Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/PrototypeDungeon.unity");
        }
    }
}
