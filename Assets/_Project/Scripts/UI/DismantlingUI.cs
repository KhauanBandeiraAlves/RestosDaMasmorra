using System.Text;
using UnityEngine;
using UnityEngine.UI;
using RestosDaMasmorra.Economy;

namespace RestosDaMasmorra.UI
{
    public class DismantlingUI : MonoBehaviour
    {
        [SerializeField] GameObject panelRoot;
        [SerializeField] Text contentText;

        public void Configure(GameObject root, Text text)
        {
            panelRoot = root;
            contentText = text;
            Hide();
        }

        public void Show(DismantlingRecipe recipe, int index, int total, bool canDismantle)
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            if (contentText == null) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"BANCADA DE DESMONTAGEM ({index + 1}/{total})");
            sb.AppendLine();

            if (recipe == null || !recipe.IsValid)
            {
                sb.AppendLine("(nenhuma receita configurada)");
            }
            else
            {
                sb.AppendLine($"{recipe.InputItem.DisplayName} x{recipe.InputQuantity}");
                sb.AppendLine("->");
                foreach (DismantlingOutput output in recipe.Outputs)
                {
                    sb.AppendLine($"  {output.item.DisplayName} x{output.quantity}");
                }
                sb.AppendLine();
                sb.AppendLine(canDismantle ? "[E] Desmontar" : "(material insuficiente no estoque)");
            }

            sb.AppendLine("[TAB] trocar receita   [ESC] sair");
            contentText.text = sb.ToString();
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }
    }
}
