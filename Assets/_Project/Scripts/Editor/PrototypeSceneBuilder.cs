using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RestosDaMasmorra.Core;
using RestosDaMasmorra.Items;
using RestosDaMasmorra.Player;
using RestosDaMasmorra.UI;

namespace RestosDaMasmorra.EditorTools
{
    // One-off tool that assembles the Phase A prototype assets: sample ItemDefinitions,
    // the Player prefab, and the PrototypeBase / PrototypeDungeon scenes.
    // Safe to delete once those assets exist; re-run to regenerate them from scratch.
    public static class PrototypeSceneBuilder
    {
        const string Dungeon = "Assets/ThirdParty/KayKit/Dungeon/Models/";
        const string AdvChar = "Assets/ThirdParty/KayKit/Adventurers/Characters/";
        const string ResourceBits = "Assets/ThirdParty/KayKit/ResourceBits/Models/";
        const string WeaponsBits = "Assets/ThirdParty/KayKit/FantasyWeaponsBits/Models/";

        const string ItemsFolder = "Assets/_Project/ScriptableObjects/Items/";
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";

        public static void BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            ItemDefinition brokenSword = CreateOrLoadItem("Broken_Sword", "Espada Quebrada", ItemCategory.Scrap, 2, 5, "Metal");
            ItemDefinition goldNugget = CreateOrLoadItem("Gold_Nugget", "Pepita de Ouro", ItemCategory.Resource, 1, 10, "Gold");
            ItemDefinition ironBar = CreateOrLoadItem("Iron_Bar", "Barra de Ferro", ItemCategory.Resource, 1, 6, "Iron");
            AssetDatabase.SaveAssets();

            GameObject playerPrefab = BuildPlayerPrefab();

            BuildPrototypeBase(playerPrefab);
            BuildPrototypeDungeon(playerPrefab, brokenSword, goldNugget, ironBar);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PrototypeSceneBuilder: BuildAll complete.");
        }

        static ItemDefinition CreateOrLoadItem(string id, string displayName, ItemCategory category, int slotSize, int baseValue, string materialType)
        {
            string path = ItemsFolder + id + ".asset";
            ItemDefinition existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null) return existing;

            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            SerializedObject so = new SerializedObject(item);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("slotSize").intValue = slotSize;
            so.FindProperty("baseValue").intValue = baseValue;
            so.FindProperty("materialType").stringValue = materialType;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(ItemsFolder);
            AssetDatabase.CreateAsset(item, path);
            return item;
        }

        static GameObject BuildPlayerPrefab()
        {
            GameObject root = new GameObject("Player");
            root.tag = "Player";

            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 2.6f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 1.3f, 0f);

            root.AddComponent<PlayerStamina>();
            root.AddComponent<PlayerMovement>();
            root.AddComponent<PlayerInteraction>();
            root.AddComponent<PlayerInventory>();

            GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AdvChar + "Knight.fbx");
            if (visualPrefab != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, root.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
            }

            Directory.CreateDirectory("Assets/_Project/Prefabs/Characters");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void BuildPrototypeBase(GameObject playerPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);

            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.transform.position = new Vector3(0f, 0.1f, -4f);

            GameObject portalGO = new GameObject("DungeonEntrance");
            portalGO.transform.position = new Vector3(0f, 0.5f, 4f);
            BoxCollider portalCollider = portalGO.AddComponent<BoxCollider>();
            portalCollider.isTrigger = true;
            portalCollider.size = new Vector3(2f, 2f, 2f);
            ScenePortal portal = portalGO.AddComponent<ScenePortal>();
            SetPrivateField(portal, "targetSceneName", "PrototypeDungeon");
            SetPrivateField(portal, "promptLabel", "Entrar na Dungeon");

            GameObject markerVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerVisual.name = "PortalMarker";
            markerVisual.transform.SetParent(portalGO.transform);
            markerVisual.transform.localPosition = Vector3.zero;
            markerVisual.transform.localScale = new Vector3(1f, 0.1f, 1f);
            Object.DestroyImmediate(markerVisual.GetComponent<Collider>());

            SetupLightingAndCamera(playerInstance.transform, out Canvas canvas);
            WirePlayerHud(canvas, playerInstance);

            Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/PrototypeBase.unity");
        }

        static void BuildPrototypeDungeon(GameObject playerPrefab, ItemDefinition brokenSword, ItemDefinition goldNugget, ItemDefinition ironBar)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject roomRoot = new GameObject("Room");

            GameObject floorProbe = Spawn(Dungeon + "floor_tile_large.fbx", Vector3.zero);
            float tileX = WorldBounds(floorProbe).size.x;
            float tileZ = WorldBounds(floorProbe).size.z;
            Object.DestroyImmediate(floorProbe);

            GameObject wallProbe = Spawn(Dungeon + "wall.fbx", Vector3.zero);
            Bounds wallBounds = WorldBounds(wallProbe);
            float wallHeight = wallBounds.size.y;
            float wallThickness = Mathf.Min(wallBounds.size.x, wallBounds.size.z);
            Object.DestroyImmediate(wallProbe);

            const int tilesX = 4;
            const int tilesZ = 4;

            for (int x = 0; x < tilesX; x++)
            {
                for (int z = 0; z < tilesZ; z++)
                {
                    GameObject f = Spawn(Dungeon + "floor_tile_large.fbx", new Vector3(x * tileX, 0f, z * tileZ));
                    AddMeshColliders(f);
                    f.transform.SetParent(roomRoot.transform);
                }
            }

            Vector3 roomMin = new Vector3(-tileX * 0.5f, 0f, -tileZ * 0.5f);
            Vector3 roomMax = new Vector3((tilesX - 0.5f) * tileX, wallHeight, (tilesZ - 0.5f) * tileZ);
            Vector3 roomCenter = (roomMin + roomMax) * 0.5f;
            roomCenter.y = 0f;

            for (int x = 0; x < tilesX; x++)
            {
                bool isDoorGap = x == tilesX / 2;
                string piece = x == tilesX / 2 - 1 ? "wall_doorway.fbx" : "wall.fbx";
                if (isDoorGap) continue;
                GameObject w = Spawn(Dungeon + piece, new Vector3(x * tileX, 0f, roomMin.z));
                AddMeshColliders(w);
                w.transform.SetParent(roomRoot.transform);
            }
            for (int x = 0; x < tilesX; x++)
            {
                GameObject w = Spawn(Dungeon + "wall.fbx", new Vector3(x * tileX, 0f, roomMax.z - wallThickness));
                w.transform.rotation = Quaternion.Euler(0, 180, 0);
                AddMeshColliders(w);
                w.transform.SetParent(roomRoot.transform);
            }
            for (int z = 0; z < tilesZ; z++)
            {
                GameObject wl = Spawn(Dungeon + "wall.fbx", new Vector3(roomMin.x, 0f, z * tileZ));
                wl.transform.rotation = Quaternion.Euler(0, 90, 0);
                AddMeshColliders(wl);
                wl.transform.SetParent(roomRoot.transform);

                GameObject wr = Spawn(Dungeon + "wall.fbx", new Vector3(roomMax.x - wallThickness, 0f, z * tileZ));
                wr.transform.rotation = Quaternion.Euler(0, -90, 0);
                AddMeshColliders(wr);
                wr.transform.SetParent(roomRoot.transform);
            }

            Spawn(Dungeon + "chest.fbx", roomCenter + new Vector3(tileX * 0.8f, 0f, tileZ * 0.8f)).transform.SetParent(roomRoot.transform);

            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.transform.position = new Vector3(roomMin.x + tileX * 0.5f + (tilesX / 2 - 1) * tileX, 0.1f, roomMin.z - 1f);

            GameObject exitPortalGO = new GameObject("ReturnToBase");
            exitPortalGO.transform.position = new Vector3(playerInstance.transform.position.x, 0.5f, roomMin.z - 1.5f);
            BoxCollider exitCollider = exitPortalGO.AddComponent<BoxCollider>();
            exitCollider.isTrigger = true;
            exitCollider.size = new Vector3(2f, 2f, 2f);
            ScenePortal exitPortal = exitPortalGO.AddComponent<ScenePortal>();
            SetPrivateField(exitPortal, "targetSceneName", "PrototypeBase");
            SetPrivateField(exitPortal, "promptLabel", "Voltar para a Base");

            SpawnWorldItem(brokenSword, roomCenter + new Vector3(-1.4f, 0f, 1.0f), roomRoot.transform);
            SpawnWorldItem(goldNugget, roomCenter + new Vector3(1.2f, 0f, -1.1f), roomRoot.transform);
            SpawnWorldItem(ironBar, roomCenter + new Vector3(0.3f, 0f, 1.6f), roomRoot.transform);

            SetupLightingAndCamera(playerInstance.transform, out Canvas canvas);
            WirePlayerHud(canvas, playerInstance);

            Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/PrototypeDungeon.unity");
        }

        static void SpawnWorldItem(ItemDefinition definition, Vector3 position, Transform parent)
        {
            string modelPath = ResourceBits + "Gold_Nuggets.fbx";
            if (definition != null && definition.MaterialType == "Iron") modelPath = ResourceBits + "Iron_Bar.fbx";
            else if (definition != null && definition.Category == ItemCategory.Scrap) modelPath = WeaponsBits + "Sword_1handed.fbx";

            GameObject visual = Spawn(modelPath, position);
            visual.name = "WorldItem_" + (definition != null ? definition.Id : "Unknown");
            visual.transform.SetParent(parent);

            SphereCollider collider = visual.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.6f;

            WorldItem worldItem = visual.AddComponent<WorldItem>();
            SetPrivateField(worldItem, "definition", definition);
            SetPrivateField(worldItem, "ownership", ItemOwnership.Discarded);
        }

        internal static void SetupLightingAndCamera(Transform playerTransform, out Canvas canvas)
        {
            GameObject dirLightGO = new GameObject("Directional Light");
            Light dirLight = dirLightGO.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1f, 0.96f, 0.88f);
            dirLight.intensity = 1.1f;
            dirLightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.33f, 0.38f);

            GameObject camGO = new GameObject("Isometric Camera");
            camGO.tag = "MainCamera";
            Camera cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            camGO.transform.rotation = Quaternion.Euler(32f, 45f, 0f);
            IsoCameraFollow follow = camGO.AddComponent<IsoCameraFollow>();
            follow.SetTarget(playerTransform);
            camGO.transform.position = playerTransform.position + new Vector3(-6.21f, 12.79f, -6.21f);

            camGO.AddComponent<AudioListener>();

            GameObject canvasGO = new GameObject("HUD Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        internal static void WirePlayerHud(Canvas canvas, GameObject playerInstance)
        {
            GameObject hudGO = new GameObject("PlayerHUD");
            hudGO.transform.SetParent(canvas.transform, false);
            PlayerHUD hud = hudGO.AddComponent<PlayerHUD>();

            Text staminaText = CreateHudText(canvas.transform, "StaminaText", new Vector2(-260, -20));
            Text backpackText = CreateHudText(canvas.transform, "BackpackText", new Vector2(-260, -45));
            Text interactionText = CreateHudText(canvas.transform, "InteractionText", new Vector2(0, 60), TextAnchor.LowerCenter);
            Text suspicionText = CreateHudText(canvas.transform, "SuspicionText", new Vector2(-260, -70));

            SetPrivateField(hud, "stamina", playerInstance.GetComponent<PlayerStamina>());
            SetPrivateField(hud, "inventory", playerInstance.GetComponent<PlayerInventory>());
            SetPrivateField(hud, "interaction", playerInstance.GetComponent<PlayerInteraction>());
            SetPrivateField(hud, "staminaText", staminaText);
            SetPrivateField(hud, "backpackText", backpackText);
            SetPrivateField(hud, "interactionText", interactionText);
            SetPrivateField(hud, "suspicionText", suspicionText);
        }

        static Text CreateHudText(Transform parent, string name, Vector2 anchoredPos, TextAnchor anchor = TextAnchor.UpperRight)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor == TextAnchor.LowerCenter ? new Vector2(0.5f, 0f) : new Vector2(1f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = rect.anchorMin;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(400, 30);

            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        static void AddMeshColliders(GameObject root)
        {
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }

        static GameObject Spawn(string path, Vector3 pos)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"PrototypeSceneBuilder: missing asset at {path}");
                return new GameObject("MISSING_" + Path.GetFileName(path));
            }
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.position = pos;
            return inst;
        }

        static Bounds WorldBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        internal static void SetPrivateField(Object target, string fieldName, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"PrototypeSceneBuilder: field '{fieldName}' not found on {target.GetType().Name}");
                return;
            }

            switch (prop.propertyType)
            {
                case SerializedPropertyType.String:
                    prop.stringValue = (string)value;
                    break;
                case SerializedPropertyType.ObjectReference:
                    prop.objectReferenceValue = (Object)value;
                    break;
                case SerializedPropertyType.Enum:
                    prop.enumValueIndex = (int)value;
                    break;
                default:
                    Debug.LogError($"PrototypeSceneBuilder: unsupported field type for '{fieldName}'");
                    break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
