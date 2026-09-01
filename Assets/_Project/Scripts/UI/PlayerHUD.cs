using UnityEngine;
using UnityEngine.UI;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.UI
{
    // Minimal, discreet dungeon HUD: stamina, backpack usage, current interaction prompt.
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] PlayerStamina stamina;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] PlayerInteraction interaction;

        [SerializeField] Text staminaText;
        [SerializeField] Text backpackText;
        [SerializeField] Text interactionText;
        [SerializeField] Text suspicionText;

        [SerializeField] int suspicion;

        void OnEnable()
        {
            if (stamina != null) stamina.StaminaChanged += OnStaminaChanged;
            if (inventory != null) inventory.InventoryChanged += OnInventoryChanged;
            if (interaction != null) interaction.InteractableChanged += OnInteractableChanged;

            OnInventoryChanged();
            OnInteractableChanged(null);
        }

        void OnDisable()
        {
            if (stamina != null) stamina.StaminaChanged -= OnStaminaChanged;
            if (inventory != null) inventory.InventoryChanged -= OnInventoryChanged;
            if (interaction != null) interaction.InteractableChanged -= OnInteractableChanged;
        }

        void OnStaminaChanged(float current, float max)
        {
            if (staminaText != null) staminaText.text = $"Stamina: {current:F0} / {max:F0}";
        }

        void OnInventoryChanged()
        {
            if (backpackText != null && inventory != null)
                backpackText.text = $"Backpack {inventory.UsedSlots} / {inventory.Capacity}";
        }

        void OnInteractableChanged(string prompt)
        {
            if (interactionText != null)
                interactionText.text = string.IsNullOrEmpty(prompt) ? "" : prompt;
        }

        public void SetSuspicion(int value)
        {
            suspicion = value;
            if (suspicionText != null)
                suspicionText.text = suspicion > 0 ? $"Suspicion: {suspicion}" : "";
        }
    }
}
