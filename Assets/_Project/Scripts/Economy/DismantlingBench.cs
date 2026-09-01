using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RestosDaMasmorra.Core;
using RestosDaMasmorra.UI;

namespace RestosDaMasmorra.Economy
{
    public class DismantlingBench : MonoBehaviour, IInteractable
    {
        [SerializeField] List<DismantlingRecipe> recipes = new List<DismantlingRecipe>();
        [SerializeField] DismantlingUI ui;

        bool active;
        int selectedIndex;

        public string InteractionPrompt => "E - Usar bancada";
        public bool CanInteract => !active;

        public void Configure(List<DismantlingRecipe> recipeList, DismantlingUI uiPanel)
        {
            recipes = recipeList;
            ui = uiPanel;
        }

        public void Interact(GameObject interactor)
        {
            active = true;
            selectedIndex = 0;
            RefreshUI();
        }

        void Update()
        {
            if (!active) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }
            if (kb.tabKey.wasPressedThisFrame && recipes.Count > 0)
            {
                selectedIndex = (selectedIndex + 1) % recipes.Count;
                RefreshUI();
            }
            if (kb.eKey.wasPressedThisFrame)
            {
                TryConfirm();
            }
        }

        void TryConfirm()
        {
            if (recipes.Count == 0 || GameSession.Instance == null) return;
            DismantlingRecipe recipe = recipes[selectedIndex];
            DismantlingService.TryDismantle(GameSession.Instance.Storage, recipe);
            RefreshUI();
        }

        void Close()
        {
            active = false;
            ui?.Hide();
        }

        void RefreshUI()
        {
            if (ui == null) return;
            DismantlingRecipe recipe = recipes.Count > 0 ? recipes[selectedIndex] : null;
            bool can = GameSession.Instance != null && DismantlingService.CanDismantle(GameSession.Instance.Storage, recipe);
            ui.Show(recipe, selectedIndex, Mathf.Max(1, recipes.Count), can);
        }
    }
}
