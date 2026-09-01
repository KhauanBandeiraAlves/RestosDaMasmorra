using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RestosDaMasmorra.Characters.Combat;
using RestosDaMasmorra.Items;

namespace RestosDaMasmorra.Characters
{
    public static class PartySpawner
    {
        public static List<AdventurerController> Spawn(
            IReadOnlyList<AdventurerDefinition> definitions,
            Vector3 spawnPosition,
            IReadOnlyList<Vector3> route,
            ItemDefinition brokenWeaponItem,
            int seed,
            Transform parent)
        {
            var result = new List<AdventurerController>();

            for (int i = 0; i < definitions.Count; i++)
            {
                AdventurerDefinition def = definitions[i];
                if (def == null) continue;

                Vector3 offset = new Vector3((i - (definitions.Count - 1) * 0.5f) * 0.8f, 0f, 0f);
                GameObject go = new GameObject("Adventurer_" + def.Type);
                go.transform.SetParent(parent, false);
                go.transform.position = spawnPosition + offset;

                NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
                agent.radius = 0.35f;
                agent.height = 2.6f;
                agent.baseOffset = 0f;
                // Local avoidance keeps a minimum separation between agents that can end up
                // larger than a melee attack range, permanently preventing contact. Combat
                // resolution here is a simple range check, not crowd simulation, so avoidance
                // between combatants only gets in the way.
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

                go.AddComponent<Health>();

                if (def.VisualPrefab != null)
                {
                    GameObject visual = Object.Instantiate(def.VisualPrefab, go.transform);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                }

                AdventurerController controller = go.AddComponent<AdventurerController>();
                controller.Initialize(def, route, brokenWeaponItem, seed + i * 97);

                result.Add(controller);
            }

            return result;
        }
    }
}
