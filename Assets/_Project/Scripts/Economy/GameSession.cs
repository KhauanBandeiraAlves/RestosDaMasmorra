using UnityEngine;

namespace RestosDaMasmorra.Economy
{
    // Session-lifetime state: survives scene loads (Base <-> Dungeon), lives only in
    // memory (no disk save yet). A plain DontDestroyOnLoad singleton for now — simple and
    // not tangled up with anything that would make it awkward to make host-authoritative
    // later, since it holds no per-connection state.
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [SerializeField, Range(0f, 1f)] float lossPercentOnSoloDefeat = 0.3f;

        public SharedStorage Storage { get; private set; }
        public float LossPercentOnSoloDefeat => lossPercentOnSoloDefeat;

        // Temporary, per-run metadata (cleared/reset each time a new dungeon starts).
        public int CurrentRunSeed { get; private set; }
        public int CurrentSuspicion { get; private set; }

        void Awake() => Initialize();

        // Awake() only runs once a scene is actually in Play Mode; Editor tooling that adds
        // this component outside Play Mode (batch-mode scene builders/screenshot tools)
        // must call this explicitly right after construction — same idempotent pattern as
        // AdventurerController/EnemyController's EnsureRefs().
        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                return;
            }

            if (Instance == this && Storage != null) return; // already initialized

            Instance = this;
            if (Storage == null) Storage = new SharedStorage();
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        public void BeginRun(int seed)
        {
            CurrentRunSeed = seed;
        }

        public void AddSuspicion(int amount)
        {
            if (amount <= 0) return;
            CurrentSuspicion += amount;
        }
    }
}
