using System;
using UnityEngine;

namespace RestosDaMasmorra.Player
{
    public class PlayerStamina : MonoBehaviour
    {
        [SerializeField, Min(0f)] float maxStamina = 100f;
        [SerializeField, Min(0f)] float drainPerSecond = 20f;
        [SerializeField, Min(0f)] float regenPerSecond = 15f;
        [SerializeField, Min(0f)] float regenDelaySeconds = 0.5f;

        float current;
        float timeSinceStoppedRunning;

        public float MaxStamina => maxStamina;
        public float Current => current;
        public bool CanRun => current > 0f;

        public event Action<float, float> StaminaChanged;

        void Awake()
        {
            current = maxStamina;
        }

        // isRunningInput = the player is holding the run key AND moving.
        public void Tick(float deltaTime, bool isRunningInput)
        {
            bool actuallyRunning = isRunningInput && CanRun;

            if (actuallyRunning)
            {
                current = Mathf.Max(0f, current - drainPerSecond * deltaTime);
                timeSinceStoppedRunning = 0f;
            }
            else
            {
                timeSinceStoppedRunning += deltaTime;
                if (timeSinceStoppedRunning >= regenDelaySeconds)
                {
                    current = Mathf.Min(maxStamina, current + regenPerSecond * deltaTime);
                }
            }

            StaminaChanged?.Invoke(current, maxStamina);
        }

        public bool IsCurrentlyRunning(bool isRunningInput) => isRunningInput && CanRun;
    }
}
