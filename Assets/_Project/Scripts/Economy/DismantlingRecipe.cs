using System;
using System.Collections.Generic;
using UnityEngine;
using RestosDaMasmorra.Items;

namespace RestosDaMasmorra.Economy
{
    [Serializable]
    public class DismantlingOutput
    {
        public ItemDefinition item;
        [Min(1)] public int quantity = 1;
    }

    [CreateAssetMenu(fileName = "NewDismantlingRecipe", menuName = "RestosDaMasmorra/Dismantling Recipe")]
    public class DismantlingRecipe : ScriptableObject
    {
        [SerializeField] ItemDefinition inputItem;
        [SerializeField, Min(1)] int inputQuantity = 1;
        [SerializeField] List<DismantlingOutput> outputs = new List<DismantlingOutput>();

        public ItemDefinition InputItem => inputItem;
        public int InputQuantity => inputQuantity;
        public IReadOnlyList<DismantlingOutput> Outputs => outputs;

        public bool IsValid => inputItem != null && inputQuantity > 0 && outputs != null && outputs.Count > 0;

        public void EditorConfigure(ItemDefinition input, int quantity, List<DismantlingOutput> outputList)
        {
            inputItem = input;
            inputQuantity = quantity;
            outputs = outputList;
        }
    }
}
