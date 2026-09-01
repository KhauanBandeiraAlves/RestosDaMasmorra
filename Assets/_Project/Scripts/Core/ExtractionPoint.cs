using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using RestosDaMasmorra.Economy;
using RestosDaMasmorra.Player;

namespace RestosDaMasmorra.Core
{
    // Two-step interaction (press E to arm, press E again to confirm) so extraction is
    // never triggered by an accidental tap. Not stamina-gated.
    public class ExtractionPoint : MonoBehaviour, IInteractable, IExtractionPoint
    {
        [SerializeField] string returnSceneName = "PrototypeBase";
        [SerializeField] float confirmWindowSeconds = 5f;

        bool confirming;
        float confirmingSince;

        public string InteractionPrompt => confirming ? "E - Confirmar extração (ESC cancela)" : "E - Extrair";
        public bool CanInteract => true;

        public void Configure(string returnScene) => returnSceneName = returnScene;

        void Update()
        {
            if (!confirming) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                confirming = false;
                return;
            }

            if (Time.time - confirmingSince > confirmWindowSeconds) confirming = false;
        }

        public void Interact(GameObject interactor)
        {
            if (!confirming)
            {
                confirming = true;
                confirmingSince = Time.time;
                return;
            }

            confirming = false;
            Extract(interactor);
        }

        public void Extract(GameObject player)
        {
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null && GameSession.Instance != null)
            {
                GameSession.Instance.Storage.TransferFromRunInventory(inventory);
            }

            if (!SceneLoadGate.SuppressForTests) SceneManager.LoadScene(returnSceneName);
        }
    }
}
