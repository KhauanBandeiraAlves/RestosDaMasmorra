using System.Collections.Generic;
using UnityEngine;

namespace RestosDaMasmorra.Characters.Combat
{
    // Cheap lookup shared by adventurers and enemies so neither has to scan the whole
    // scene every frame to find a target.
    public static class CombatantRegistry
    {
        static readonly List<ICombatant> All = new List<ICombatant>();

        public static void Register(ICombatant combatant)
        {
            if (!All.Contains(combatant)) All.Add(combatant);
        }

        public static void Unregister(ICombatant combatant)
        {
            All.Remove(combatant);
        }

        public static ICombatant FindNearestAlive(Vector3 origin, float radius, Team team)
        {
            ICombatant best = null;
            float bestDistSqr = radius * radius;

            for (int i = 0; i < All.Count; i++)
            {
                ICombatant c = All[i];
                if (c == null || !c.IsAlive || c.CombatTeam != team) continue;

                float distSqr = (c.CombatTransform.position - origin).sqrMagnitude;
                if (distSqr <= bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    best = c;
                }
            }

            return best;
        }

        // Test-only helper: clears stale entries between isolated PlayMode/EditMode test runs.
        public static void ClearAll() => All.Clear();
    }
}
