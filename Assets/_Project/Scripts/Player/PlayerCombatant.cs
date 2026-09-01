using UnityEngine;
using RestosDaMasmorra.Characters.Combat;

namespace RestosDaMasmorra.Player
{
    // Makes the player a valid target in CombatantRegistry (Team.Party), so an enemy that
    // finds no adventurer nearby can naturally end up targeting the player instead — same
    // nearest-alive lookup, no special-cased fallback path. The player never attacks back;
    // this only makes them damageable.
    [RequireComponent(typeof(Health))]
    public class PlayerCombatant : MonoBehaviour, ICombatant
    {
        Health health;

        public Transform CombatTransform => transform;
        public Health CombatHealth => health;
        public Team CombatTeam => Team.Party;
        public bool IsAlive => health != null && health.IsAlive;

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
        }

        void OnEnable()
        {
            if (health == null) health = GetComponent<Health>();
            CombatantRegistry.Register(this);
        }

        void OnDisable() => CombatantRegistry.Unregister(this);
    }
}
