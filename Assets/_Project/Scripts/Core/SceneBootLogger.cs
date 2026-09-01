using UnityEngine;

namespace RestosDaMasmorra.Core
{
    // Small diagnostic component: logs that a scene actually finished loading and that the
    // player/camera it expects are really there. Cheap enough to leave in permanently as a
    // sanity check (e.g. in PrototypeBase.unity) rather than being purely a one-off tool.
    public class SceneBootLogger : MonoBehaviour
    {
        [SerializeField] string sceneLabel = "SCENE";

        void Start()
        {
            Debug.Log($"{sceneLabel} LOADED");

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Debug.Log(player != null ? "PLAYER FOUND" : "PLAYER NOT FOUND");

            Camera cam = Camera.main;
            Debug.Log(cam != null ? "CAMERA FOUND" : "CAMERA NOT FOUND");
        }
    }
}
