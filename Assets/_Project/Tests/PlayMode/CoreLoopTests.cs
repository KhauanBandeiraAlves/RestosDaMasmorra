using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using RestosDaMasmorra.Characters;
using RestosDaMasmorra.Characters.Combat;
using RestosDaMasmorra.Core;
using RestosDaMasmorra.Dungeon;
using RestosDaMasmorra.Economy;
using RestosDaMasmorra.Enemies;
using RestosDaMasmorra.Items;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.Tests.PlayMode
{
    static class CoreLoopTestUtil
    {
        public static GameObject CreateFlatGroundWithNavMesh(float size = 40f)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);
            NavMeshSurface surface = ground.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();
            return ground;
        }

        public static GameObject CreatePlayer()
        {
            GameObject go = new GameObject("TestPlayer");
            go.tag = "Player";
            go.AddComponent<CharacterController>();
            go.AddComponent<PlayerStamina>();
            go.AddComponent<PlayerMovement>();
            go.AddComponent<PlayerInteraction>();
            go.AddComponent<PlayerInventory>();
            go.AddComponent<PlayerSuspicion>();
            Health health = go.AddComponent<Health>();
            health.SetMaxHealth(20f);
            go.AddComponent<PlayerCombatant>();
            go.AddComponent<PlayerLifeController>();
            return go;
        }

        public static GameObject EnsureGameSession()
        {
            if (GameSession.Instance != null) return GameSession.Instance.gameObject;
            GameObject go = new GameObject("GameSession", typeof(GameSession));
            go.GetComponent<GameSession>().Initialize();
            return go;
        }
    }

    public class ExtractionTests
    {
        GameObject session;
        GameObject player;

        [SetUp]
        public void SetUp()
        {
            session = CoreLoopTestUtil.EnsureGameSession();
            // Extract() would otherwise trigger a real SceneManager.LoadScene, which loads
            // PrototypeBase/PrototypeDungeon (the latter spawns a procedural dungeon + bakes
            // NavMesh on Start) from inside a running PlayMode test — this previously hung
            // the Editor. Tests only need the transfer logic, not the scene transition.
            SceneLoadGate.SuppressForTests = true;
        }

        [TearDown]
        public void TearDown()
        {
            SceneLoadGate.SuppressForTests = false;
            if (player != null) Object.Destroy(player);
            CombatantRegistry.ClearAll();
        }

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator ExtractionPoint_TransfersInventory_ToSharedStorage()
        {
            player = CoreLoopTestUtil.CreatePlayer();
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            inventory.TryAddItem(item);
            inventory.TryAddItem(item);
            yield return null;

            int before = GameSession.Instance.Storage.TotalCount();

            GameObject extractionGO = new GameObject("Extraction");
            ExtractionPoint extraction = extractionGO.AddComponent<ExtractionPoint>();

            // First press arms confirmation, does not transfer yet.
            extraction.Interact(player);
            yield return null;
            Assert.AreEqual(2, inventory.Items.Count, "First interaction should only arm the confirmation, not extract yet.");

            extraction.Extract(player); // simulate confirmed extraction without depending on a real scene load
            yield return null;

            Assert.AreEqual(0, inventory.Items.Count);
            Assert.AreEqual(before + 2, GameSession.Instance.Storage.TotalCount());

            Object.Destroy(extractionGO);
        }
    }

    public class DefeatFlowTests
    {
        GameObject session;
        GameObject player;

        [SetUp]
        public void SetUp()
        {
            session = CoreLoopTestUtil.EnsureGameSession();
            SceneLoadGate.SuppressForTests = true; // see ExtractionTests.SetUp for why
        }

        [TearDown]
        public void TearDown()
        {
            SceneLoadGate.SuppressForTests = false;
            if (player != null) Object.Destroy(player);
            CombatantRegistry.ClearAll();
        }

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator PlayerLifeState_TransitionsToReturned_OnDeath()
        {
            player = CoreLoopTestUtil.CreatePlayer();
            Health health = player.GetComponent<Health>();
            PlayerLifeController life = player.GetComponent<PlayerLifeController>();
            yield return null;

            Assert.AreEqual(PlayerLifeState.Alive, life.State);

            health.TakeDamage(9999f);
            yield return null;

            Assert.AreEqual(PlayerLifeState.Returned, life.State);
        }

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator SoloDefeat_LosesOnlyPartOfRunInventory_PreservesOldStorage()
        {
            player = CoreLoopTestUtil.CreatePlayer();
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            for (int i = 0; i < 10; i++) inventory.TryAddItem(item);

            ItemDefinition oldStuff = ScriptableObject.CreateInstance<ItemDefinition>();
            GameSession.Instance.Storage.AddStack(oldStuff, 20);

            Health health = player.GetComponent<Health>();
            yield return null;

            health.TakeDamage(9999f);
            yield return null;

            Assert.AreEqual(20, GameSession.Instance.Storage.GetStackCount(oldStuff), "Defeat must never touch storage from earlier runs.");
            Assert.AreEqual(0, inventory.Items.Count, "Run inventory should be empty after defeat resolves (lost + preserved-to-storage).");
        }
    }

    public class PartyWipeTests
    {
        readonly List<GameObject> spawned = new List<GameObject>();
        float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = 25f;
            // The overpowered test enemy can also kill the player (registered as
            // Team.Party via PlayerCombatant) after the party dies, which would otherwise
            // trigger a real SceneManager.LoadScene mid-test — see ExtractionTests.SetUp.
            SceneLoadGate.SuppressForTests = true;
        }

        [TearDown]
        public void TearDown()
        {
            SceneLoadGate.SuppressForTests = false;
            Time.timeScale = originalTimeScale;
            foreach (GameObject go in spawned) if (go != null) Object.Destroy(go);
            spawned.Clear();
            CombatantRegistry.ClearAll();
        }

        [UnityTest]
        [Timeout(20000)]
        public IEnumerator PlayerCanStillActAndCollect_AfterEntirePartyDies()
        {
            GameObject ground = CoreLoopTestUtil.CreateFlatGroundWithNavMesh();
            spawned.Add(ground);

            AdventurerDefinition weakKnight = ScriptableObject.CreateInstance<AdventurerDefinition>();
            weakKnight.EditorConfigure(AdventurerType.Knight, "Knight", 1, 3f, 1, 1.5f, 2f, false, null);

            EnemyDefinition strongSkeleton = ScriptableObject.CreateInstance<EnemyDefinition>();
            strongSkeleton.EditorConfigure("Skeleton", 100, 3f, 999, 1.5f, 0.2f, null, null);

            var party = PartySpawner.Spawn(new List<AdventurerDefinition> { weakKnight }, Vector3.zero, new List<Vector3> { Vector3.zero }, null, 1, null);
            spawned.Add(party[0].gameObject);
            EnemyController enemy = EnemySpawner.Spawn(strongSkeleton, new Vector3(1f, 0f, 0f), 2, null);
            spawned.Add(enemy.gameObject);

            for (int i = 0; i < 400 && party[0].IsAlive; i++) yield return null;
            Assert.IsFalse(party[0].IsAlive, "Setup assumption failed: adventurer should have died to the overpowered enemy.");

            // Player must remain fully functional: can move, and can still pick up an item.
            GameObject player = CoreLoopTestUtil.CreatePlayer();
            spawned.Add(player);
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            Vector3 startPos = player.transform.position;
            for (int i = 0; i < 30; i++) { movement.Tick(new Vector2(0f, 1f), false, 0.05f); yield return null; }
            Assert.Greater(Vector3.Distance(startPos, player.transform.position), 0.1f, "Player should still be able to move after the party wipes.");

            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            GameObject worldItemGO = new GameObject("Loot");
            spawned.Add(worldItemGO);
            worldItemGO.AddComponent<SphereCollider>();
            WorldItem worldItem = worldItemGO.AddComponent<WorldItem>();
            worldItem.EditorConfigure(item, ItemOwnership.Discarded);
            worldItem.Interact(player);

            Assert.AreEqual(1, player.GetComponent<PlayerInventory>().Items.Count, "Player should still be able to collect items after the party wipes.");
        }
    }

    public class CoreLoopEndToEndTest
    {
        readonly List<GameObject> spawned = new List<GameObject>();
        float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = 25f;
            if (GameSession.Instance == null)
            {
                GameObject sessionGO = new GameObject("GameSession", typeof(GameSession));
                sessionGO.GetComponent<GameSession>().Initialize();
                spawned.Add(sessionGO);
            }
            SceneLoadGate.SuppressForTests = true; // see ExtractionTests.SetUp for why
        }

        [TearDown]
        public void TearDown()
        {
            SceneLoadGate.SuppressForTests = false;
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

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator FullCoreLoop_BaseToDungeonToExtractionToDismantle()
        {
            // --- Base: storage starts empty, dismantling bench with one recipe ---
            ItemDefinition brokenSword = ScriptableObject.CreateInstance<ItemDefinition>();
            ItemDefinition metal = ScriptableObject.CreateInstance<ItemDefinition>();

            DismantlingRecipe recipe = ScriptableObject.CreateInstance<DismantlingRecipe>();
            recipe.EditorConfigure(brokenSword, 1, new List<DismantlingOutput> { new DismantlingOutput { item = metal, quantity = 3 } });

            // --- Dungeon: generate, build, bake nav, spawn party + enemies ---
            GameObject entrance = MakeRoom(RoomType.Entrance, new Vector2(12, 12), new[] { SocketDirection.North });
            GameObject combat = MakeRoom(RoomType.Combat, new Vector2(16, 16), new[] { SocketDirection.South, SocketDirection.North }, true, 2, 3);
            GameObject boss = MakeRoom(RoomType.Boss, new Vector2(16, 20), new[] { SocketDirection.South });

            DungeonDefinition def = ScriptableObject.CreateInstance<DungeonDefinition>();
            def.EditorConfigure("e2e", 3, 4, 0, "Dungeon", entrance, boss, new List<GameObject> { combat });

            DungeonLayoutResult layout = DungeonGenerator.Generate(def, 42);
            Assert.IsTrue(layout.Success, layout.FailureReason);

            GameObject dungeonRoot = new GameObject("DungeonRoot");
            spawned.Add(dungeonRoot);
            DungeonBuildResult build = DungeonSceneBuilder.Build(layout, dungeonRoot.transform);

            NavMeshSurface surface = dungeonRoot.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();

            GameObject player = CoreLoopTestUtil.CreatePlayer();
            spawned.Add(player);
            player.transform.position = build.EntranceWorldPosition + new Vector3(0f, 0.1f, -1f);

            AdventurerDefinition knight = ScriptableObject.CreateInstance<AdventurerDefinition>();
            knight.EditorConfigure(AdventurerType.Knight, "Knight", 30, 3.5f, 8, 1.6f, 0.6f, false, null);
            EnemyDefinition skeleton = ScriptableObject.CreateInstance<EnemyDefinition>();
            skeleton.EditorConfigure("Skeleton", 5, 2.5f, 1, 1.4f, 1.5f, null, null);

            List<Vector3> route = new List<Vector3>();
            foreach (PlacedRoom room in layout.MainPath) route.Add(room.Position);

            var party = PartySpawner.Spawn(new List<AdventurerDefinition> { knight }, build.EntranceWorldPosition, route, null, 7, dungeonRoot.transform);
            var enemies = EnemySpawner.SpawnForLayout(layout, skeleton, 8, dungeonRoot.transform);
            spawned.AddRange(System.Linq.Enumerable.Select(party, p => p.gameObject));
            foreach (var e in enemies) spawned.Add(e.gameObject);

            Assert.Greater(enemies.Count, 0);

            // Let combat happen — the enemy should end up dead and drop nothing here (no
            // loot table configured), but the encounter itself must resolve cleanly.
            for (int i = 0; i < 500; i++) yield return null;

            // Simulate the player picking up a world item (stand-in for "collected loot").
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            GameObject worldItemGO = new GameObject("Loot");
            spawned.Add(worldItemGO);
            worldItemGO.AddComponent<SphereCollider>();
            WorldItem worldItem = worldItemGO.AddComponent<WorldItem>();
            worldItem.EditorConfigure(brokenSword, ItemOwnership.Discarded);
            worldItem.Interact(player);
            Assert.AreEqual(1, inventory.Items.Count);

            // --- Return to Entrance and extract (transfer, not scene-load, to stay a pure
            // logic test rather than depending on actual scene assets in Build Settings) ---
            player.transform.position = build.EntranceWorldPosition;
            GameObject extractionGO = new GameObject("Extraction");
            spawned.Add(extractionGO);
            ExtractionPoint extraction = extractionGO.AddComponent<ExtractionPoint>();
            extraction.Extract(player);

            Assert.AreEqual(0, inventory.Items.Count, "Inventory should be empty after extraction.");
            Assert.AreEqual(1, GameSession.Instance.Storage.GetStackCount(brokenSword), "Storage should now have the extracted item.");

            // --- Base: dismantle the extracted item ---
            bool dismantled = DismantlingService.TryDismantle(GameSession.Instance.Storage, recipe);

            Assert.IsTrue(dismantled);
            Assert.AreEqual(0, GameSession.Instance.Storage.GetStackCount(brokenSword));
            Assert.AreEqual(3, GameSession.Instance.Storage.GetStackCount(metal));
        }
    }
}
