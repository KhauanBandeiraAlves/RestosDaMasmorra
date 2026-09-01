using System;
using UnityEngine;

namespace RestosDaMasmorra.Characters.Combat
{
    public class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] float maxHealth = 10f;

        bool initialized;
        float current;

        public float MaxHealth => maxHealth;
        public float Current
        {
            get
            {
                EnsureInitialized();
                return current;
            }
            private set => current = value;
        }
        public bool IsAlive => Current > 0f;

        public event Action Died;
        public event Action<float> DamageTaken;

        void Awake() => EnsureInitialized();

        // Awake() only runs once a scene is actually in Play Mode; editor tooling that
        // reads/damages Health outside Play Mode (batch-mode screenshot tools) would
        // otherwise see Current stuck at its C# default (0) instead of full health.
        void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;
            current = maxHealth;
        }

        public void SetMaxHealth(float value)
        {
            maxHealth = Mathf.Max(1f, value);
            initialized = true;
            current = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            current = Mathf.Max(0f, Current - amount);
            DamageTaken?.Invoke(amount);

            if (current <= 0f) Died?.Invoke();
        }
    }
}
