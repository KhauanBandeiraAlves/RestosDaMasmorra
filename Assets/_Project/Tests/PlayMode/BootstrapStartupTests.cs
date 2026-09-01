using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using RestosDaMasmorra.Economy;

namespace RestosDaMasmorra.Tests.PlayMode
{
    // Exercises the REAL scene flow (Bootstrap -> PrototypeBase) via SceneManager, with no
    // SceneLoadGate suppression, so it actually proves the shipped startup path works.
    public class BootstrapStartupTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Deliberately does NOT reload Bootstrap here: that would re-trigger
            // Bootstrap.Start() -> LoadScene(PrototypeBase), leaving PrototypeBase's KayKit
            // props (non-read/write meshes) active and polluting NavMesh bakes in whichever
            // PlayMode test runs next in this same Play session. Instead, swap to a plain
            // empty scene and unload PrototypeBase/GameSession directly.
            Scene empty = SceneManager.CreateScene("BootstrapStartupTests_Cleanup");
            SceneManager.SetActiveScene(empty);

            Scene prototypeBase = SceneManager.GetSceneByName("PrototypeBase");
            if (prototypeBase.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(prototypeBase);
            }

            if (GameSession.Instance != null)
            {
                Object.Destroy(GameSession.Instance.gameObject);
                yield return null;
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator Bootstrap_Loads_PrototypeBase_With_Player_And_Camera_And_SingleGameSession()
        {
            LogAssert.ignoreFailingMessages = false;

            AsyncOperation loadBootstrap = SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            while (!loadBootstrap.isDone) yield return null;

            // Bootstrap.Start() runs and immediately requests PrototypeBase; wait for that
            // scene to actually become active (LoadScene is synchronous internally, but the
            // active-scene switch still needs a frame to be observable here).
            float timeout = Time.realtimeSinceStartup + 20f;
            while (SceneManager.GetActiveScene().name != "PrototypeBase" && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.AreEqual("PrototypeBase", SceneManager.GetActiveScene().name,
                "Bootstrap did not load PrototypeBase within the timeout.");

            Assert.IsNotNull(GameSession.Instance, "GameSession was not created by Bootstrap.");

            GameSession[] sessions = Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None);
            Assert.AreEqual(1, sessions.Length, "Duplicate GameSession detected after Bootstrap flow.");

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Assert.IsNotNull(player, "No Player found in PrototypeBase after Bootstrap flow.");

            Assert.IsNotNull(Camera.main, "No main camera found in PrototypeBase after Bootstrap flow.");
        }
    }
}
