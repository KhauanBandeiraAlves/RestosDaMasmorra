using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RestosDaMasmorra.Economy;
using RestosDaMasmorra.Items;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.Tests.EditMode
{
    public class SharedStorageTests
    {
        static ItemDefinition MakeItem(string id, ItemCategory category)
        {
            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            SerializedObjectSet(item, category);
            return item;
        }

        static void SerializedObjectSet(ItemDefinition item, ItemCategory category)
        {
            var so = new UnityEditor.SerializedObject(item);
            so.FindProperty("category").enumValueIndex = (int)category;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void AddItem_ResourceStacks()
        {
            SharedStorage storage = new SharedStorage();
            ItemDefinition bone = MakeItem("bone", ItemCategory.Resource);

            storage.AddItem(new ItemInstance(bone));
            storage.AddItem(new ItemInstance(bone));
            storage.AddItem(new ItemInstance(bone));

            Assert.AreEqual(3, storage.GetStackCount(bone));
            Assert.AreEqual(0, storage.IndividualItems.Count);
        }

        [Test]
        public void AddItem_WeaponDoesNotStack()
        {
            SharedStorage storage = new SharedStorage();
            ItemDefinition sword = MakeItem("sword", ItemCategory.Weapon);

            storage.AddItem(new ItemInstance(sword));
            storage.AddItem(new ItemInstance(sword));

            Assert.AreEqual(0, storage.GetStackCount(sword));
            Assert.AreEqual(2, storage.IndividualItems.Count);
        }

        [Test]
        public void RemoveStack_InsufficientQuantity_Fails()
        {
            SharedStorage storage = new SharedStorage();
            ItemDefinition scrap = MakeItem("scrap", ItemCategory.Scrap);
            storage.AddStack(scrap, 2);

            bool removed = storage.RemoveStack(scrap, 5);

            Assert.IsFalse(removed);
            Assert.AreEqual(2, storage.GetStackCount(scrap));
        }

        [Test]
        public void RemoveStack_SufficientQuantity_Succeeds()
        {
            SharedStorage storage = new SharedStorage();
            ItemDefinition scrap = MakeItem("scrap", ItemCategory.Scrap);
            storage.AddStack(scrap, 5);

            bool removed = storage.RemoveStack(scrap, 5);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, storage.GetStackCount(scrap));
        }

        [Test]
        public void TransferFromRunInventory_MovesAllItems_AndClearsInventory()
        {
            GameObject go = new GameObject("Inv");
            PlayerInventory inventory = go.AddComponent<PlayerInventory>();
            ItemDefinition bone = MakeItem("bone", ItemCategory.Resource);
            inventory.TryAddItem(bone);
            inventory.TryAddItem(bone);

            SharedStorage storage = new SharedStorage();
            storage.TransferFromRunInventory(inventory);

            Assert.AreEqual(2, storage.GetStackCount(bone));
            Assert.AreEqual(0, inventory.Items.Count);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TransferFromRunInventory_NeverDuplicatesItems()
        {
            GameObject go = new GameObject("Inv");
            PlayerInventory inventory = go.AddComponent<PlayerInventory>();
            ItemDefinition sword = MakeItem("sword", ItemCategory.Weapon);
            inventory.TryAddItem(sword, ItemOwnership.PartyOwned);

            SharedStorage storage = new SharedStorage();
            storage.TransferFromRunInventory(inventory);

            // PartyOwned status must not disappear silently.
            Assert.AreEqual(1, storage.IndividualItems.Count);
            Assert.AreEqual(ItemOwnership.PartyOwned, storage.IndividualItems[0].Ownership);
            Object.DestroyImmediate(go);
        }
    }

    public class DismantlingTests
    {
        static ItemDefinition MakeStackableItem(string id)
        {
            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            var so = new UnityEditor.SerializedObject(item);
            so.FindProperty("category").enumValueIndex = (int)ItemCategory.Scrap;
            so.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        static DismantlingRecipe MakeRecipe(ItemDefinition input, int inputQty, ItemDefinition output, int outputQty)
        {
            DismantlingRecipe recipe = ScriptableObject.CreateInstance<DismantlingRecipe>();
            recipe.EditorConfigure(input, inputQty, new List<DismantlingOutput> { new DismantlingOutput { item = output, quantity = outputQty } });
            return recipe;
        }

        [Test]
        public void Recipe_IsValid_WithInputAndOutputs()
        {
            ItemDefinition input = MakeStackableItem("in");
            ItemDefinition output = MakeStackableItem("out");
            DismantlingRecipe recipe = MakeRecipe(input, 1, output, 3);

            Assert.IsTrue(recipe.IsValid);
        }

        [Test]
        public void Recipe_IsInvalid_WithoutInput()
        {
            DismantlingRecipe recipe = ScriptableObject.CreateInstance<DismantlingRecipe>();
            recipe.EditorConfigure(null, 1, new List<DismantlingOutput>());

            Assert.IsFalse(recipe.IsValid);
        }

        [Test]
        public void CannotDismantle_WithoutEnoughInput()
        {
            SharedStorage storage = new SharedStorage();
            ItemDefinition input = MakeStackableItem("in");
            ItemDefinition output = MakeStackableItem("out");
            DismantlingRecipe recipe = MakeRecipe(input, 2, output, 1);
            storage.AddStack(input, 1);

            Assert.IsFalse(DismantlingService.CanDismantle(storage, recipe));
            Assert.IsFalse(DismantlingService.TryDismantle(storage, recipe));
        }

        [Test]
        public void Dismantle_ConsumesInput_AndAddsOutputs()
        {
            SharedStorage storage = new SharedStorage();
            ItemDefinition input = MakeStackableItem("in");
            ItemDefinition output = MakeStackableItem("out");
            DismantlingRecipe recipe = MakeRecipe(input, 1, output, 3);
            storage.AddStack(input, 1);

            bool success = DismantlingService.TryDismantle(storage, recipe);

            Assert.IsTrue(success);
            Assert.AreEqual(0, storage.GetStackCount(input));
            Assert.AreEqual(3, storage.GetStackCount(output));
        }

        [Test]
        public void Dismantle_MultipleOutputs_AllAdded()
        {
            SharedStorage storage = new SharedStorage();
            ItemDefinition input = MakeStackableItem("armor");
            ItemDefinition metal = MakeStackableItem("metal");
            ItemDefinition leather = MakeStackableItem("leather");
            DismantlingRecipe recipe = ScriptableObject.CreateInstance<DismantlingRecipe>();
            recipe.EditorConfigure(input, 1, new List<DismantlingOutput>
            {
                new DismantlingOutput { item = metal, quantity = 2 },
                new DismantlingOutput { item = leather, quantity = 1 },
            });
            storage.AddStack(input, 1);

            DismantlingService.TryDismantle(storage, recipe);

            Assert.AreEqual(2, storage.GetStackCount(metal));
            Assert.AreEqual(1, storage.GetStackCount(leather));
        }
    }

    public class SoloDefeatTests
    {
        static ItemDefinition MakeItem(int slotSize = 1)
        {
            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            var so = new UnityEditor.SerializedObject(item);
            so.FindProperty("slotSize").intValue = slotSize;
            so.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        [Test]
        public void ResolveSoloDefeat_LosesConfiguredPercent()
        {
            GameObject go = new GameObject("Inv");
            PlayerInventory inventory = go.AddComponent<PlayerInventory>();
            var capProp = new UnityEditor.SerializedObject(inventory).FindProperty("capacity");
            capProp.intValue = 20;
            capProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            for (int i = 0; i < 10; i++) inventory.TryAddItem(MakeItem());

            SharedStorage storage = new SharedStorage();
            List<ItemInstance> lost = PlayerLifeController.ResolveSoloDefeat(inventory, storage, 0.3f, new System.Random(1));

            Assert.AreEqual(3, lost.Count); // ceil(10 * 0.3) = 3
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResolveSoloDefeat_PreservesRemainderIntoStorage()
        {
            GameObject go = new GameObject("Inv");
            PlayerInventory inventory = go.AddComponent<PlayerInventory>();
            var capProp = new UnityEditor.SerializedObject(inventory).FindProperty("capacity");
            capProp.intValue = 20;
            capProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            for (int i = 0; i < 10; i++) inventory.TryAddItem(MakeItem());

            SharedStorage storage = new SharedStorage();
            PlayerLifeController.ResolveSoloDefeat(inventory, storage, 0.3f, new System.Random(1));

            Assert.AreEqual(0, inventory.Items.Count);
            Assert.AreEqual(7, storage.TotalCount());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResolveSoloDefeat_IsDeterministic_ForSameSeed()
        {
            GameObject goA = new GameObject("InvA");
            PlayerInventory invA = goA.AddComponent<PlayerInventory>();
            var capA = new UnityEditor.SerializedObject(invA).FindProperty("capacity");
            capA.intValue = 20; capA.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject goB = new GameObject("InvB");
            PlayerInventory invB = goB.AddComponent<PlayerInventory>();
            var capB = new UnityEditor.SerializedObject(invB).FindProperty("capacity");
            capB.intValue = 20; capB.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            ItemDefinition shared = MakeItem();
            for (int i = 0; i < 8; i++) { invA.TryAddItem(shared); invB.TryAddItem(shared); }

            var lostA = PlayerLifeController.ResolveSoloDefeat(invA, new SharedStorage(), 0.5f, new System.Random(99));
            var lostB = PlayerLifeController.ResolveSoloDefeat(invB, new SharedStorage(), 0.5f, new System.Random(99));

            Assert.AreEqual(lostA.Count, lostB.Count);
            Object.DestroyImmediate(goA);
            Object.DestroyImmediate(goB);
        }

        [Test]
        public void ResolveSoloDefeat_NeverTouchesExistingStorageContents()
        {
            GameObject go = new GameObject("Inv");
            PlayerInventory inventory = go.AddComponent<PlayerInventory>();
            var capProp = new UnityEditor.SerializedObject(inventory).FindProperty("capacity");
            capProp.intValue = 20;
            capProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            for (int i = 0; i < 4; i++) inventory.TryAddItem(MakeItem());

            SharedStorage storage = new SharedStorage();
            ItemDefinition oldStuff = MakeItem();
            storage.AddStack(oldStuff, 50); // items from earlier runs

            PlayerLifeController.ResolveSoloDefeat(inventory, storage, 0.3f, new System.Random(5));

            Assert.AreEqual(50, storage.GetStackCount(oldStuff));
        }
    }
}
