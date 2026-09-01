using UnityEngine;

namespace RestosDaMasmorra.Dungeon
{
    // Lives on the root of a room prefab. Read directly off the prefab ASSET by the
    // generator (never instantiated during layout computation) so 1000-seed validation
    // runs are cheap.
    public class RoomDefinition : MonoBehaviour
    {
        [SerializeField] RoomType roomType;
        [SerializeField] string theme = "Dungeon";
        [SerializeField] float weight = 1f;
        [SerializeField] int minDepth;
        [SerializeField] int maxDepth = 99;
        [SerializeField] bool canRepeat = true;

        // Room footprint in meters, X = width (East-West), Y = depth (North-South).
        // Kept in multiples of the 4m KayKit module.
        [SerializeField] Vector2 size = new Vector2(12f, 12f);

        // Content spawn config, data-driven per room (not hardcoded by prefab name).
        [SerializeField] bool spawnsEnemies;
        [SerializeField] int minEnemies;
        [SerializeField] int maxEnemies;

        public RoomType RoomType => roomType;
        public string Theme => theme;
        public float Weight => weight;
        public int MinDepth => minDepth;
        public int MaxDepth => maxDepth;
        public bool CanRepeat => canRepeat;
        public Vector2 Size => size;
        public bool SpawnsEnemies => spawnsEnemies;
        public int MinEnemies => minEnemies;
        public int MaxEnemies => maxEnemies;

        public RoomSocket[] GetSockets() => GetComponentsInChildren<RoomSocket>(true);

        public void EditorConfigureSpawn(bool spawns, int min, int max)
        {
            spawnsEnemies = spawns;
            minEnemies = min;
            maxEnemies = max;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.6f);
            Vector3 center = transform.position + new Vector3(0f, 0.05f, 0f);
            Gizmos.DrawWireCube(center, new Vector3(size.x, 0.1f, size.y));
        }
    }
}
