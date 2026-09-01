using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using RestosDaMasmorra.Dungeon;

namespace RestosDaMasmorra.Tests.EditMode
{
    public class DungeonGeneratorTests
    {
        readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned) Object.DestroyImmediate(go);
            spawned.Clear();
        }

        GameObject MakeRoom(RoomType type, Vector2 size, SocketDirection[] sockets, float weight = 1f, int minDepth = 0, int maxDepth = 99, bool canRepeat = true)
        {
            GameObject go = new GameObject("Room_" + type + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 6));
            spawned.Add(go);

            RoomDefinition def = go.AddComponent<RoomDefinition>();
            SerializedObject so = new SerializedObject(def);
            so.FindProperty("roomType").enumValueIndex = (int)type;
            so.FindProperty("weight").floatValue = weight;
            so.FindProperty("minDepth").intValue = minDepth;
            so.FindProperty("maxDepth").intValue = maxDepth;
            so.FindProperty("canRepeat").boolValue = canRepeat;
            so.FindProperty("size").vector2Value = size;
            so.ApplyModifiedPropertiesWithoutUndo();

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
                SerializedObject socketSo = new SerializedObject(socket);
                socketSo.FindProperty("direction").enumValueIndex = (int)dir;
                socketSo.ApplyModifiedPropertiesWithoutUndo();
            }

            return go;
        }

        DungeonDefinition MakeStandardDefinition(int minRooms = 6, int maxRooms = 10, int maxBranches = 2)
        {
            GameObject entrance = MakeRoom(RoomType.Entrance, new Vector2(12, 12), new[] { SocketDirection.North });
            GameObject combatStraight = MakeRoom(RoomType.Combat, new Vector2(16, 16), new[] { SocketDirection.South, SocketDirection.North }, weight: 1f);
            GameObject combatBranch = MakeRoom(RoomType.Combat, new Vector2(16, 16), new[] { SocketDirection.South, SocketDirection.North, SocketDirection.East }, weight: 0.7f);
            GameObject corridor = MakeRoom(RoomType.Corridor, new Vector2(4, 12), new[] { SocketDirection.South, SocketDirection.North }, weight: 1.2f);
            GameObject resource = MakeRoom(RoomType.Resource, new Vector2(12, 12), new[] { SocketDirection.South }, weight: 0.7f, minDepth: 1);
            GameObject deadEnd = MakeRoom(RoomType.DeadEnd, new Vector2(12, 12), new[] { SocketDirection.South }, weight: 0.6f);
            GameObject boss = MakeRoom(RoomType.Boss, new Vector2(16, 20), new[] { SocketDirection.South }, canRepeat: false);

            DungeonDefinition def = ScriptableObject.CreateInstance<DungeonDefinition>();
            def.EditorConfigure("test_dungeon", minRooms, maxRooms, maxBranches, "Dungeon", entrance, boss,
                new List<GameObject> { combatStraight, combatBranch, corridor, resource, deadEnd });
            return def;
        }

        [Test]
        public void SocketDirection_NorthOpposite_IsSouth()
        {
            Assert.AreEqual(SocketDirection.South, SocketDirection.North.Opposite());
            Assert.AreEqual(SocketDirection.North, SocketDirection.South.Opposite());
        }

        [Test]
        public void SocketDirection_EastOpposite_IsWest()
        {
            Assert.AreEqual(SocketDirection.West, SocketDirection.East.Opposite());
            Assert.AreEqual(SocketDirection.East, SocketDirection.West.Opposite());
        }

        [Test]
        public void Generate_SameSeed_ProducesSameLayout()
        {
            DungeonDefinition def = MakeStandardDefinition();

            DungeonLayoutResult a = DungeonGenerator.Generate(def, 777);
            DungeonLayoutResult b = DungeonGenerator.Generate(def, 777);

            Assert.IsTrue(a.Success);
            Assert.IsTrue(b.Success);
            Assert.AreEqual(a.Rooms.Count, b.Rooms.Count);
            for (int i = 0; i < a.Rooms.Count; i++)
            {
                Assert.AreEqual(a.Rooms[i].Position, b.Rooms[i].Position);
                Assert.AreEqual(a.Rooms[i].YawDegrees, b.Rooms[i].YawDegrees);
                Assert.AreEqual(a.Rooms[i].Definition.RoomType, b.Rooms[i].Definition.RoomType);
            }
        }

        [Test]
        public void Generate_DifferentSeeds_CanProduceDifferentLayouts()
        {
            DungeonDefinition def = MakeStandardDefinition();

            var roomCounts = new HashSet<int>();
            for (int seed = 1; seed <= 8; seed++)
            {
                DungeonLayoutResult r = DungeonGenerator.Generate(def, seed);
                Assert.IsTrue(r.Success);
                roomCounts.Add(r.Rooms.Count);
            }

            Assert.Greater(roomCounts.Count, 1, "Expected some variation in room counts across different seeds.");
        }

        [Test]
        public void Generate_EntranceAlwaysExists()
        {
            DungeonDefinition def = MakeStandardDefinition();
            DungeonLayoutResult r = DungeonGenerator.Generate(def, 42);

            Assert.IsTrue(r.Success);
            Assert.IsNotNull(r.Entrance);
            Assert.AreEqual(RoomType.Entrance, r.Entrance.Definition.RoomType);
        }

        [Test]
        public void Generate_BossAlwaysExists()
        {
            DungeonDefinition def = MakeStandardDefinition();
            DungeonLayoutResult r = DungeonGenerator.Generate(def, 42);

            Assert.IsTrue(r.Success);
            Assert.IsNotNull(r.Boss);
            Assert.AreEqual(RoomType.Boss, r.Boss.Definition.RoomType);
        }

        [Test]
        public void Generate_BossIsReachable()
        {
            DungeonDefinition def = MakeStandardDefinition();
            DungeonLayoutResult r = DungeonGenerator.Generate(def, 123);

            Assert.IsTrue(r.Success);
            Assert.IsTrue(DungeonGenerator.IsBossReachable(r));
        }

        [Test]
        public void Generate_RespectsMinMaxRooms()
        {
            DungeonDefinition def = MakeStandardDefinition(minRooms: 5, maxRooms: 7);

            for (int seed = 1; seed <= 10; seed++)
            {
                DungeonLayoutResult r = DungeonGenerator.Generate(def, seed);
                Assert.IsTrue(r.Success, $"seed {seed} failed: {r.FailureReason}");
                Assert.GreaterOrEqual(r.Rooms.Count, 5);
                Assert.LessOrEqual(r.Rooms.Count, 7 + def.MaxBranches); // branches can add beyond the main-path target
            }
        }

        [Test]
        public void Generate_RespectsCanRepeatFalse()
        {
            DungeonDefinition def = MakeStandardDefinition();
            DungeonLayoutResult r = DungeonGenerator.Generate(def, 99);

            Assert.IsTrue(r.Success);
            int bossCount = r.Rooms.Count(x => x.Definition.RoomType == RoomType.Boss);
            Assert.AreEqual(1, bossCount, "Boss room (CanRepeat=false) must appear at most once.");
        }

        [Test]
        public void Generate_RespectsMinMaxDepth()
        {
            DungeonDefinition def = MakeStandardDefinition();
            DungeonLayoutResult r = DungeonGenerator.Generate(def, 55);

            Assert.IsTrue(r.Success);
            foreach (PlacedRoom room in r.Rooms)
            {
                Assert.GreaterOrEqual(room.Depth, room.Definition.MinDepth);
                Assert.LessOrEqual(room.Depth, room.Definition.MaxDepth);
            }
        }

        [Test]
        public void Generate_NoRoomsOverlap()
        {
            DungeonDefinition def = MakeStandardDefinition();
            DungeonLayoutResult r = DungeonGenerator.Generate(def, 2024);

            Assert.IsTrue(r.Success);
            Assert.IsFalse(DungeonGenerator.HasAnyOverlap(r.Rooms, out string reason), reason);
        }

        [Test]
        public void Generate_BranchesDoNotExceedMaxBranches()
        {
            DungeonDefinition def = MakeStandardDefinition(maxBranches: 2);

            for (int seed = 1; seed <= 10; seed++)
            {
                DungeonLayoutResult r = DungeonGenerator.Generate(def, seed);
                Assert.IsTrue(r.Success);
                Assert.LessOrEqual(r.BranchCount, 2);
            }
        }

        [Test]
        public void Generate_FailsGracefully_WhenPoolIsEmpty()
        {
            GameObject entrance = MakeRoom(RoomType.Entrance, new Vector2(12, 12), new[] { SocketDirection.North });
            GameObject boss = MakeRoom(RoomType.Boss, new Vector2(16, 20), new[] { SocketDirection.South });

            DungeonDefinition def = ScriptableObject.CreateInstance<DungeonDefinition>();
            def.EditorConfigure("empty_pool", 6, 10, 2, "Dungeon", entrance, boss, new List<GameObject>());

            DungeonLayoutResult r = DungeonGenerator.Generate(def, 1);

            Assert.IsFalse(r.Success);
            Assert.IsNotEmpty(r.FailureReason);
        }

        [Test]
        public void Generate_FailsGracefully_WhenEntranceHasNoSockets()
        {
            GameObject entrance = MakeRoom(RoomType.Entrance, new Vector2(12, 12), new SocketDirection[0]);
            GameObject boss = MakeRoom(RoomType.Boss, new Vector2(16, 20), new[] { SocketDirection.South });
            GameObject combat = MakeRoom(RoomType.Combat, new Vector2(16, 16), new[] { SocketDirection.South, SocketDirection.North });

            DungeonDefinition def = ScriptableObject.CreateInstance<DungeonDefinition>();
            def.EditorConfigure("bad_entrance", 3, 5, 1, "Dungeon", entrance, boss, new List<GameObject> { combat });

            DungeonLayoutResult r = DungeonGenerator.Generate(def, 1);

            Assert.IsFalse(r.Success);
            Assert.IsNotEmpty(r.FailureReason);
        }

        [Test]
        public void MainPath_StartsAtEntrance_EndsAtBoss_BranchesAreExcluded()
        {
            DungeonDefinition def = MakeStandardDefinition();
            DungeonLayoutResult r = DungeonGenerator.Generate(def, 321);

            Assert.IsTrue(r.Success);
            Assert.AreEqual(r.Entrance, r.MainPath[0]);
            Assert.AreEqual(r.Boss, r.MainPath[r.MainPath.Count - 1]);

            foreach (PlacedRoom room in r.MainPath) Assert.IsTrue(room.IsMainPath);

            int branchRoomCount = r.Rooms.Count(x => !x.IsMainPath);
            Assert.AreEqual(r.BranchCount, branchRoomCount);

            // No branch room should ever appear in the main path sequence.
            foreach (PlacedRoom room in r.Rooms.Where(x => !x.IsMainPath))
            {
                Assert.IsFalse(r.MainPath.Contains(room));
            }
        }

        [Test]
        public void MainPath_IsContiguousChainOfConnections()
        {
            DungeonDefinition def = MakeStandardDefinition();
            DungeonLayoutResult r = DungeonGenerator.Generate(def, 654);

            Assert.IsTrue(r.Success);
            for (int i = 0; i < r.MainPath.Count - 1; i++)
            {
                PlacedRoom a = r.MainPath[i];
                PlacedRoom b = r.MainPath[i + 1];
                bool connected = a.Connections.Any(c => c.other == b);
                Assert.IsTrue(connected, $"Main path room {i} is not directly connected to room {i + 1}.");
            }
        }
    }
}
