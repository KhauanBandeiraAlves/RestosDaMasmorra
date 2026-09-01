using System;

namespace RestosDaMasmorra.Items
{
    // A single collected item. Kept deliberately simple (no stacking) for the prototype.
    [Serializable]
    public class ItemInstance
    {
        public ItemDefinition Definition { get; }
        public ItemOwnership Ownership { get; }

        public ItemInstance(ItemDefinition definition, ItemOwnership ownership = ItemOwnership.Discarded)
        {
            Definition = definition;
            Ownership = ownership;
        }
    }
}
