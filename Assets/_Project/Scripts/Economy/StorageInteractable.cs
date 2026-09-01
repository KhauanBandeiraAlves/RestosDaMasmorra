using UnityEngine;
using RestosDaMasmorra.Core;
using RestosDaMasmorra.UI;

namespace RestosDaMasmorra.Economy
{
    public class StorageInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] StorageUI ui;
        bool shown;

        public string InteractionPrompt => shown ? "E - Fechar estoque" : "E - Ver estoque";
        public bool CanInteract => true;

        public void Configure(StorageUI storageUI) => ui = storageUI;

        public void Interact(GameObject interactor)
        {
            shown = !shown;
            if (shown) ui.Show(GameSession.Instance != null ? GameSession.Instance.Storage : null);
            else ui.Hide();
        }
    }
}
