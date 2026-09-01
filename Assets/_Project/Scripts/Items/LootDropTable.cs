using System;
using System.Collections.Generic;
using UnityEngine;

namespace RestosDaMasmorra.Items
{
    [Serializable]
    public class LootDropEntry
    {
        public ItemDefinition item;
        [Range(0f, 1f)] public float dropChance = 0.5f;
        [Min(1)] public int minCount = 1;
        [Min(1)] public int maxCount = 1;
    }

    [CreateAssetMenu(fileName = "NewLootDropTable", menuName = "RestosDaMasmorra/Loot Drop Table")]
    public class LootDropTable : ScriptableObject
    {
        [SerializeField] List<LootDropEntry> entries = new List<LootDropEntry>();

        public IReadOnlyList<LootDropEntry> Entries => entries;

        public void EditorConfigure(List<LootDropEntry> newEntries) => entries = newEntries;

        // Rolls every entry independently against its own chance. Deterministic given the
        // supplied System.Random, so it stays test-friendly.
        public List<(ItemDefinition item, int count)> Roll(System.Random rng)
        {
            var results = new List<(ItemDefinition, int)>();
            foreach (LootDropEntry entry in entries)
            {
                if (entry.item == null) continue;
                if (rng.NextDouble() > entry.dropChance) continue;

                int count = entry.minCount >= entry.maxCount ? entry.minCount : rng.Next(entry.minCount, entry.maxCount + 1);
                results.Add((entry.item, count));
            }
            return results;
        }
    }
}
