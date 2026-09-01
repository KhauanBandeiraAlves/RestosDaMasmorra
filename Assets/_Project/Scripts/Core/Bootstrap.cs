using UnityEngine;
using UnityEngine.SceneManagement;

namespace RestosDaMasmorra.Core
{
    // Entry point scene. For now it only kicks off the minimal flow
    // Bootstrap -> PrototypeBase. Save/init/multiplayer bootstrap responsibilities
    // will be layered on here later.
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] string firstSceneName = "PrototypeBase";

        void Start()
        {
            SceneManager.LoadScene(firstSceneName);
        }
    }
}
