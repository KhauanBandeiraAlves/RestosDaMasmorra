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

        public List<ItemInstance> RemovePortion(float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            int countToRemove = Mathf.CeilToInt(items.Count * fraction);
            List<ItemInstance> removed = new List<ItemInstance>();
            for (int i = 0; i < countToRemove && items.Count > 0; i++)
            {
                int index = items.Count - 1;
                removed.Add(items[index]);
                items.RemoveAt(index);
            }
            if (removed.Count > 0) InventoryChanged?.Invoke();
            return removed;
        }
    }
}
