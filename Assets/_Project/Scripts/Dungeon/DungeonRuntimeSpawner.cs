using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using RestosDaMasmorra.Characters;
using RestosDaMasmorra.Core;
using RestosDaMasmorra.Enemies;
using RestosDaMasmorra.Items;

namespace RestosDaMasmorra.Dungeon
{
    // Placed in PrototypeDungeon.unity. Generates a procedural layout on Start, builds it
    // into the scene, bakes navigation, spawns the party + enemies, and positions the
    // player + return-to-base portal at the Entrance.
    public class DungeonRuntimeSpawner : MonoBehaviour
    {
        [SerializeField] DungeonDefinition definition;
        [SerializeField] int seed = 12345; // 0 = random every run
        [SerializeField] Transform playerTransform;
        [SerializeField] string returnSceneName = "PrototypeBase";

        [SerializeField] List<AdventurerDefinition> partyDefinitions = new List<AdventurerDefinition>();
        [SerializeField] EnemyDefinition enemyDefinition;
        [SerializeField] ItemDefinition brokenWeaponItem;

        public DungeonLayoutResult LastLayout { get; private set; }
        public DungeonBuildResult LastBuildResult { get; private set; }
        public List<AdventurerController> Party { get; private set; } = new List<AdventurerController>();
        public List<EnemyController> Enemies { get; private set; } = new List<EnemyController>();

        public void EditorConfigure(DungeonDefinition dungeonDefinition, int fixedSeed, Transform player, string returnScene)
        {
            definition = dungeonDefinition;
            seed = fixedSeed;
            playerTransform = player;
            returnSceneName = returnScene;
        }

        public void EditorConfigureContent(List<AdventurerDefinition> party, EnemyDefinition enemy, ItemDefinition brokenWeapon)
        {
            partyDefinitions = party;
            enemyDefinition = enemy;
            brokenWeaponItem = brokenWeapon;
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
            Vector3 spawnPos = LastBuildResult.EntranceWorldPosition + new Vector3(0f, 0.1f, -(entranceSize.y * 0.5f - 2.5f));

            if (playerTransform != null) playerTransform.position = spawnPos;

            GameObject portalGO = new GameObject("ReturnToBase");
            portalGO.transform.SetParent(transform, false);
            portalGO.transform.position = portalPos;
            BoxCollider portalCollider = portalGO.AddComponent<BoxCollider>();
            portalCollider.isTrigger = true;
            portalCollider.size = new Vector3(2f, 2f, 2f);
            ScenePortal portal = portalGO.AddComponent<ScenePortal>();
            portal.Configure(returnSceneName, "Voltar para a Base");

            BakeNavMesh();
            SpawnParty(spawnPos, actualSeed);
            SpawnEnemies(actualSeed);
        }

        void BakeNavMesh()
        {
            NavMeshSurface surface = gameObject.GetComponent<NavMeshSurface>();
            if (surface == null) surface = gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
        }

        void SpawnParty(Vector3 spawnPos, int actualSeed)
        {
            if (partyDefinitions == null || partyDefinitions.Count == 0) return;

            List<Vector3> route = new List<Vector3>();
            foreach (PlacedRoom room in LastLayout.MainPath) route.Add(room.Position);

            GameObject partyRoot = new GameObject("Party");
            partyRoot.transform.SetParent(transform, false);

            Party = PartySpawner.Spawn(partyDefinitions, spawnPos, route, brokenWeaponItem, actualSeed + 500, partyRoot.transform);
        }

        void SpawnEnemies(int actualSeed)
        {
            if (enemyDefinition == null) return;

            GameObject enemyRoot = new GameObject("Enemies");
            enemyRoot.transform.SetParent(transform, false);

            Enemies = EnemySpawner.SpawnForLayout(LastLayout, enemyDefinition, actualSeed + 900, enemyRoot.transform);
        }
    }
}
