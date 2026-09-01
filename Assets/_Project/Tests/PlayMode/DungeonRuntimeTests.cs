using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RestosDaMasmorra.Dungeon;

namespace RestosDaMasmorra.Tests.PlayMode
{
    public class DungeonRuntimeTests
    {
        static GameObject MakeRoom(RoomType type, Vector2 size, SocketDirection[] sockets, float weight = 1f, int minDepth = 0, int maxDepth = 99, bool canRepeat = true)
        {
            GameObject go = new GameObject("Room_" + type);

            // Real room prefabs always carry a floor mesh (from RoomPrefabFactory); mirror
            // that here so AddMeshColliders has something to attach a collider to.
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(go.transform, false);
            floor.transform.localScale = new Vector3(size.x, 0.1f, size.y);
            Object.DestroyImmediate(floor.GetComponent<Collider>());

            RoomDefinition def = go.AddComponent<RoomDefinition>();
            SetField(def, "roomType", type);
            SetField(def, "weight", weight);
            SetField(def, "minDepth", minDepth);
            SetField(def, "maxDepth", maxDepth);
            SetField(def, "canRepeat", canRepeat);
            SetField(def, "size", size);

            float halfW = size.x * 0.5f;
            float halfD = size.y * 0.5f;
            foreach (SocketDirection dir in sockets)
            {
                GameObject socketGO = new GameObject("Socket_" + dir);
                socketGO.transform.SetParent(go.transform, false);
                socketGO.transform.localPosition = dir switch
                {
                    SocketDirection.South => new Vector3(0f, 0f, -halfD),
                    SocketDirection.North => new Vector3(0f, 0f, halfD),
                    SocketDirection.West => new Vector3(-halfW, 0f, 0f),
                    SocketDirection.East => new Vector3(halfW, 0f, 0f),
                    _ => Vector3.zero
                };
                RoomSocket socket = socketGO.AddComponent<RoomSocket>();
                SetField(socket, "direction", dir);
            }
            return go;
        }

        static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        [UnityTest]
        public IEnumerator GeneratedDungeon_BuildsWithoutErrors_AndHasWalkableColliders()
        {
            GameObject entrance = MakeRoom(RoomType.Entrance, new Vector2(12, 12), new[] { SocketDirection.North });
            GameObject combat = MakeRoom(RoomType.Combat, new Vector2(16, 16), new[] { SocketDirection.South, SocketDirection.North });
            GameObject boss = MakeRoom(RoomType.Boss, new Vector2(16, 20), new[] { SocketDirection.South });

            DungeonDefinition def = ScriptableObject.CreateInstance<DungeonDefinition>();
            def.EditorConfigure("runtime_test", 3, 4, 0, "Dungeon", entrance, boss,
                new System.Collections.Generic.List<GameObject> { combat });

            DungeonLayoutResult layout = DungeonGenerator.Generate(def, 5);
            Assert.IsTrue(layout.Success, layout.FailureReason);

            GameObject root = new GameObject("TestDungeonRoot");
            DungeonBuildResult build = DungeonSceneBuilder.Build(layout, root.transform);
            yield return null;

            Assert.IsNotNull(build.Root);
            Assert.AreEqual(layout.Rooms.Count, build.Instances.Count);

            // Every placed room must have produced at least one collider so the player
            // cannot fall through floors or walk through walls.
            foreach (var kvp in build.Instances)
            {
                Collider[] colliders = kvp.Value.GetComponentsInChildren<Collider>();
                Assert.Greater(colliders.Length, 0, $"Room {kvp.Key.Definition.RoomType} produced no colliders.");
            }

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Boss_IsReachable_ThroughConnectionGraph()
        {
            GameObject entrance = MakeRoom(RoomType.Entrance, new Vector2(12, 12), new[] { SocketDirection.North });
            GameObject combat = MakeRoom(RoomType.Combat, new Vector2(16, 16), new[] { SocketDirection.South, SocketDirection.North });
            GameObject boss = MakeRoom(RoomType.Boss, new Vector2(16, 20), new[] { SocketDirection.South });

            DungeonDefinition def = ScriptableObject.CreateInstance<DungeonDefinition>();
            def.EditorConfigure("runtime_test2", 4, 6, 0, "Dungeon", entrance, boss,
                new System.Collections.Generic.List<GameObject> { combat });

            DungeonLayoutResult layout = DungeonGenerator.Generate(def, 9);
            yield return null;

            Assert.IsTrue(layout.Success, layout.FailureReason);
            Assert.IsTrue(DungeonGenerator.IsBossReachable(layout));
        }
    }
}
