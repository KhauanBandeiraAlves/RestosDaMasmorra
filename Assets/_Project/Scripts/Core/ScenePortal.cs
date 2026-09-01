using UnityEngine;
using UnityEngine.SceneManagement;

namespace RestosDaMasmorra.Core
{
    // Generic scene-to-scene transition trigger, used for both the dungeon entrance
    // (Base -> Dungeon) and the return path (Dungeon -> Base) during the prototype flow.
    public class ScenePortal : MonoBehaviour, IInteractable
    {
        [SerializeField] string targetSceneName = "PrototypeBase";
        [SerializeField] string promptLabel = "Entrar";

        public string InteractionPrompt => $"E - {promptLabel}";
        public bool CanInteract => true;

        // Runtime-safe configuration (no UnityEditor dependency), used by code that spawns
        // portals procedurally, e.g. DungeonRuntimeSpawner.
        public void Configure(string sceneName, string label)
        {
            targetSceneName = sceneName;
            promptLabel = label;
        }

        public void Interact(GameObject interactor)
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
