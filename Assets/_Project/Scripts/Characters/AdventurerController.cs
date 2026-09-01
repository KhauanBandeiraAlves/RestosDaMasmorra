using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RestosDaMasmorra.Characters.Combat;
using RestosDaMasmorra.Items;

namespace RestosDaMasmorra.Characters
{
    // Advances through the dungeon's main path on its own, fights any enemy that gets
    // close, and keeps going afterwards. Never waits for the player.
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class AdventurerController : MonoBehaviour, ICombatant
    {
        [SerializeField] float detectionRadius = 8f;
        [SerializeField] float waypointArrivalDistance = 0.6f;
        [SerializeField] float brokenWeaponDropChance = 0.15f;

        NavMeshAgent agent;
        Health health;
        AdventurerDefinition definition;
        List<Vector3> route = new List<Vector3>();
        int routeIndex;
        ICombatant target;
        float attackTimer;
        ItemDefinition brokenWeaponItem;
        System.Random rng;

        public Transform CombatTransform => transform;
        public Health CombatHealth => health;
        public Team CombatTeam => Team.Party;
        public bool IsAlive => health.IsAlive;
        public AdventurerDefinition Definition => definition;
        public bool HasReachedEndOfRoute => routeIndex >= route.Count;

        // Awake/OnEnable only run once the scene is actually in Play Mode; editor-time
        // tooling (batch-mode scene builders, screenshot tools) calls Initialize()
        // straight after AddComponent while still in Edit Mode, so refs are fetched
        // idempotently here too rather than relying solely on Awake having already run.
        void EnsureRefs()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (health == null) health = GetComponent<Health>();
        }

        void Awake() => EnsureRefs();

        void OnEnable() => CombatantRegistry.Register(this);
        void OnDisable() => CombatantRegistry.Unregister(this);

        public void Initialize(AdventurerDefinition def, IReadOnlyList<Vector3> waypoints, ItemDefinition brokenWeapon, int seed)
        {
            EnsureRefs();
            definition = def;
            brokenWeaponItem = brokenWeapon;
            rng = new System.Random(seed);

            health.SetMaxHealth(def.Health);
            agent.speed = def.MoveSpeed;

            route = new List<Vector3>(waypoints);
            routeIndex = 0;
            if (route.Count > 0) agent.SetDestination(route[0]);

            health.Died += HandleDeath;
        }

        void Update()
        {
            if (!health.IsAlive) return;

            if (target == null || !target.IsAlive)
            {
                target = CombatantRegistry.FindNearestAlive(transform.position, detectionRadius, Team.Enemy);
            }

            if (target != null) Fight();
            else Travel();
        }

        void Travel()
        {
            if (agent.isStopped) agent.isStopped = false;
            if (routeIndex >= route.Count) return;

            if (!agent.pathPending && agent.remainingDistance <= waypointArrivalDistance)
            {
                routeIndex++;
                if (routeIndex < route.Count) agent.SetDestination(route[routeIndex]);
            }
        }

        void Fight()
        {
            float dist = Vector3.Distance(transform.position, target.CombatTransform.position);

            if (dist > definition.AttackRange)
            {
                agent.isStopped = false;
                agent.SetDestination(target.CombatTransform.position);
                return;
            }

            agent.isStopped = true;
            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f) return;

            target.CombatHealth.TakeDamage(definition.AttackDamage);
            attackTimer = definition.AttackCooldown;

            if (!target.IsAlive)
            {
                TryDropBrokenWeapon();
                target = null;
                if (routeIndex < route.Count) agent.SetDestination(route[routeIndex]);
            }
        }

        void TryDropBrokenWeapon()
        {
            if (brokenWeaponItem == null || rng == null) return;
            if (rng.NextDouble() > brokenWeaponDropChance) return;

            Vector3 dropPos = transform.position + new Vector3((float)rng.NextDouble() - 0.5f, 0f, (float)rng.NextDouble() - 0.5f);
            LootSpawner.SpawnDrop(brokenWeaponItem, dropPos, ItemOwnership.Discarded);
        }

        void HandleDeath()
        {
            agent.isStopped = true;
            enabled = false;
        }
    }
}
