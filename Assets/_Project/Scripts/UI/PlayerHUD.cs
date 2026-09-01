using UnityEngine;
using UnityEngine.UI;
using RestosDaMasmorra.Characters.Combat;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.UI
{
    // Minimal, discreet dungeon HUD: health, stamina, backpack usage, current interaction
    // prompt, suspicion (only shown once > 0).
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField] PlayerStamina stamina;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] PlayerInteraction interaction;
        [SerializeField] PlayerSuspicion suspicion;

        [SerializeField] Text healthText;
        [SerializeField] Text staminaText;
        [SerializeField] Text backpackText;
        [SerializeField] Text interactionText;
        [SerializeField] Text suspicionText;

        void OnEnable()
        {
            if (health != null) health.DamageTaken += OnHealthChanged;
            if (stamina != null) stamina.StaminaChanged += OnStaminaChanged;
            if (inventory != null) inventory.InventoryChanged += OnInventoryChanged;
            if (interaction != null) interaction.InteractableChanged += OnInteractableChanged;
            if (suspicion != null) suspicion.Changed += SetSuspicion;

            RefreshNow();
        }

        // OnEnable (and therefore the initial text population above) only runs once a
        // scene is actually in Play Mode. Editor tooling that wants a HUD screenshot with
        // real values — the scene isn't playing — should call this directly first.
        public void RefreshNow()
        {
            OnHealthChanged(0f);
            if (stamina != null) OnStaminaChanged(stamina.Current, stamina.MaxStamina);
            OnInventoryChanged();
            OnInteractableChanged(null);
            SetSuspicion(suspicion != null ? suspicion.Value : 0);
        }

        void OnDisable()
        {
            if (health != null) health.DamageTaken -= OnHealthChanged;
            if (stamina != null) stamina.StaminaChanged -= OnStaminaChanged;
            if (inventory != null) inventory.InventoryChanged -= OnInventoryChanged;
            if (interaction != null) interaction.InteractableChanged -= OnInteractableChanged;
            if (suspicion != null) suspicion.Changed -= SetSuspicion;
        }

        void OnHealthChanged(float _)
        {
            if (healthText != null && health != null)
                healthText.text = $"Health: {health.Current:F0} / {health.MaxHealth:F0}";
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
