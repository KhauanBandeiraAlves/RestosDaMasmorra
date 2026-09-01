namespace RestosDaMasmorra.Economy
{
    // Pure logic, kept separate from any UI so it's directly EditMode-testable.
    public static class DismantlingService
    {
        public static bool CanDismantle(SharedStorage storage, DismantlingRecipe recipe)
        {
            if (storage == null || recipe == null || !recipe.IsValid) return false;
            return storage.GetStackCount(recipe.InputItem) >= recipe.InputQuantity;
        }

        public static bool TryDismantle(SharedStorage storage, DismantlingRecipe recipe)
        {
            if (!CanDismantle(storage, recipe)) return false;

            storage.RemoveStack(recipe.InputItem, recipe.InputQuantity);
            foreach (DismantlingOutput output in recipe.Outputs)
            {
                storage.AddStack(output.item, output.quantity);
            }

            return true;
        }
    }
}
