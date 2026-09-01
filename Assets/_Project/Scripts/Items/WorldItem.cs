using UnityEngine;
using RestosDaMasmorra.Core;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.Items
{
    [RequireComponent(typeof(Collider))]
    public class WorldItem : MonoBehaviour, IInteractable
    {
        [SerializeField] ItemDefinition definition;
        [SerializeField] ItemOwnership ownership = ItemOwnership.Discarded;

        public ItemDefinition Definition => definition;
        public ItemOwnership Ownership => ownership;

        public string InteractionPrompt =>
            definition != null ? $"E - Coletar {definition.DisplayName}" : "E - Coletar";

        public bool CanInteract => definition != null;

        // Runtime-safe configuration (no UnityEditor dependency), used by LootSpawner and
        // other code that spawns world items procedurally.
        public void EditorConfigure(ItemDefinition itemDefinition, ItemOwnership itemOwnership)
        {
            definition = itemDefinition;
            ownership = itemOwnership;
        }

        public void Interact(GameObject interactor)
        {
            PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();
            if (inventory == null) return;

            if (inventory.TryAddItem(definition, ownership))
            {
                if (ownership == ItemOwnership.PartyOwned)
                {
                    PlayerSuspicion suspicion = interactor.GetComponent<PlayerSuspicion>();
                    suspicion?.Add(Mathf.Max(1, definition.BaseValue));
                }

                gameObject.SetActive(false);
            }
        }
    }
}
