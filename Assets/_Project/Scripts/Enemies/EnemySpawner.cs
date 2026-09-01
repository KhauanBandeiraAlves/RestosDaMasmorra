using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RestosDaMasmorra.Characters.Combat;
using RestosDaMasmorra.Dungeon;

namespace RestosDaMasmorra.Enemies
{
    public static class EnemySpawner
    {
        // Reads RoomDefinition's data-driven spawn config (SpawnsEnemies/MinEnemies/
        // MaxEnemies) — never hardcoded by prefab name.
        public static List<EnemyController> SpawnForLayout(DungeonLayoutResult layout, EnemyDefinition enemyDefinition, int seed, Transform parent)
        {
            var result = new List<EnemyController>();
            if (enemyDefinition == null) return result;

            System.Random rng = new System.Random(seed);

            foreach (PlacedRoom room in layout.Rooms)
            {
                if (!room.Definition.SpawnsEnemies) continue;

                int count = room.Definition.MinEnemies >= room.Definition.MaxEnemies
                    ? room.Definition.MinEnemies
                    : rng.Next(room.Definition.MinEnemies, room.Definition.MaxEnemies + 1);

                Vector2 size = room.Definition.Size;
                for (int i = 0; i < count; i++)
                {
                    float ox = ((float)rng.NextDouble() - 0.5f) * (size.x - 3f);
                    float oz = ((float)rng.NextDouble() - 0.5f) * (size.y - 3f);
                    Vector3 pos = room.Position + new Vector3(ox, 0.1f, oz);

                    result.Add(Spawn(enemyDefinition, pos, seed + result.Count * 131, parent));
                }
            }

            return result;
        }

        public static EnemyController Spawn(EnemyDefinition definition, Vector3 position, int seed, Transform parent)
        {
            GameObject go = new GameObject("Enemy_" + definition.DisplayName);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 2.4f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

            go.AddComponent<Health>();

            if (definition.VisualPrefab != null)
            {
                GameObject visual = Object.Instantiate(definition.VisualPrefab, go.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
            }

            EnemyController controller = go.AddComponent<EnemyController>();
            controller.Initialize(definition, seed);
            return controller;
        }
    }
}
