using System;
using UnityEngine;

namespace RestosDaMasmorra.Characters.Combat
{
    public class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] float maxHealth = 10f;

        public float MaxHealth => maxHealth;
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;

        public event Action Died;
        public event Action<float> DamageTaken;

        void Awake()
        {
            Current = maxHealth;
        }

        public void SetMaxHealth(float value)
        {
            maxHealth = Mathf.Max(1f, value);
            Current = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            Current = Mathf.Max(0f, Current - amount);
            DamageTaken?.Invoke(amount);

            if (Current <= 0f) Died?.Invoke();
        }
    }
}
