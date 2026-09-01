using UnityEngine;

namespace RestosDaMasmorra.Characters
{
    [CreateAssetMenu(fileName = "NewAdventurer", menuName = "RestosDaMasmorra/Adventurer Definition")]
    public class AdventurerDefinition : ScriptableObject
    {
        [SerializeField] AdventurerType type;
        [SerializeField] string displayName = "Adventurer";
        [SerializeField, Min(1f)] float health = 20f;
        [SerializeField, Min(0f)] float moveSpeed = 3.5f;
        [SerializeField, Min(0f)] float attackDamage = 4f;
        [SerializeField, Min(0f)] float attackRange = 1.5f;
        [SerializeField, Min(0.05f)] float attackCooldown = 1.2f;
        [SerializeField] bool isRanged;
        [SerializeField] GameObject visualPrefab;

        public AdventurerType Type => type;
        public string DisplayName => displayName;
        public float Health => health;
        public float MoveSpeed => moveSpeed;
        public float AttackDamage => attackDamage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public bool IsRanged => isRanged;
        public GameObject VisualPrefab => visualPrefab;

        public void EditorConfigure(AdventurerType t, string name, float hp, float speed, float dmg, float range, float cooldown, bool ranged, GameObject visual)
        {
            type = t;
            displayName = name;
            health = hp;
            moveSpeed = speed;
            attackDamage = dmg;
            attackRange = range;
            attackCooldown = cooldown;
            isRanged = ranged;
            visualPrefab = visual;
        }
    }
}
