using System.Collections.Generic;
using UnityEngine;

namespace RestosDaMasmorra.Dungeon
{
    public class DungeonBuildResult
    {
        public GameObject Root;
        public Vector3 EntranceWorldPosition;
        public Vector3 BossWorldPosition;
        public Dictionary<PlacedRoom, GameObject> Instances = new Dictionary<PlacedRoom, GameObject>();
    }

    // Turns a logical DungeonLayoutResult into actual scene GameObjects. Runtime-safe
    // (plain Object.Instantiate), so the same code path works in the Editor tool, in
    // PlayMode, and in a built player.
    public static class DungeonSceneBuilder
    {
        const float WallHeight = 4f;
        const float WallThickness = 1f;
        const float DoorGapWidth = 4f;

        public static DungeonBuildResult Build(DungeonLayoutResult layout, Transform parent = null)
        {
            var buildResult = new DungeonBuildResult();
            if (!layout.Success) return buildResult;

            GameObject root = new GameObject("GeneratedDungeon");
            if (parent != null) root.transform.SetParent(parent, false);
            buildResult.Root = root;

            foreach (PlacedRoom placedRoom in layout.Rooms)
            {
                GameObject instance = Object.Instantiate(placedRoom.Prefab, placedRoom.Position, Quaternion.Euler(0f, placedRoom.YawDegrees, 0f), root.transform);
                instance.name = placedRoom.Definition.RoomType + "_" + placedRoom.Position;
                buildResult.Instances[placedRoom] = instance;

                AddMeshColliders(instance);
                CapUnusedSockets(instance, placedRoom);
            }

            buildResult.EntranceWorldPosition = layout.Entrance.Position;
            buildResult.BossWorldPosition = layout.Boss.Position;
            return buildResult;
        }

        static void AddMeshColliders(GameObject root)
        {
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                if (mf.GetComponent<Collider>() != null) continue;
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }

        // Blocks movement through any socket that didn't end up connected to another room,
        // so the player can never walk out of the generated layout into empty space.
        static void CapUnusedSockets(GameObject instance, PlacedRoom placedRoom)
        {
            RoomSocket[] sockets = instance.GetComponentsInChildren<RoomSocket>(true);
            for (int i = 0; i < sockets.Length; i++)
            {
                if (placedRoom.ConnectedSocketIndices.Contains(i))
                {
                    sockets[i].ConnectedAtBuildTime = true;
                    continue;
                }

                GameObject cap = new GameObject("SocketCap");
                cap.transform.SetParent(sockets[i].transform, false);
                cap.transform.localPosition = Vector3.zero;
                BoxCollider capCollider = cap.AddComponent<BoxCollider>();
                capCollider.size = new Vector3(DoorGapWidth, WallHeight, WallThickness);
                capCollider.center = new Vector3(0f, WallHeight * 0.5f, 0f);
            }
        }
    }
}
