using System;
using System.Collections.Generic;
using UnityEngine;
using RestosDaMasmorra.Items;

namespace RestosDaMasmorra.Player
{
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] int capacity = 10;

        readonly List<ItemInstance> items = new List<ItemInstance>();

        public int Capacity => capacity;
        public int UsedSlots
        {
            get
            {
                int used = 0;
                foreach (ItemInstance item in items) used += item.Definition.SlotSize;
                return used;
            }
        }

        public IReadOnlyList<ItemInstance> Items => items;

        public event Action InventoryChanged;

        public bool HasSpaceFor(ItemDefinition definition)
        {
            if (definition == null) return false;
            return UsedSlots + definition.SlotSize <= capacity;
        }

        public bool TryAddItem(ItemDefinition definition, ItemOwnership ownership = ItemOwnership.Discarded)
        {
            if (!HasSpaceFor(definition)) return false;

            items.Add(new ItemInstance(definition, ownership));
            InventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItem(ItemInstance item)
        {
            bool removed = items.Remove(item);
            if (removed) InventoryChanged?.Invoke();
            return removed;
        }

        public List<ItemInstance> RemoveAll()
        {
            List<ItemInstance> removed = new List<ItemInstance>(items);
            items.Clear();
            InventoryChanged?.Invoke();
            return removed;
        }

        // Removes a fraction of the current items. With no rng, removal is deterministic
        // (from the end of the list) for simple/predictable behavior. Pass an rng (e.g. for
        // a defeat roll) to shuffle selection first — still fully deterministic for a given
        // seed, which is what makes solo-defeat loss testable.
        public List<ItemInstance> RemovePortion(float fraction, System.Random rng = null)
        {
            fraction = Mathf.Clamp01(fraction);
            int countToRemove = Mathf.CeilToInt(items.Count * fraction);

            List<ItemInstance> pool = new List<ItemInstance>(items);
            if (rng != null)
            {
                for (int i = pool.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (pool[i], pool[j]) = (pool[j], pool[i]);
                }
            }

            List<ItemInstance> removed = new List<ItemInstance>();
            for (int i = 0; i < countToRemove && pool.Count > 0; i++)
            {
                ItemInstance instance = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);
                items.Remove(instance);
                removed.Add(instance);
            }

            if (removed.Count > 0) InventoryChanged?.Invoke();
            return removed;
        }
    }
}
