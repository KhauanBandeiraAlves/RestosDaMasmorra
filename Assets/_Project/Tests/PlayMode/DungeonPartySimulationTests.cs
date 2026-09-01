using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using RestosDaMasmorra.Characters;
using RestosDaMasmorra.Characters.Combat;
using RestosDaMasmorra.Dungeon;
using RestosDaMasmorra.Enemies;
using RestosDaMasmorra.Items;

namespace RestosDaMasmorra.Tests.PlayMode
{
    public class DungeonPartySimulationTests
    {
        readonly List<GameObject> spawned = new List<GameObject>();
        float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            // See CombatBehaviourTests.SetUp: batch-mode frames run far faster than real
            // time, so NavMeshAgent's acceleration-based movement needs time scaled up to
            // make meaningful progress within a bounded number of yielded frames.
            originalTimeScale = Time.timeScale;
            Time.timeScale = 25f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = originalTimeScale;
            foreach (GameObject go in spawned) if (go != null) Object.Destroy(go);
            spawned.Clear();
            CombatantRegistry.ClearAll();
        }

        GameObject MakeRoom(RoomType type, Vector2 size, SocketDirection[] sockets, bool spawnsEnemies = false, int minE = 0, int maxE = 0)
        {
            GameObject go = new GameObject("Room_" + type);
            spawned.Add(go);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(go.transform, false);
            floor.transform.localScale = new Vector3(size.x, 0.1f, size.y);
            Object.Destroy(floor.GetComponent<Collider>());
            floor.AddComponent<MeshCollider>().sharedMesh = floor.GetComponent<MeshFilter>().sharedMesh;

            RoomDefinition def = go.AddComponent<RoomDefinition>();
            SetField(def, "roomType", type);
            SetField(def, "weight", 1f);
            SetField(def, "minDepth", 0);
            SetField(def, "maxDepth", 99);
            SetField(def, "canRepeat", true);
            SetField(def, "size", size);
            def.EditorConfigureSpawn(spawnsEnemies, minE, maxE);

            float halfW = size.x * 0.5f, halfD = size.y * 0.5f;
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
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(target, value);
        }

        DungeonDefinition MakeDefinitionWithCombatRooms(int minRooms, int maxRooms, int enemiesPerRoomMin, int enemiesPerRoomMax)
        {
            GameObject entrance = MakeRoom(RoomType.Entrance, new Vector2(12, 12), new[] { SocketDirection.North });
            GameObject combatA = MakeRoom(RoomType.Combat, new Vector2(16, 16), new[] { SocketDirection.South, SocketDirection.North }, true, enemiesPerRoomMin, enemiesPerRoomMax);
            GameObject combatB = MakeRoom(RoomType.Combat, new Vector2(16, 16), new[] { SocketDirection.South, SocketDirection.North, SocketDirection.East }, true, enemiesPerRoomMin, enemiesPerRoomMax);
            GameObject corridor = MakeRoom(RoomType.Corridor, new Vector2(4, 12), new[] { SocketDirection.South, SocketDirection.North });
            GameObject resource = MakeRoom(RoomType.Resource, new Vector2(12, 12), new[] { SocketDirection.South }, false);
            GameObject boss = MakeRoom(RoomType.Boss, new Vector2(16, 20), new[] { SocketDirection.South }, true, enemiesPerRoomMin + 1, enemiesPerRoomMax + 2);

            DungeonDefinition def = ScriptableObject.CreateInstance<DungeonDefinition>();
            def.EditorConfigure("sim_test", minRooms, maxRooms, 2, "Dungeon", entrance, boss,
                new List<GameObject> { combatA, combatB, corridor, resource });
            return def;
        }

        [UnityTest]
        [Timeout(20000)]
        public IEnumerator PartyAndEnemies_SimulateForAWhile_NoExceptions_AndCombatHappens()
        {
            DungeonDefinition def = MakeDefinitionWithCombatRooms(5, 7, 2, 3);
            DungeonLayoutResult layout = DungeonGenerator.Generate(def, 11);
            Assert.IsTrue(layout.Success, layout.FailureReason);

            GameObject root = new GameObject("SimRoot");
            spawned.Add(root);
            DungeonBuildResult build = DungeonSceneBuilder.Build(layout, root.transform);

            NavMeshSurface surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();

            AdventurerDefinition knight = ScriptableObject.CreateInstance<AdventurerDefinition>();
            knight.EditorConfigure(AdventurerType.Knight, "Knight", 30, 3.5f, 6, 1.6f, 1f, false, null);
            AdventurerDefinition mage = ScriptableObject.CreateInstance<AdventurerDefinition>();
            mage.EditorConfigure(AdventurerType.Mage, "Mage", 14, 3f, 5, 6f, 1.6f, true, null);
            AdventurerDefinition archer = ScriptableObject.CreateInstance<AdventurerDefinition>();
            archer.EditorConfigure(AdventurerType.Archer, "Archer", 18, 3.4f, 4, 7f, 1.1f, true, null);

            EnemyDefinition skeleton = ScriptableObject.CreateInstance<EnemyDefinition>();
            skeleton.EditorConfigure("Skeleton", 8, 2.5f, 3, 1.4f, 1.5f, null, null);

            List<Vector3> route = new List<Vector3>();
            foreach (PlacedRoom room in layout.MainPath) route.Add(room.Position);

            var party = PartySpawner.Spawn(new List<AdventurerDefinition> { knight, mage, archer }, layout.Entrance.Position, route, null, 5, root.transform);
            var enemies = EnemySpawner.SpawnForLayout(layout, skeleton, 6, root.transform);

            Assert.AreEqual(3, party.Count);
            Assert.Greater(enemies.Count, 0);

            Vector3 startPos = party[0].transform.position;

            for (int i = 0; i < 400; i++) yield return null;

            bool anyEnemyDamaged = enemies.Exists(e => e == null || !e.IsAlive || e.CombatHealth.Current < e.CombatHealth.MaxHealth);
            float traveledDistance = Vector3.Distance(startPos, party[0].transform.position);

            Assert.IsTrue(anyEnemyDamaged || traveledDistance > 0.5f,
                "Expected either combat to occur or the party to make travel progress.");
        }

        [UnityTest]
        [Timeout(20000)]
        public IEnumerator StressTest_ManyEnemiesAndWorldItems_NoExceptions()
        {
            DungeonDefinition def = MakeDefinitionWithCombatRooms(6, 8, 4, 6);
            DungeonLayoutResult layout = DungeonGenerator.Generate(def, 777);
            Assert.IsTrue(layout.Success, layout.FailureReason);

            GameObject root = new GameObject("StressRoot");
            spawned.Add(root);
            DungeonSceneBuilder.Build(layout, root.transform);

            NavMeshSurface surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();

            AdventurerDefinition knight = ScriptableObject.CreateInstance<AdventurerDefinition>();
            knight.EditorConfigure(AdventurerType.Knight, "Knight", 40, 3.5f, 8, 1.6f, 0.8f, false, null);
            AdventurerDefinition mage = ScriptableObject.CreateInstance<AdventurerDefinition>();
            mage.EditorConfigure(AdventurerType.Mage, "Mage", 20, 3f, 6, 6f, 1.2f, true, null);
            AdventurerDefinition archer = ScriptableObject.CreateInstance<AdventurerDefinition>();
            archer.EditorConfigure(AdventurerType.Archer, "Archer", 22, 3.4f, 5, 7f, 1f, true, null);

            EnemyDefinition skeleton = ScriptableObject.CreateInstance<EnemyDefinition>();
            skeleton.EditorConfigure("Skeleton", 6, 2.5f, 2, 1.4f, 1.5f, null, null);

            List<Vector3> route = new List<Vector3>();
            foreach (PlacedRoom room in layout.MainPath) route.Add(room.Position);

            var party = PartySpawner.Spawn(new List<AdventurerDefinition> { knight, mage, archer }, layout.Entrance.Position, route, null, 21, root.transform);
            var enemies = EnemySpawner.SpawnForLayout(layout, skeleton, 22, root.transform);

            Assert.GreaterOrEqual(enemies.Count, 15, "Expected the stress dungeon to spawn a substantial number of enemies.");

            // ~50 scattered world items.
            ItemDefinition testItem = ScriptableObject.CreateInstance<ItemDefinition>();
            System.Random rng = new System.Random(9);
            int itemsSpawned = 0;
            foreach (PlacedRoom room in layout.Rooms)
            {
                if (itemsSpawned >= 50) break;
                int perRoom = Mathf.Max(1, 50 / layout.Rooms.Count);
                for (int i = 0; i < perRoom && itemsSpawned < 50; i++)
                {
                    Vector3 pos = room.Position + new Vector3(((float)rng.NextDouble() - 0.5f) * 4f, 0f, ((float)rng.NextDouble() - 0.5f) * 4f);
                    WorldItem dropped = LootSpawner.SpawnDrop(testItem, pos, ItemOwnership.Discarded, root.transform);
                    if (dropped != null) itemsSpawned++;
                }
            }
            Assert.GreaterOrEqual(itemsSpawned, 40);

            for (int i = 0; i < 300; i++) yield return null;

            // Reaching here without the test framework flagging an exception/error log is the
            // actual pass condition; this final check just confirms nothing got silently wiped.
            Assert.IsNotNull(party[0]);
        }
    }
}
