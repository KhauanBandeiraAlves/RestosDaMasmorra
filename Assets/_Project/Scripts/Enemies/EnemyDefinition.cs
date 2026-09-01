using UnityEngine;
using RestosDaMasmorra.Items;

namespace RestosDaMasmorra.Enemies
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "RestosDaMasmorra/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField] string displayName = "Enemy";
        [SerializeField, Min(1f)] float health = 12f;
        [SerializeField, Min(0f)] float moveSpeed = 3f;
        [SerializeField, Min(0f)] float attackDamage = 3f;
        [SerializeField, Min(0f)] float attackRange = 1.4f;
        [SerializeField, Min(0.05f)] float attackCooldown = 1.5f;
        [SerializeField] GameObject visualPrefab;
        [SerializeField] LootDropTable lootTable;

        public string DisplayName => displayName;
        public float Health => health;
        public float MoveSpeed => moveSpeed;
        public float AttackDamage => attackDamage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public GameObject VisualPrefab => visualPrefab;
        public LootDropTable LootTable => lootTable;

        public void EditorConfigure(string name, float hp, float speed, float dmg, float range, float cooldown, GameObject visual, LootDropTable loot)
        {
            displayName = name;
            health = hp;
            moveSpeed = speed;
            attackDamage = dmg;
            attackRange = range;
            attackCooldown = cooldown;
            visualPrefab = visual;
            lootTable = loot;
        }
    }
}
