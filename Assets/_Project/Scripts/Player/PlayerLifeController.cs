using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RestosDaMasmorra.Characters.Combat;
using RestosDaMasmorra.Core;
using RestosDaMasmorra.Economy;
using RestosDaMasmorra.Items;

namespace RestosDaMasmorra.Player
{
    // Centralizes life/defeat state so nothing downstream needs an "if (multiplayer)"
    // branch: solo play resolves Downed straight into Returned; Phase E will instead move
    // Downed -> Reviving -> Alive when another player revives, using the same states.
    public class PlayerLifeController : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] string returnSceneName = "PrototypeBase";

        public PlayerLifeState State { get; private set; } = PlayerLifeState.Alive;
        public event Action Defeated;

        public void Configure(Health playerHealth, PlayerInventory playerInventory, string returnScene)
        {
            if (health != null) health.Died -= HandleDeath;
            health = playerHealth;
            inventory = playerInventory;
            returnSceneName = returnScene;
            if (health != null) health.Died += HandleDeath;
        }

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
        }

        void OnEnable()
        {
            if (health != null) health.Died += HandleDeath;
        }

        void OnDisable()
        {
            if (health != null) health.Died -= HandleDeath;
        }

        void HandleDeath()
        {
            if (State != PlayerLifeState.Alive) return;
            State = PlayerLifeState.Downed;

            float lossPercent = GameSession.Instance != null ? GameSession.Instance.LossPercentOnSoloDefeat : 0.3f;
            SharedStorage storage = GameSession.Instance != null ? GameSession.Instance.Storage : null;
            System.Random rng = new System.Random(Environment.TickCount);

            ResolveSoloDefeat(inventory, storage, lossPercent, rng);

            State = PlayerLifeState.Returned;
            Defeated?.Invoke();

            health.SetMaxHealth(health.MaxHealth); // full heal for the next run

            if (!SceneLoadGate.SuppressForTests) SceneManager.LoadScene(returnSceneName);
        }

        // Pure and testable: removes lossPercent of the run inventory (destroyed) and
        // transfers whatever remains to permanent storage. Never touches storage contents
        // from earlier runs — it only ever adds to them.
        public static List<ItemInstance> ResolveSoloDefeat(PlayerInventory inventory, SharedStorage storage, float lossPercent, System.Random rng)
        {
            List<ItemInstance> lost = inventory.RemovePortion(lossPercent, rng);
            storage?.TransferFromRunInventory(inventory);
            return lost;
        }
    }
}
