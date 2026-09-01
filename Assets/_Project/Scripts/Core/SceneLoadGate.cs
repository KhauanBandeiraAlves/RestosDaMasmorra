namespace RestosDaMasmorra.Core
{
    // Lets automated tests exercise ExtractionPoint/PlayerLifeController's actual logic
    // (storage transfer, life-state transition) without triggering a real
    // SceneManager.LoadScene — loading a full scene (which itself spawns a procedural
    // dungeon + bakes NavMesh) from inside a running PlayMode test is what caused the
    // Editor to destabilize/hang. Defaults to allowing real loads; only tests flip it.
    public static class SceneLoadGate
    {
        public static bool SuppressForTests;
    }
}
