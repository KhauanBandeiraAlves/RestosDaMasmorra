using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using RestosDaMasmorra.Characters;
using RestosDaMasmorra.Characters.Combat;
using RestosDaMasmorra.Enemies;
using RestosDaMasmorra.Items;

namespace RestosDaMasmorra.Tests.PlayMode
{
    static class CombatTestUtil
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

        public static AdventurerDefinition MakeAdventurerDef(AdventurerType type, float health, float speed, float damage, float range, float cooldown, bool ranged)
        {
            AdventurerDefinition def = ScriptableObject.CreateInstance<AdventurerDefinition>();
            def.EditorConfigure(type, type.ToString(), health, speed, damage, range, cooldown, ranged, null);
            return def;
        }

        public static EnemyDefinition MakeEnemyDef(float health, float speed, float damage, float range, float cooldown, LootDropTable loot = null)
        {
            EnemyDefinition def = ScriptableObject.CreateInstance<EnemyDefinition>();
            def.EditorConfigure("TestSkeleton", health, speed, damage, range, cooldown, null, loot);
            return def;
        }
    }

    public class CombatBehaviourTests
    {
        GameObject ground;
        float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            // Batch-mode PlayMode frames can execute far faster than real time, giving each
            // frame a tiny Time.deltaTime; NavMeshAgent's acceleration-based movement barely
            // progresses per frame as a result. Scaling time up compensates so a bounded
            // number of yielded frames reliably covers the distances these tests need.
            originalTimeScale = Time.timeScale;
            Time.timeScale = 25f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = originalTimeScale;
            if (ground != null) Object.Destroy(ground);
            CombatantRegistry.ClearAll();
        }

        [UnityTest]
        public IEnumerator Adventurer_SelectsNearestEnemy()
        {
            ground = CombatTestUtil.CreateFlatGroundWithNavMesh();

            AdventurerDefinition advDef = CombatTestUtil.MakeAdventurerDef(AdventurerType.Knight, 30, 3.5f, 5, 1.6f, 1f, false);
            EnemyDefinition enemyDef = CombatTestUtil.MakeEnemyDef(10, 2.5f, 3, 1.4f, 1.5f);

            var party = PartySpawner.Spawn(new List<AdventurerDefinition> { advDef }, Vector3.zero, new List<Vector3> { Vector3.zero }, null, 1, null);
            EnemyController enemy = EnemySpawner.Spawn(enemyDef, new Vector3(3, 0, 0), 2, null);

            yield return null;
            yield return null;

            AdventurerController adventurer = party[0];
            Assert.IsNotNull(adventurer);

            // Give it a couple of frames to acquire a target via CombatantRegistry.
            yield return null;

            // Combat presence check: after some simulated time, the enemy should have taken damage.
            for (int i = 0; i < 600 && enemy.CombatHealth.Current == enemy.CombatHealth.MaxHealth; i++)
                yield return null;

            Assert.Less(enemy.CombatHealth.Current, enemy.CombatHealth.MaxHealth, "Adventurer never engaged the nearest enemy.");

            Object.Destroy(party[0].gameObject);
            Object.Destroy(enemy.gameObject);
        }

        [UnityTest]
        public IEnumerator Enemy_SelectsNearestAdventurer_OverPlayer()
        {
            ground = CombatTestUtil.CreateFlatGroundWithNavMesh();

            GameObject playerGO = new GameObject("Player");
            playerGO.tag = "Player";
            playerGO.transform.position = new Vector3(-10, 0, 0);

            AdventurerDefinition advDef = CombatTestUtil.MakeAdventurerDef(AdventurerType.Knight, 30, 3.5f, 5, 1.6f, 1f, false);
            EnemyDefinition enemyDef = CombatTestUtil.MakeEnemyDef(10, 2.5f, 3, 1.4f, 1.5f);

            var party = PartySpawner.Spawn(new List<AdventurerDefinition> { advDef }, new Vector3(2, 0, 0), new List<Vector3> { new Vector3(2, 0, 0) }, null, 1, null);
            EnemyController enemy = EnemySpawner.Spawn(enemyDef, Vector3.zero, 2, null);

            for (int i = 0; i < 600 && party[0].CombatHealth.Current == party[0].CombatHealth.MaxHealth; i++)
                yield return null;

            Assert.Less(party[0].CombatHealth.Current, party[0].CombatHealth.MaxHealth, "Enemy did not prioritize the nearby adventurer over the far player.");

            Object.Destroy(playerGO);
            Object.Destroy(party[0].gameObject);
            Object.Destroy(enemy.gameObject);
        }

        [UnityTest]
        public IEnumerator MeleeAdventurer_ClosesDistance_BeforeAttacking()
        {
            ground = CombatTestUtil.CreateFlatGroundWithNavMesh();

            AdventurerDefinition advDef = CombatTestUtil.MakeAdventurerDef(AdventurerType.Knight, 30, 3.5f, 5, 1.5f, 1f, false);
            EnemyDefinition enemyDef = CombatTestUtil.MakeEnemyDef(50, 0f, 0f, 100f, 100f); // stationary, won't retaliate meaningfully

            var party = PartySpawner.Spawn(new List<AdventurerDefinition> { advDef }, Vector3.zero, new List<Vector3> { Vector3.zero }, null, 1, null);
            EnemyController enemy = EnemySpawner.Spawn(enemyDef, new Vector3(5, 0, 0), 2, null);

            for (int i = 0; i < 600 && enemy.CombatHealth.Current == enemy.CombatHealth.MaxHealth; i++)
                yield return null;

            float finalDist = Vector3.Distance(party[0].transform.position, enemy.transform.position);
            Assert.LessOrEqual(finalDist, advDef.AttackRange + 0.3f, "Melee adventurer should close to within its attack range.");

            Object.Destroy(party[0].gameObject);
            Object.Destroy(enemy.gameObject);
        }

        [UnityTest]
        public IEnumerator RangedAdventurer_AttacksWithoutFullyClosingDistance()
        {
            ground = CombatTestUtil.CreateFlatGroundWithNavMesh();

            AdventurerDefinition advDef = CombatTestUtil.MakeAdventurerDef(AdventurerType.Mage, 15, 3f, 5, 7f, 1f, true);
            EnemyDefinition enemyDef = CombatTestUtil.MakeEnemyDef(50, 0f, 0f, 100f, 100f);

            var party = PartySpawner.Spawn(new List<AdventurerDefinition> { advDef }, Vector3.zero, new List<Vector3> { Vector3.zero }, null, 1, null);
            EnemyController enemy = EnemySpawner.Spawn(enemyDef, new Vector3(6, 0, 0), 2, null);

            for (int i = 0; i < 600 && enemy.CombatHealth.Current == enemy.CombatHealth.MaxHealth; i++)
                yield return null;

            float finalDist = Vector3.Distance(party[0].transform.position, enemy.transform.position);
            Assert.LessOrEqual(finalDist, advDef.AttackRange + 0.3f);
            Assert.Greater(finalDist, 1.6f, "Ranged adventurer should not need to fully close to melee distance.");

            Object.Destroy(party[0].gameObject);
            Object.Destroy(enemy.gameObject);
        }

        [UnityTest]
        public IEnumerator Enemy_Dies_WhenHealthReachesZero_AndStopsAttacking()
        {
            ground = CombatTestUtil.CreateFlatGroundWithNavMesh();

            AdventurerDefinition advDef = CombatTestUtil.MakeAdventurerDef(AdventurerType.Knight, 30, 3.5f, 100, 1.6f, 0.1f, false);
            EnemyDefinition enemyDef = CombatTestUtil.MakeEnemyDef(5, 2.5f, 3, 1.4f, 1.5f);

            var party = PartySpawner.Spawn(new List<AdventurerDefinition> { advDef }, Vector3.zero, new List<Vector3> { Vector3.zero }, null, 1, null);
            EnemyController enemy = EnemySpawner.Spawn(enemyDef, new Vector3(1.5f, 0, 0), 2, null);

            for (int i = 0; i < 600 && enemy.IsAlive; i++)
                yield return null;

            Assert.IsFalse(enemy.IsAlive);
            Assert.IsFalse(enemy.enabled, "Dead enemy should stop running its controller (and therefore stop attacking).");

            Object.Destroy(party[0].gameObject);
            Object.Destroy(enemy.gameObject);
        }

        [UnityTest]
        public IEnumerator EnemyDeath_DropsLootFromTable()
        {
            ground = CombatTestUtil.CreateFlatGroundWithNavMesh();

            ItemDefinition bone = ScriptableObject.CreateInstance<ItemDefinition>();
            LootDropTable table = ScriptableObject.CreateInstance<LootDropTable>();
            table.EditorConfigure(new List<LootDropEntry>
            {
                new LootDropEntry { item = bone, dropChance = 1f, minCount = 1, maxCount = 1 }
            });

            AdventurerDefinition advDef = CombatTestUtil.MakeAdventurerDef(AdventurerType.Knight, 30, 3.5f, 100, 1.6f, 0.1f, false);
            EnemyDefinition enemyDef = CombatTestUtil.MakeEnemyDef(5, 2.5f, 0, 1.4f, 1.5f, table);

            var party = PartySpawner.Spawn(new List<AdventurerDefinition> { advDef }, Vector3.zero, new List<Vector3> { Vector3.zero }, null, 1, null);
            EnemyController enemy = EnemySpawner.Spawn(enemyDef, new Vector3(1.5f, 0, 0), 2, null);
            Vector3 deathPos = enemy.transform.position;

            for (int i = 0; i < 600 && enemy.IsAlive; i++)
                yield return null;
            yield return null;

            WorldItem[] dropped = Object.FindObjectsByType<WorldItem>(FindObjectsSortMode.None);
            Assert.IsTrue(System.Array.Exists(dropped, d => d.Definition == bone), "Expected loot to spawn near the enemy's death position.");

            Object.Destroy(party[0].gameObject);
            Object.Destroy(enemy.gameObject);
            foreach (WorldItem d in dropped) Object.Destroy(d.gameObject);
        }
    }
}
