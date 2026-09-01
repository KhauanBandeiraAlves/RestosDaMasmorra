using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RestosDaMasmorra.Characters;
using RestosDaMasmorra.Dungeon;
using RestosDaMasmorra.Enemies;
using RestosDaMasmorra.Items;

namespace RestosDaMasmorra.EditorTools
{
    // One-off orchestration for Phase C: loot items, adventurer/enemy definitions, enemy
    // spawn config on the room prefabs, and rewiring PrototypeDungeon.unity with the party
    // and enemy spawners. Safe to re-run to regenerate everything from scratch.
    public static class PhaseCBuilder
    {
        const string ItemsFolder = "Assets/_Project/ScriptableObjects/Items/";
        const string CharactersFolder = "Assets/_Project/ScriptableObjects/Characters/";
        const string RoomsFolder = "Assets/_Project/Prefabs/Dungeon/";
        const string DungeonDefPath = "Assets/_Project/ScriptableObjects/Dungeon/PrototypeDungeonDefinition.asset";
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";

        const string AdvChar = "Assets/ThirdParty/KayKit/Adventurers/Characters/";
        const string SkeletonChar = "Assets/ThirdParty/KayKit/Skeletons/Characters/";
        const string ResourceBits = "Assets/ThirdParty/KayKit/ResourceBits/Models/";

        public static void BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Directory.CreateDirectory(CharactersFolder);

            // --- loot items ---
            ItemDefinition bone = CreateOrLoadItem("Bone", "Osso", ItemCategory.Resource, 1, 2, "Bone", null);
            ItemDefinition coin = CreateOrLoadItem("Coin", "Moeda", ItemCategory.Resource, 1, 1, "Gold", ResourceBits + "Gold_Nuggets.fbx");
            ItemDefinition scrap = CreateOrLoadItem("Scrap", "Sucata", ItemCategory.Scrap, 1, 1, "Metal", ResourceBits + "Parts_Pile_Small.fbx");
            ItemDefinition brokenSword = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemsFolder + "Broken_Sword.asset");

            var lootEntries = new List<LootDropEntry>
            {
                new LootDropEntry { item = bone, dropChance = 0.7f, minCount = 1, maxCount = 2 },
                new LootDropEntry { item = coin, dropChance = 0.6f, minCount = 1, maxCount = 3 },
                new LootDropEntry { item = scrap, dropChance = 0.4f, minCount = 1, maxCount = 1 },
            };
            LootDropTable lootTable = CreateOrLoadAsset<LootDropTable>(ItemsFolder + "SkeletonLoot.asset");
            lootTable.EditorConfigure(lootEntries);
            EditorUtility.SetDirty(lootTable);

            // --- adventurer definitions ---
            AdventurerDefinition knight = CreateOrLoadAsset<AdventurerDefinition>(CharactersFolder + "Adventurer_Knight.asset");
            knight.EditorConfigure(AdventurerType.Knight, "Knight", 30f, 3.2f, 6f, 1.6f, 1.0f, false,
                AssetDatabase.LoadAssetAtPath<GameObject>(AdvChar + "Knight.fbx"));
            EditorUtility.SetDirty(knight);

            AdventurerDefinition mage = CreateOrLoadAsset<AdventurerDefinition>(CharactersFolder + "Adventurer_Mage.asset");
            mage.EditorConfigure(AdventurerType.Mage, "Mage", 14f, 3.0f, 5f, 6f, 1.6f, true,
                AssetDatabase.LoadAssetAtPath<GameObject>(AdvChar + "Mage.fbx"));
            EditorUtility.SetDirty(mage);

            AdventurerDefinition archer = CreateOrLoadAsset<AdventurerDefinition>(CharactersFolder + "Adventurer_Archer.asset");
            archer.EditorConfigure(AdventurerType.Archer, "Archer", 18f, 3.4f, 4f, 7f, 1.1f, true,
                AssetDatabase.LoadAssetAtPath<GameObject>(AdvChar + "Ranger.fbx"));
            EditorUtility.SetDirty(archer);

            // --- enemy definition ---
            EnemyDefinition skeleton = CreateOrLoadAsset<EnemyDefinition>(CharactersFolder + "Enemy_Skeleton.asset");
            skeleton.EditorConfigure("Skeleton Warrior", 10f, 2.6f, 3f, 1.4f, 1.4f,
                AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonChar + "Skeleton_Warrior.fbx"), lootTable);
            EditorUtility.SetDirty(skeleton);

            AssetDatabase.SaveAssets();

            // --- enemy spawn config on room prefabs (data-driven, not by prefab name) ---
            ConfigureSpawn(RoomsFolder + "Room_Combat_Straight.prefab", true, 2, 4);
            ConfigureSpawn(RoomsFolder + "Room_Combat_Branch.prefab", true, 2, 4);
            ConfigureSpawn(RoomsFolder + "Room_Combat_Branch_West.prefab", true, 2, 4);
            ConfigureSpawn(RoomsFolder + "Room_Combat_Wide.prefab", true, 2, 4);
            ConfigureSpawn(RoomsFolder + "Room_Combat_Narrow.prefab", true, 2, 4);
            ConfigureSpawn(RoomsFolder + "Room_Boss.prefab", true, 5, 7);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RewirePrototypeDungeonScene(knight, mage, archer, skeleton, brokenSword);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PhaseCBuilder: BuildAll complete.");
        }

        static void ConfigureSpawn(string prefabPath, bool spawns, int min, int max)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"PhaseCBuilder: prefab not found at {prefabPath}");
                return;
            }
            RoomDefinition def = prefab.GetComponent<RoomDefinition>();
            def.EditorConfigureSpawn(spawns, min, max);
            EditorUtility.SetDirty(prefab);
        }

        static ItemDefinition CreateOrLoadItem(string id, string displayName, ItemCategory category, int slotSize, int baseValue, string materialType, string visualPath)
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

            if (visualPath != null)
            {
                GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
                item.EditorSetVisual(visual);
            }

            if (isNew)
            {
                Directory.CreateDirectory(ItemsFolder);
                AssetDatabase.CreateAsset(item, path);
            }
            else
            {
                EditorUtility.SetDirty(item);
            }

            return item;
        }

        static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        static void RewirePrototypeDungeonScene(AdventurerDefinition knight, AdventurerDefinition mage, AdventurerDefinition archer, EnemyDefinition skeleton, ItemDefinition brokenSword)
        {
            string scenePath = "Assets/_Project/Scenes/PrototypeDungeon.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            DungeonRuntimeSpawner spawner = Object.FindFirstObjectByType<DungeonRuntimeSpawner>();
            if (spawner == null)
            {
                Debug.LogError("PhaseCBuilder: no DungeonRuntimeSpawner found in PrototypeDungeon.unity");
                return;
            }

            spawner.EditorConfigureContent(new List<AdventurerDefinition> { knight, mage, archer }, skeleton, brokenSword);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
