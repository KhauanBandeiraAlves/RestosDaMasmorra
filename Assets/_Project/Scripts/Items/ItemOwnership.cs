namespace RestosDaMasmorra.Items
{
    // Distinguishes loot the party discarded/dropped from loot that still "belongs"
    // to the party and would count as stealing if picked up.
    public enum ItemOwnership
    {
        Discarded,
        PartyOwned
    }
}
