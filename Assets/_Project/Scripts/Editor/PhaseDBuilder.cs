using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RestosDaMasmorra.Core;
using RestosDaMasmorra.Economy;
using RestosDaMasmorra.Items;
using RestosDaMasmorra.UI;

namespace RestosDaMasmorra.EditorTools
{
    // One-off orchestration for Phase D: dismantling items/recipes, and rewiring
    // PrototypeBase.unity with a real Storage + Dismantling Bench. PrototypeDungeon.unity
    // is NOT rebuilt here — DungeonRuntimeSpawner already spawns its own ExtractionPoint
    // at runtime, and this must never touch the procedural pipeline from Phase B/C.
    public static class PhaseDBuilder
    {
        const string Dungeon = "Assets/ThirdParty/KayKit/Dungeon/Models/";
        const string ItemsFolder = "Assets/_Project/ScriptableObjects/Items/";
        const string EconomyFolder = "Assets/_Project/ScriptableObjects/Crafting/";
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";

        public static void BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Directory.CreateDirectory(EconomyFolder);

            // Regenerate the Player prefab so it picks up Health/PlayerCombatant/
            // PlayerLifeController — safe, this method never touches scene files.
            GameObject playerPrefab = InvokeBuildPlayerPrefab();

            ItemDefinition brokenSword = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemsFolder + "Broken_Sword.asset");
            ItemDefinition bone = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemsFolder + "Bone.asset");
            ItemDefinition scrap = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemsFolder + "Scrap.asset");

            ItemDefinition metal = CreateOrLoadItem("Metal", "Metal", ItemCategory.Resource, 1, 2, "Metal");
            ItemDefinition leather = CreateOrLoadItem("Leather", "Couro", ItemCategory.Resource, 1, 2, "Leather");
            ItemDefinition boneResource = CreateOrLoadItem("Bone_Resource", "Recurso Ósseo", ItemCategory.Resource, 1, 1, "Bone");
            ItemDefinition brokenArmor = CreateOrLoadItem("Broken_Armor", "Armadura Quebrada", ItemCategory.Scrap, 2, 6, "Metal");
            AssetDatabase.SaveAssets();

            DismantlingRecipe swordRecipe = CreateOrLoadRecipe("Recipe_BrokenSword", brokenSword, 1,
                new List<DismantlingOutput> { new DismantlingOutput { item = metal, quantity = 3 } });

            DismantlingRecipe armorRecipe = CreateOrLoadRecipe("Recipe_BrokenArmor", brokenArmor, 1,
                new List<DismantlingOutput> { new DismantlingOutput { item = metal, quantity = 2 }, new DismantlingOutput { item = leather, quantity = 1 } });

            DismantlingRecipe boneRecipe = CreateOrLoadRecipe("Recipe_Bone", bone, 1,
                new List<DismantlingOutput> { new DismantlingOutput { item = boneResource, quantity = 1 } });

            DismantlingRecipe scrapRecipe = CreateOrLoadRecipe("Recipe_Scrap", scrap, 1,
                new List<DismantlingOutput> { new DismantlingOutput { item = metal, quantity = 1 } });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            List<DismantlingRecipe> recipes = new List<DismantlingRecipe> { swordRecipe, armorRecipe, boneRecipe, scrapRecipe };
            RebuildPrototypeBase(playerPrefab, recipes);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PhaseDBuilder: BuildAll complete.");
        }

        static GameObject InvokeBuildPlayerPrefab()
        {
            var method = typeof(PrototypeSceneBuilder).GetMethod("BuildPlayerPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (GameObject)method.Invoke(null, null);
        }

        static ItemDefinition CreateOrLoadItem(string id, string displayName, ItemCategory category, int slotSize, int baseValue, string materialType)
        {
            string path = ItemsFolder + id + ".asset";
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            bool isNew = item == null;
            if (isNew) item = ScriptableObject.CreateInstance<ItemDefinition>();

            SerializedObject so = new SerializedObject(item);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("slotSize").intValue = slotSize;
            so.FindProperty("baseValue").intValue = baseValue;
            so.FindProperty("materialType").stringValue = materialType;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (isNew)
            {
                Directory.CreateDirectory(ItemsFolder);
                AssetDatabase.CreateAsset(item, path);
            }
            else EditorUtility.SetDirty(item);

            return item;
        }

        static DismantlingRecipe CreateOrLoadRecipe(string name, ItemDefinition input, int inputQty, List<DismantlingOutput> outputs)
        {
            string path = EconomyFolder + name + ".asset";
            DismantlingRecipe recipe = AssetDatabase.LoadAssetAtPath<DismantlingRecipe>(path);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<DismantlingRecipe>();
                AssetDatabase.CreateAsset(recipe, path);
            }
            recipe.EditorConfigure(input, inputQty, outputs);
            EditorUtility.SetDirty(recipe);
            return recipe;
        }

        static void RebuildPrototypeBase(GameObject playerPrefab, List<DismantlingRecipe> recipes)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(24f, 1f, 24f);

            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.transform.position = new Vector3(0f, 0.1f, -4f);

            // Dungeon entrance portal.
            GameObject portalGO = new GameObject("DungeonEntrance");
            portalGO.transform.position = new Vector3(0f, 0.5f, 4f);
            BoxCollider portalCollider = portalGO.AddComponent<BoxCollider>();
            portalCollider.isTrigger = true;
            portalCollider.size = new Vector3(2f, 2f, 2f);
            ScenePortal portal = portalGO.AddComponent<ScenePortal>();
            portal.Configure("PrototypeDungeon", "Entrar na Dungeon");
            SpawnVisual(Dungeon + "wall_doorway.fbx", portalGO.transform, Vector3.zero, 0f);

            SetupLightingAndCameraLocal(playerInstance.transform, out Canvas canvas);
            PrototypeSceneBuilder.WirePlayerHud(canvas, playerInstance);

            // Storage.
            GameObject storageGO = SpawnVisual(Dungeon + "chest_gold.fbx", null, new Vector3(-5f, 0f, -1f), 0f);
            storageGO.name = "Storage";
            SphereCollider storageCollider = storageGO.AddComponent<SphereCollider>();
            storageCollider.isTrigger = true;
            storageCollider.radius = 1.2f;
            StorageUI storageUI = BuildStorageUI(canvas);
            StorageInteractable storageInteractable = storageGO.AddComponent<StorageInteractable>();
            storageInteractable.Configure(storageUI);

            // Dismantling bench.
            GameObject benchGO = SpawnVisual(Dungeon + "table_medium.fbx", null, new Vector3(5f, 0f, -1f), 0f);
            benchGO.name = "DismantlingBench";
            SphereCollider benchCollider = benchGO.AddComponent<SphereCollider>();
            benchCollider.isTrigger = true;
            benchCollider.radius = 1.2f;
            DismantlingUI dismantlingUI = BuildDismantlingUI(canvas);
            DismantlingBench bench = benchGO.AddComponent<DismantlingBench>();
            bench.Configure(recipes, dismantlingUI);

            Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/PrototypeBase.unity");
        }

        static GameObject SpawnVisual(string path, Transform parent, Vector3 localOrWorldPos, float yaw)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"PhaseDBuilder: missing asset at {path}");
                return new GameObject("MISSING_" + Path.GetFileName(path));
            }
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (parent != null) inst.transform.localPosition = localOrWorldPos;
            else inst.transform.position = localOrWorldPos;
            inst.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return inst;
        }

        static void SetupLightingAndCameraLocal(Transform playerTransform, out Canvas canvas)
        {
            PrototypeSceneBuilder.SetupLightingAndCamera(playerTransform, out canvas);
        }

        static StorageUI BuildStorageUI(Canvas canvas)
        {
            GameObject panelGO = new GameObject("StoragePanel");
            panelGO.transform.SetParent(canvas.transform, false);
            RectTransform rect = panelGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(420, 320);
            Image bg = panelGO.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            Text text = CreatePanelText(panelGO.transform);

            GameObject uiGO = new GameObject("StorageUI");
            uiGO.transform.SetParent(canvas.transform, false);
            StorageUI ui = uiGO.AddComponent<StorageUI>();
            ui.Configure(panelGO, text);
            return ui;
        }

        static DismantlingUI BuildDismantlingUI(Canvas canvas)
        {
            GameObject panelGO = new GameObject("DismantlingPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            RectTransform rect = panelGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(420, 320);
            Image bg = panelGO.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            Text text = CreatePanelText(panelGO.transform);

            GameObject uiGO = new GameObject("DismantlingUI");
            uiGO.transform.SetParent(canvas.transform, false);
            DismantlingUI ui = uiGO.AddComponent<DismantlingUI>();
            ui.Configure(panelGO, text);
            return ui;
        }

        static Text CreatePanelText(Transform parent)
        {
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(parent, false);
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16, 16);
            textRect.offsetMax = new Vector2(-16, -16);

            Text text = textGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
