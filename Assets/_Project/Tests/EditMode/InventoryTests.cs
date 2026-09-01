using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using RestosDaMasmorra.Items;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.Tests.EditMode
{
    public class InventoryTests
    {
        static ItemDefinition MakeItem(string id, int slotSize)
        {
            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            SerializedObject so = new SerializedObject(item);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("slotSize").intValue = slotSize;
            so.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        [Test]
        public void ItemDefinition_ExposesConfiguredFields()
        {
            ItemDefinition item = MakeItem("test_item", 2);
            Assert.AreEqual("test_item", item.Id);
            Assert.AreEqual(2, item.SlotSize);
        }

        [Test]
        public void Inventory_StartsEmpty()
        {
            GameObject go = new GameObject("InventoryTest");
            PlayerInventory inventory = go.AddComponent<PlayerInventory>();

            Assert.AreEqual(0, inventory.UsedSlots);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Inventory_AddItem_IncreasesUsedSlots()
        {
            GameObject go = new GameObject("InventoryTest");
            PlayerInventory inventory = go.AddComponent<PlayerInventory>();
            ItemDefinition item = MakeItem("sword", 2);

            bool added = inventory.TryAddItem(item);

            Assert.IsTrue(added);
            Assert.AreEqual(2, inventory.UsedSlots);
            Assert.AreEqual(1, inventory.Items.Count);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Inventory_RemoveItem_DecreasesUsedSlots()
        {
            GameObject go = new GameObject("InventoryTest");
            PlayerInventory inventory = go.AddComponent<PlayerInventory>();
            ItemDefinition item = MakeItem("sword", 2);
            inventory.TryAddItem(item);
            ItemInstance instance = inventory.Items[0];

            bool removed = inventory.RemoveItem(instance);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, inventory.UsedSlots);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Inventory_PreventsOverflow()
        {
            GameObject go = new GameObject("InventoryTest");
            PlayerInventory inventory = go.AddComponent<PlayerInventory>();
            SerializedObject so = new SerializedObject(inventory);
            so.FindProperty("capacity").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();

            ItemDefinition bigItem = MakeItem("big", 4);

            bool added = inventory.TryAddItem(bigItem);

            Assert.IsFalse(added);
            Assert.AreEqual(0, inventory.UsedSlots);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Inventory_RejectsItemWhenNotEnoughRemainingSpace()
        {
            GameObject go = new GameObject("InventoryTest");
            PlayerInventory inventory = go.AddComponent<PlayerInventory>();
            SerializedObject so = new SerializedObject(inventory);
            so.FindProperty("capacity").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();

            inventory.TryAddItem(MakeItem("a", 2));
            bool secondAdded = inventory.TryAddItem(MakeItem("b", 2));

            Assert.IsFalse(secondAdded);
            Assert.AreEqual(2, inventory.UsedSlots);
            Object.DestroyImmediate(go);
        }
    }
}
