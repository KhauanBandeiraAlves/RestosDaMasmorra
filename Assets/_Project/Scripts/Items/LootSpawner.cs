using UnityEngine;

namespace RestosDaMasmorra.Items
{
    // Instantiates a physical, collectible WorldItem for loot drops (enemy deaths,
    // broken-weapon chance after combat, etc.). Runtime-safe.
    public static class LootSpawner
    {
        public static WorldItem SpawnDrop(ItemDefinition item, Vector3 position, ItemOwnership ownership, Transform parent = null)
        {
            if (item == null) return null;

            GameObject visual = item.VisualPrefab != null
                ? Object.Instantiate(item.VisualPrefab, position, Quaternion.identity, parent)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);

            if (item.VisualPrefab == null)
            {
                visual.transform.position = position;
                visual.transform.localScale = Vector3.one * 0.3f;
                if (parent != null) visual.transform.SetParent(parent, true);
                Object.Destroy(visual.GetComponent<Collider>());
            }

            visual.name = "Loot_" + item.Id;

            SphereCollider collider = visual.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.6f;

            WorldItem worldItem = visual.AddComponent<WorldItem>();
            worldItem.EditorConfigure(item, ownership);

            return worldItem;
        }
    }
}
