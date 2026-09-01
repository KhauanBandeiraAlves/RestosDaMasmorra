using System.Collections.Generic;
using UnityEngine;

namespace RestosDaMasmorra.Dungeon
{
    [CreateAssetMenu(fileName = "NewDungeonDefinition", menuName = "RestosDaMasmorra/Dungeon Definition")]
    public class DungeonDefinition : ScriptableObject
    {
        [SerializeField] string dungeonId = "dungeon_id";
        [SerializeField] int minRooms = 6;
        [SerializeField] int maxRooms = 10;
        [SerializeField] int maxBranches = 2;
        [SerializeField] string theme = "Dungeon";
        [SerializeField] GameObject entrancePrefab;
        [SerializeField] GameObject bossPrefab;
        [SerializeField] List<GameObject> roomPool = new List<GameObject>();

        public string DungeonId => dungeonId;
        public int MinRooms => minRooms;
        public int MaxRooms => maxRooms;
        public int MaxBranches => maxBranches;
        public string Theme => theme;
        public GameObject EntrancePrefab => entrancePrefab;
        public GameObject BossPrefab => bossPrefab;
        public IReadOnlyList<GameObject> RoomPool => roomPool;

        // Editor-tooling convenience (RoomPrefabFactory / DungeonPrototypeBuilder). Not
        // meant to be called during actual gameplay.
        public void EditorConfigure(string id, int min, int max, int branches, string dungeonTheme,
            GameObject entrance, GameObject boss, List<GameObject> pool)
        {
            dungeonId = id;
            minRooms = min;
            maxRooms = max;
            maxBranches = branches;
            theme = dungeonTheme;
            entrancePrefab = entrance;
            bossPrefab = boss;
            roomPool = pool;
        }
    }
}
