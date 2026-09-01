using System;
using System.Collections.Generic;
using RestosDaMasmorra.Items;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.Economy
{
    // Base's permanent stockpile. Resource/Scrap items stack by count; Weapon/Armor/
    // PartyLoot stay as individual instances so they can carry per-item properties later
    // (quality, durability, modular parts) without a rework.
    public class SharedStorage
    {
        readonly Dictionary<ItemDefinition, int> stacks = new Dictionary<ItemDefinition, int>();
        readonly List<ItemInstance> individualItems = new List<ItemInstance>();

        public IReadOnlyDictionary<ItemDefinition, int> Stacks => stacks;
        public IReadOnlyList<ItemInstance> IndividualItems => individualItems;

        public event Action Changed;

        public static bool IsStackable(ItemDefinition item) =>
            item != null && (item.Category == ItemCategory.Resource || item.Category == ItemCategory.Scrap);

        public void AddItem(ItemInstance instance)
        {
            if (instance?.Definition == null) return;

            if (IsStackable(instance.Definition)) AddStack(instance.Definition, 1);
            else individualItems.Add(instance);

            Changed?.Invoke();
        }

        public void AddStack(ItemDefinition item, int count)
        {
            if (item == null || count <= 0) return;
            stacks.TryGetValue(item, out int current);
            stacks[item] = current + count;
            Changed?.Invoke();
        }

        public bool RemoveStack(ItemDefinition item, int count)
        {
            if (item == null || count <= 0) return false;
            if (!stacks.TryGetValue(item, out int current) || current < count) return false;

            int remaining = current - count;
            if (remaining > 0) stacks[item] = remaining;
            else stacks.Remove(item);

            Changed?.Invoke();
            return true;
        }

        public int GetStackCount(ItemDefinition item)
        {
            stacks.TryGetValue(item, out int count);
            return count;
        }

        public bool RemoveIndividual(ItemInstance instance)
        {
            bool removed = individualItems.Remove(instance);
            if (removed) Changed?.Invoke();
            return removed;
        }

        public int TotalCount()
        {
            int total = individualItems.Count;
            foreach (int c in stacks.Values) total += c;
            return total;
        }

        // Moves everything currently in the run inventory into permanent storage, then
        // clears the run inventory. Single pass over a snapshot of the source list, so a
        // partially-consumed inventory can never leave items double-counted.
        public void TransferFromRunInventory(PlayerInventory inventory)
        {
            if (inventory == null) return;

            List<ItemInstance> snapshot = new List<ItemInstance>(inventory.Items);
            foreach (ItemInstance item in snapshot) AddItem(item);

            inventory.RemoveAll();
        }
    }
}
