using UnityEngine;
using UnityEngine.UI;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.UI
{
    // Minimal, discreet dungeon HUD: stamina, backpack usage, current interaction prompt,
    // suspicion (only shown once > 0).
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] PlayerStamina stamina;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] PlayerInteraction interaction;
        [SerializeField] PlayerSuspicion suspicion;

        [SerializeField] Text staminaText;
        [SerializeField] Text backpackText;
        [SerializeField] Text interactionText;
        [SerializeField] Text suspicionText;

        void OnEnable()
        {
            if (stamina != null) stamina.StaminaChanged += OnStaminaChanged;
            if (inventory != null) inventory.InventoryChanged += OnInventoryChanged;
            if (interaction != null) interaction.InteractableChanged += OnInteractableChanged;
            if (suspicion != null) suspicion.Changed += SetSuspicion;

            OnInventoryChanged();
            OnInteractableChanged(null);
            SetSuspicion(suspicion != null ? suspicion.Value : 0);
        }

        void OnDisable()
        {
            if (stamina != null) stamina.StaminaChanged -= OnStaminaChanged;
            if (inventory != null) inventory.InventoryChanged -= OnInventoryChanged;
            if (interaction != null) interaction.InteractableChanged -= OnInteractableChanged;
            if (suspicion != null) suspicion.Changed -= SetSuspicion;
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
            if (suspicionText != null)
                suspicionText.text = value > 0 ? $"Suspicion: {value}" : "";
        }
    }
}
