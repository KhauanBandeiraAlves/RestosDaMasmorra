using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RestosDaMasmorra.Characters.Combat;
using RestosDaMasmorra.Items;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.Tests.EditMode
{
    public class HealthTests
    {
        [Test]
        public void TakeDamage_ReducesCurrent()
        {
            GameObject go = new GameObject("HealthTest");
            Health health = go.AddComponent<Health>();
            health.SetMaxHealth(10f);

            health.TakeDamage(4f);

            Assert.AreEqual(6f, health.Current);
            Assert.IsTrue(health.IsAlive);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TakeDamage_BeyondMax_ClampsToZero_AndFiresDied()
        {
            GameObject go = new GameObject("HealthTest");
            Health health = go.AddComponent<Health>();
            health.SetMaxHealth(5f);

            bool died = false;
            health.Died += () => died = true;

            health.TakeDamage(999f);

            Assert.AreEqual(0f, health.Current);
            Assert.IsFalse(health.IsAlive);
            Assert.IsTrue(died);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void DeadEnemy_TakeDamage_DoesNothing_AndDoesNotRefireDied()
        {
            GameObject go = new GameObject("HealthTest");
            Health health = go.AddComponent<Health>();
            health.SetMaxHealth(5f);
            int deathCount = 0;
            health.Died += () => deathCount++;

            health.TakeDamage(999f);
            health.TakeDamage(1f);

            Assert.AreEqual(1, deathCount);
            Object.DestroyImmediate(go);
        }
    }

    public class CombatantRegistryTests
    {
        class FakeCombatant : ICombatant
        {
            public Transform CombatTransform { get; set; }
            public Health CombatHealth { get; set; }
            public Team CombatTeam { get; set; }
            public bool IsAlive { get; set; } = true;
        }

        [TearDown]
        public void TearDown() => CombatantRegistry.ClearAll();

        [Test]
        public void FindNearestAlive_ReturnsClosestMatchingTeam_WithinRadius()
        {
            GameObject a = new GameObject("A");
            GameObject b = new GameObject("B");
            a.transform.position = new Vector3(1, 0, 0);
            b.transform.position = new Vector3(5, 0, 0);

            var near = new FakeCombatant { CombatTransform = a.transform, CombatTeam = Team.Enemy };
            var far = new FakeCombatant { CombatTransform = b.transform, CombatTeam = Team.Enemy };
            CombatantRegistry.Register(near);
            CombatantRegistry.Register(far);

            ICombatant found = CombatantRegistry.FindNearestAlive(Vector3.zero, 10f, Team.Enemy);

            Assert.AreSame(near, found);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void FindNearestAlive_IgnoresDeadAndWrongTeam()
        {
            GameObject a = new GameObject("A");
            a.transform.position = Vector3.zero;

            var dead = new FakeCombatant { CombatTransform = a.transform, CombatTeam = Team.Enemy, IsAlive = false };
            var wrongTeam = new FakeCombatant { CombatTransform = a.transform, CombatTeam = Team.Party, IsAlive = true };
            CombatantRegistry.Register(dead);
            CombatantRegistry.Register(wrongTeam);

            ICombatant found = CombatantRegistry.FindNearestAlive(Vector3.zero, 10f, Team.Enemy);

            Assert.IsNull(found);
            Object.DestroyImmediate(a);
        }

        [Test]
        public void FindNearestAlive_RespectsRadius()
        {
            GameObject a = new GameObject("A");
            a.transform.position = new Vector3(20, 0, 0);
            var farAway = new FakeCombatant { CombatTransform = a.transform, CombatTeam = Team.Enemy };
            CombatantRegistry.Register(farAway);

            ICombatant found = CombatantRegistry.FindNearestAlive(Vector3.zero, 5f, Team.Enemy);

            Assert.IsNull(found);
            Object.DestroyImmediate(a);
        }
    }

    public class LootDropTableTests
    {
        static ItemDefinition MakeItem(string id)
        {
            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            return item;
        }

        [Test]
        public void Roll_AlwaysDropsEntry_WithChanceOne()
        {
            LootDropTable table = ScriptableObject.CreateInstance<LootDropTable>();
            ItemDefinition bone = MakeItem("bone");
            table.EditorConfigure(new List<LootDropEntry>
            {
                new LootDropEntry { item = bone, dropChance = 1f, minCount = 2, maxCount = 2 }
            });

            var drops = table.Roll(new System.Random(1));

            Assert.AreEqual(1, drops.Count);
            Assert.AreEqual(bone, drops[0].item);
            Assert.AreEqual(2, drops[0].count);
        }

        [Test]
        public void Roll_NeverDropsEntry_WithChanceZero()
        {
            LootDropTable table = ScriptableObject.CreateInstance<LootDropTable>();
            table.EditorConfigure(new List<LootDropEntry>
            {
                new LootDropEntry { item = MakeItem("x"), dropChance = 0f, minCount = 1, maxCount = 1 }
            });

            var drops = table.Roll(new System.Random(1));

            Assert.AreEqual(0, drops.Count);
        }

        [Test]
        public void Roll_IsDeterministic_ForSameSeed()
        {
            LootDropTable table = ScriptableObject.CreateInstance<LootDropTable>();
            table.EditorConfigure(new List<LootDropEntry>
            {
                new LootDropEntry { item = MakeItem("a"), dropChance = 0.5f, minCount = 1, maxCount = 3 },
                new LootDropEntry { item = MakeItem("b"), dropChance = 0.5f, minCount = 1, maxCount = 3 },
            });

            var dropsA = table.Roll(new System.Random(42));
            var dropsB = table.Roll(new System.Random(42));

            Assert.AreEqual(dropsA.Count, dropsB.Count);
        }
    }

    public class PartyOwnedSuspicionTests
    {
        [Test]
        public void CollectingPartyOwnedItem_IncreasesSuspicion()
        {
            GameObject playerGO = new GameObject("Player");
            PlayerInventory inventory = playerGO.AddComponent<PlayerInventory>();
            PlayerSuspicion suspicion = playerGO.AddComponent<PlayerSuspicion>();

            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();

            GameObject worldItemGO = new GameObject("WorldItem");
            worldItemGO.AddComponent<SphereCollider>();
            WorldItem worldItem = worldItemGO.AddComponent<WorldItem>();
            worldItem.EditorConfigure(item, ItemOwnership.PartyOwned);

            worldItem.Interact(playerGO);

            Assert.Greater(suspicion.Value, 0);
            Assert.AreEqual(1, inventory.Items.Count);
            Assert.AreEqual(ItemOwnership.PartyOwned, inventory.Items[0].Ownership);

            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(worldItemGO);
        }

        [Test]
        public void CollectingDiscardedItem_DoesNotIncreaseSuspicion()
        {
            GameObject playerGO = new GameObject("Player");
            playerGO.AddComponent<PlayerInventory>();
            PlayerSuspicion suspicion = playerGO.AddComponent<PlayerSuspicion>();

            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();

            GameObject worldItemGO = new GameObject("WorldItem");
            worldItemGO.AddComponent<SphereCollider>();
            WorldItem worldItem = worldItemGO.AddComponent<WorldItem>();
            worldItem.EditorConfigure(item, ItemOwnership.Discarded);

            worldItem.Interact(playerGO);

            Assert.AreEqual(0, suspicion.Value);

            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(worldItemGO);
        }
    }
}
