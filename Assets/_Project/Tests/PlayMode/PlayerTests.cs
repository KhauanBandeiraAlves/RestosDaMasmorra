using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RestosDaMasmorra.Items;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.Tests.PlayMode
{
    static class ReflectionTestUtil
    {
        public static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }
    }

    public class PlayerMovementAndStaminaTests
    {
        static GameObject CreatePlayer()
        {
            GameObject go = new GameObject("TestPlayer");
            go.AddComponent<CharacterController>();
            go.AddComponent<PlayerStamina>();
            go.AddComponent<PlayerMovement>();
            go.AddComponent<PlayerInteraction>();
            go.AddComponent<PlayerInventory>();
            return go;
        }

        [UnityTest]
        [Timeout(20000)]
        public IEnumerator Player_SpawnsWithRequiredComponents()
        {
            GameObject player = CreatePlayer();
            yield return null;

            Assert.IsNotNull(player.GetComponent<CharacterController>());
            Assert.IsNotNull(player.GetComponent<PlayerMovement>());
            Assert.IsNotNull(player.GetComponent<PlayerStamina>());
            Assert.IsNotNull(player.GetComponent<PlayerInventory>());

            Object.Destroy(player);
        }

        [UnityTest]
        [Timeout(20000)]
        public IEnumerator Player_MovesForward_WhenGivenForwardInput()
        {
            GameObject player = CreatePlayer();
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            yield return null;

            Vector3 startPos = player.transform.position;

            for (int i = 0; i < 30; i++)
            {
                movement.Tick(new Vector2(0f, 1f), false, 0.05f);
                yield return null;
            }

            float distanceMoved = Vector3.Distance(startPos, player.transform.position);
            Assert.Greater(distanceMoved, 0.05f);

            Object.Destroy(player);
        }

        [UnityTest]
        [Timeout(20000)]
        public IEnumerator Player_StaminaDrainsWhileRunning()
        {
            GameObject player = CreatePlayer();
            PlayerStamina stamina = player.GetComponent<PlayerStamina>();
            yield return null;

            float startStamina = stamina.Current;
            for (int i = 0; i < 30; i++)
            {
                stamina.Tick(0.1f, true);
                yield return null;
            }

            Assert.Less(stamina.Current, startStamina);
            Object.Destroy(player);
        }

        [UnityTest]
        [Timeout(20000)]
        public IEnumerator Player_StaminaRegeneratesAfterRunning()
        {
            GameObject player = CreatePlayer();
            PlayerStamina stamina = player.GetComponent<PlayerStamina>();
            yield return null;

            for (int i = 0; i < 40; i++) stamina.Tick(0.1f, true);
            float drained = stamina.Current;

            for (int i = 0; i < 40; i++) stamina.Tick(0.1f, false);

            Assert.Greater(stamina.Current, drained);
            Object.Destroy(player);
        }

        [UnityTest]
        [Timeout(20000)]
        public IEnumerator Player_ZeroStamina_StillAllowsWalking()
        {
            GameObject player = CreatePlayer();
            PlayerStamina stamina = player.GetComponent<PlayerStamina>();
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            yield return null;

            for (int i = 0; i < 100; i++) stamina.Tick(0.5f, true);
            Assert.AreEqual(0f, stamina.Current);
            Assert.IsFalse(stamina.CanRun);

            Vector3 startPos = player.transform.position;
            for (int i = 0; i < 30; i++)
            {
                movement.Tick(new Vector2(0f, 1f), false, 0.05f);
                yield return null;
            }

            Assert.Greater(Vector3.Distance(startPos, player.transform.position), 0.05f);
            Object.Destroy(player);
        }
    }

    public class WorldItemPickupTests
    {
        [UnityTest]
        [Timeout(20000)]
        public IEnumerator Player_CanPickUpWorldItem_WhenInventoryHasSpace()
        {
            GameObject playerGO = new GameObject("TestPlayer");
            PlayerInventory inventory = playerGO.AddComponent<PlayerInventory>();
            yield return null;

            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();

            GameObject worldItemGO = new GameObject("TestWorldItem");
            worldItemGO.AddComponent<SphereCollider>();
            WorldItem worldItem = worldItemGO.AddComponent<WorldItem>();
            ReflectionTestUtil.SetPrivateField(worldItem, "definition", item);

            worldItem.Interact(playerGO);
            yield return null;

            Assert.AreEqual(1, inventory.Items.Count);
            Assert.IsFalse(worldItemGO.activeSelf);

            Object.Destroy(playerGO);
            Object.Destroy(worldItemGO);
        }

        [UnityTest]
        [Timeout(20000)]
        public IEnumerator Player_CannotPickUpWorldItem_WhenInventoryIsFull()
        {
            GameObject playerGO = new GameObject("TestPlayer");
            PlayerInventory inventory = playerGO.AddComponent<PlayerInventory>();
            ReflectionTestUtil.SetPrivateField(inventory, "capacity", 1);
            yield return null;

            ItemDefinition bigItem = ScriptableObject.CreateInstance<ItemDefinition>();
            ReflectionTestUtil.SetPrivateField(bigItem, "slotSize", 5);

            GameObject worldItemGO = new GameObject("TestWorldItem");
            worldItemGO.AddComponent<SphereCollider>();
            WorldItem worldItem = worldItemGO.AddComponent<WorldItem>();
            ReflectionTestUtil.SetPrivateField(worldItem, "definition", bigItem);

            worldItem.Interact(playerGO);
            yield return null;

            Assert.AreEqual(0, inventory.Items.Count);
            Assert.IsTrue(worldItemGO.activeSelf);

            Object.Destroy(playerGO);
            Object.Destroy(worldItemGO);
        }
    }
}
