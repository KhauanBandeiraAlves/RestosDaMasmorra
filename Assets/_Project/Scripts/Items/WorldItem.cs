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

        public string InteractionPrompt =>
            definition != null ? $"E - Coletar {definition.DisplayName}" : "E - Coletar";

        public bool CanInteract => definition != null;

        public void Interact(GameObject interactor)
        {
            PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();
            if (inventory == null) return;

            if (inventory.TryAddItem(definition, ownership))
            {
                gameObject.SetActive(false);
            }
        }
    }
}
