using UnityEngine;

namespace RestosDaMasmorra.Core
{
    // Anything that can end a run and send the player back to the base — the Entrance
    // today, potentially an internal extraction point or a helper carrying loot later.
    public interface IExtractionPoint
    {
        void Extract(GameObject player);
    }
}
