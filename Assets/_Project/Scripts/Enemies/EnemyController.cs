using UnityEngine;
using UnityEngine.AI;
using RestosDaMasmorra.Characters.Combat;
using RestosDaMasmorra.Items;

namespace RestosDaMasmorra.Enemies
{
    // Targets the nearest living Team.Party combatant. The player is registered as a
    // Team.Party combatant too (see PlayerCombatant), so this naturally prioritizes
    // whichever is closer — in practice the adventurers, since they lead — and only ends
    // up on the player when no adventurer is nearby, without a separate hardcoded
    // "player fallback" path.
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class EnemyController : MonoBehaviour, ICombatant
    {
        [SerializeField] float detectionRadius = 7f;

        NavMeshAgent agent;
        Health health;
        EnemyDefinition definition;
        ICombatant target;
        float attackTimer;
        System.Random rng;
        bool lootDropped;

        public Transform CombatTransform => transform;
        public Health CombatHealth => health;
        public Team CombatTeam => Team.Enemy;
        public bool IsAlive => health.IsAlive;
        public EnemyDefinition Definition => definition;

        void EnsureRefs()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (health == null) health = GetComponent<Health>();
        }

        void Awake() => EnsureRefs();

        void OnEnable() => CombatantRegistry.Register(this);
        void OnDisable() => CombatantRegistry.Unregister(this);

        public void Initialize(EnemyDefinition def, int seed)
        {
            EnsureRefs();
            definition = def;
            rng = new System.Random(seed);

            health.SetMaxHealth(def.Health);
            agent.speed = def.MoveSpeed;

            health.Died += HandleDeath;
        }

        void Update()
        {
            if (!health.IsAlive) return;

            if (target == null || !target.IsAlive)
            {
                target = CombatantRegistry.FindNearestAlive(transform.position, detectionRadius, Team.Party);
            }

            if (target != null)
            {
                Fight();
                return;
            }

            agent.isStopped = true;
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

            if (!target.IsAlive) target = null;
        }

        void HandleDeath()
        {
            agent.isStopped = true;
            enabled = false;

            if (!lootDropped)
            {
                lootDropped = true;
                DropLoot();
            }
        }

        void DropLoot()
        {
            if (definition == null || definition.LootTable == null || rng == null) return;

            foreach ((ItemDefinition item, int count) drop in definition.LootTable.Roll(rng))
            {
                for (int i = 0; i < drop.count; i++)
                {
                    Vector3 pos = transform.position + new Vector3((float)rng.NextDouble() - 0.5f, 0f, (float)rng.NextDouble() - 0.5f);
                    LootSpawner.SpawnDrop(drop.item, pos, ItemOwnership.Discarded);
                }
            }
        }
    }
}
