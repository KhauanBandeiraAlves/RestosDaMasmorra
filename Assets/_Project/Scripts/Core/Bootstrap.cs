using UnityEngine;
using UnityEngine.SceneManagement;
using RestosDaMasmorra.Economy;

namespace RestosDaMasmorra.Core
{
    // Entry point scene. Creates the session-lifetime GameSession (shared storage, etc.)
    // and kicks off the minimal flow Bootstrap -> PrototypeBase. Save/multiplayer
    // bootstrap responsibilities will be layered on here later.
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] string firstSceneName = "PrototypeBase";

        void Start()
        {
            if (GameSession.Instance == null)
            {
                GameObject sessionGO = new GameObject("GameSession");
                sessionGO.AddComponent<GameSession>().Initialize();
            }

            SceneManager.LoadScene(firstSceneName);
        }
    }
}
