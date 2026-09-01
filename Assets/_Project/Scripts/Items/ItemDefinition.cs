using UnityEngine;

namespace RestosDaMasmorra.Items
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "RestosDaMasmorra/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] string id = "item_id";
        [SerializeField] string displayName = "Item";
        [SerializeField] ItemCategory category = ItemCategory.Scrap;
        [SerializeField, Min(1)] int slotSize = 1;
        [SerializeField, Min(0)] int baseValue = 0;
        [SerializeField] string materialType = "";

        public string Id => id;
        public string DisplayName => displayName;
        public ItemCategory Category => category;
        public int SlotSize => slotSize;
        public int BaseValue => baseValue;
        public string MaterialType => materialType;
    }
}
