using UnityEngine;
using RestosDaMasmorra.Core;

namespace RestosDaMasmorra.Dungeon
{
    // Placed in PrototypeDungeon.unity. Generates a procedural layout on Start, builds it
    // into the scene, and positions the player + return-to-base portal at the Entrance.
    public class DungeonRuntimeSpawner : MonoBehaviour
    {
        [SerializeField] DungeonDefinition definition;
        [SerializeField] int seed = 12345; // 0 = random every run
        [SerializeField] Transform playerTransform;
        [SerializeField] string returnSceneName = "PrototypeBase";

        public DungeonLayoutResult LastLayout { get; private set; }
        public DungeonBuildResult LastBuildResult { get; private set; }

        public void EditorConfigure(DungeonDefinition dungeonDefinition, int fixedSeed, Transform player, string returnScene)
        {
            definition = dungeonDefinition;
            seed = fixedSeed;
            playerTransform = player;
            returnSceneName = returnScene;
        }

        void Start()
        {
            GenerateAndBuild();
        }

        public void GenerateAndBuild()
        {
            int actualSeed = seed != 0 ? seed : System.Environment.TickCount;
            LastLayout = DungeonGenerator.Generate(definition, actualSeed);

            if (!LastLayout.Success)
            {
                Debug.LogError($"DungeonRuntimeSpawner: generation failed — {LastLayout.FailureReason}");
                return;
            }

            LastBuildResult = DungeonSceneBuilder.Build(LastLayout, transform);

            if (playerTransform == null)
            {
                GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null) playerTransform = playerGO.transform;
            }

            Vector2 entranceSize = LastLayout.Entrance.Definition.Size;
            Vector3 portalPos = LastBuildResult.EntranceWorldPosition + new Vector3(0f, 0.5f, -(entranceSize.y * 0.5f - 1.5f));

            if (playerTransform != null)
            {
                playerTransform.position = LastBuildResult.EntranceWorldPosition + new Vector3(0f, 0.1f, -(entranceSize.y * 0.5f - 2.5f));
            }

            GameObject portalGO = new GameObject("ReturnToBase");
            portalGO.transform.SetParent(transform, false);
            portalGO.transform.position = portalPos;
            BoxCollider portalCollider = portalGO.AddComponent<BoxCollider>();
            portalCollider.isTrigger = true;
            portalCollider.size = new Vector3(2f, 2f, 2f);
            ScenePortal portal = portalGO.AddComponent<ScenePortal>();
            portal.Configure(returnSceneName, "Voltar para a Base");
        }
    }
}
