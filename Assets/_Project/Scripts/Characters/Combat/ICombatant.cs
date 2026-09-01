using UnityEngine;

namespace RestosDaMasmorra.Characters.Combat
{
    public interface ICombatant
    {
        Transform CombatTransform { get; }
        Health CombatHealth { get; }
        Team CombatTeam { get; }
        bool IsAlive { get; }
    }
}
