using System.Text;
using UnityEngine;
using UnityEngine.UI;
using RestosDaMasmorra.Economy;

namespace RestosDaMasmorra.UI
{
    public class StorageUI : MonoBehaviour
    {
        [SerializeField] GameObject panelRoot;
        [SerializeField] Text contentText;

        SharedStorage storage;

        public bool IsShown => panelRoot != null && panelRoot.activeSelf;

        public void Configure(GameObject root, Text text)
        {
            panelRoot = root;
            contentText = text;
            Hide();
        }

        public void Show(SharedStorage sharedStorage)
        {
            if (storage != null) storage.Changed -= Refresh;
            storage = sharedStorage;
            if (storage != null) storage.Changed += Refresh;

            if (panelRoot != null) panelRoot.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (storage != null) storage.Changed -= Refresh;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        void Refresh()
        {
            if (contentText == null || storage == null) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ESTOQUE");
            sb.AppendLine();
            foreach (var kvp in storage.Stacks)
            {
                sb.AppendLine($"{kvp.Key.DisplayName} x{kvp.Value}");
            }
            foreach (var item in storage.IndividualItems)
            {
                sb.AppendLine(item.Definition.DisplayName);
            }
            if (storage.TotalCount() == 0) sb.AppendLine("(vazio)");

            contentText.text = sb.ToString();
        }
    }
}
