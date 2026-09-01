using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RestosDaMasmorra.Dungeon;

namespace RestosDaMasmorra.EditorTools
{
    // Builds greybox room prefabs out of the KayKit Dungeon pack: floor tiles + perimeter
    // walls, with a doorway + RoomSocket punched in the middle of each requested side.
    // Room origin (0,0,0) is the room's footprint CENTER, matching PlacedRoom.Position.
    public static class RoomPrefabFactory
    {
        const string Dungeon = "Assets/ThirdParty/KayKit/Dungeon/Models/";
        const float Tile = 4f;
        const float WallThickness = 1f;

        public static GameObject BuildRoom(
            string savePath,
            RoomType roomType,
            Vector2 size,
            IEnumerable<SocketDirection> socketSides,
            float weight = 1f,
            int minDepth = 0,
            int maxDepth = 99,
            bool canRepeat = true)
        {
            int tilesX = Mathf.RoundToInt(size.x / Tile);
            int tilesZ = Mathf.RoundToInt(size.y / Tile);
            float halfW = size.x * 0.5f;
            float halfD = size.y * 0.5f;

            HashSet<SocketDirection> doors = new HashSet<SocketDirection>(socketSides);

            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(savePath));
            RoomDefinition def = root.AddComponent<RoomDefinition>();
            SetPrivate(def, "roomType", roomType);
            SetPrivate(def, "weight", weight);
            SetPrivate(def, "minDepth", minDepth);
            SetPrivate(def, "maxDepth", maxDepth);
            SetPrivate(def, "canRepeat", canRepeat);
            SetPrivate(def, "size", size);

            GameObject floorRoot = new GameObject("Floor");
            floorRoot.transform.SetParent(root.transform, false);
            for (int x = 0; x < tilesX; x++)
            {
                for (int z = 0; z < tilesZ; z++)
                {
                    Vector3 pos = new Vector3(-halfW + x * Tile, 0f, -halfD + z * Tile);
                    Spawn(Dungeon + "floor_tile_large.fbx", pos, 0f, floorRoot.transform);
                }
            }

            GameObject wallsRoot = new GameObject("Walls");
            wallsRoot.transform.SetParent(root.transform, false);

            // South wall (z = -halfD), running along X.
            BuildWallRun(wallsRoot.transform, tilesX, doors.Contains(SocketDirection.South),
                i => new Vector3(-halfW + i * Tile, 0f, -halfD), 0f,
                root, SocketDirection.South, halfW, halfD);

            // North wall (z = +halfD - thickness), running along X, facing the opposite way.
            BuildWallRun(wallsRoot.transform, tilesX, doors.Contains(SocketDirection.North),
                i => new Vector3(-halfW + i * Tile, 0f, halfD - WallThickness), 180f,
                root, SocketDirection.North, halfW, halfD);

            // West wall (x = -halfW), running along Z.
            BuildWallRun(wallsRoot.transform, tilesZ, doors.Contains(SocketDirection.West),
                i => new Vector3(-halfW, 0f, -halfD + i * Tile), 90f,
                root, SocketDirection.West, halfW, halfD);

            // East wall (x = +halfW - thickness), running along Z.
            BuildWallRun(wallsRoot.transform, tilesZ, doors.Contains(SocketDirection.East),
                i => new Vector3(halfW - WallThickness, 0f, -halfD + i * Tile), -90f,
                root, SocketDirection.East, halfW, halfD);

            Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, savePath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void BuildWallRun(Transform parent, int segmentCount, bool hasDoor, System.Func<int, Vector3> positionAt, float yaw, GameObject roomRoot, SocketDirection side, float halfW, float halfD)
        {
            int doorIndex = hasDoor ? segmentCount / 2 : -1;

            for (int i = 0; i < segmentCount; i++)
            {
                bool isDoor = i == doorIndex;
                string piece = isDoor ? "wall_doorway.fbx" : "wall.fbx";
                GameObject seg = Spawn(Dungeon + piece, positionAt(i), yaw, parent);

                if (isDoor)
                {
                    GameObject socketGO = new GameObject("Socket_" + side);
                    socketGO.transform.SetParent(roomRoot.transform, false);
                    Vector3 socketLocalPos = side switch
                    {
                        SocketDirection.South => new Vector3(0f, 0f, -halfD),
                        SocketDirection.North => new Vector3(0f, 0f, halfD),
                        SocketDirection.West => new Vector3(-halfW, 0f, 0f),
                        SocketDirection.East => new Vector3(halfW, 0f, 0f),
                        _ => Vector3.zero
                    };
                    socketGO.transform.localPosition = socketLocalPos;
                    RoomSocket socket = socketGO.AddComponent<RoomSocket>();
                    SetPrivate(socket, "direction", side);
                }
            }
        }

        static GameObject Spawn(string path, Vector3 localPos, float yaw, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"RoomPrefabFactory: missing asset at {path}");
                return new GameObject("MISSING");
            }
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.localPosition = localPos;
            inst.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return inst;
        }

        static void SetPrivate(Object target, string fieldName, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"RoomPrefabFactory: field '{fieldName}' not found on {target.GetType().Name}");
                return;
            }
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Enum: prop.enumValueIndex = (int)value; break;
                case SerializedPropertyType.Float: prop.floatValue = (float)value; break;
                case SerializedPropertyType.Integer: prop.intValue = (int)value; break;
                case SerializedPropertyType.Boolean: prop.boolValue = (bool)value; break;
                case SerializedPropertyType.Vector2: prop.vector2Value = (Vector2)value; break;
                case SerializedPropertyType.String: prop.stringValue = (string)value; break;
                default: Debug.LogError($"RoomPrefabFactory: unsupported field type for '{fieldName}'"); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
