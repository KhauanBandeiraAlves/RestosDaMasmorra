using System;
using UnityEngine;

namespace RestosDaMasmorra.Player
{
    // Foundation only: tracks a simple accumulating value when the player picks up
    // PartyOwned loot. No AI reaction yet — just the number.
    public class PlayerSuspicion : MonoBehaviour
    {
        public int Value { get; private set; }

        public event Action<int> Changed;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Value += amount;
            Changed?.Invoke(Value);
        }
    }
}
