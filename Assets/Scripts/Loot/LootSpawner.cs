using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

public class LootSpawner : MonoBehaviour
{
    [Header("Ссылки")]
    public SplineContainer roadSpline;
    public ItemData[] lootItems;

    [Header("Настройки спавна")]
    public float spawnInterval = 15f;
    public float offsetFromRoad = 3f;
    public int maxItems = 30;

    private List<GameObject> spawnedLoot = new List<GameObject>();

    void Start()
    {
        SpawnLoot();
    }

    void SpawnLoot()
    {
        if (roadSpline == null || lootItems.Length == 0) return;

        float roadLength = SplineUtility.CalculateLength(
            roadSpline.Spline,
            roadSpline.transform.localToWorldMatrix
        );

        int count = Mathf.Min(maxItems, Mathf.FloorToInt(roadLength / spawnInterval));

        for (int i = 0; i < count; i++)
        {
            // ✅ ИСПОЛЬЗУЕМ UnityEngine.Random
            float t = UnityEngine.Random.Range(0.05f, 0.95f);

            float3 pos3 = roadSpline.Spline.EvaluatePosition(t);
            Vector3 pos = roadSpline.transform.TransformPoint(new Vector3(pos3.x, pos3.y, pos3.z));

            float3 tangent3 = roadSpline.Spline.EvaluateTangent(t);
            Vector3 tangent = roadSpline.transform.TransformDirection(new Vector3(tangent3.x, tangent3.y, tangent3.z));
            tangent.y = 0f;
            tangent.Normalize();

            Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;
            float side = UnityEngine.Random.value > 0.5f ? 1f : -1f;
            pos += right * side * offsetFromRoad;

            pos.y = GetGroundHeight(pos) + 0.5f;

            GameObject lootGO = new GameObject("Loot_" + UnityEngine.Random.Range(0, 999));
            lootGO.transform.position = pos;
            lootGO.transform.SetParent(transform);

            WorldLoot loot = lootGO.AddComponent<WorldLoot>();
            loot.itemData = lootItems[UnityEngine.Random.Range(0, lootItems.Length)];
            loot.quantity = UnityEngine.Random.Range(1, 4);

            spawnedLoot.Add(lootGO);
        }

        Debug.Log($"[LootSpawner] Создано {spawnedLoot.Count} предметов лута");
    }

    float GetGroundHeight(Vector3 position)
    {
        RaycastHit hit;
        Vector3 rayOrigin = new Vector3(position.x, position.y + 10f, position.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 20f))
        {
            return hit.point.y;
        }
        return position.y;
    }

    public void ClearLoot()
    {
        foreach (var item in spawnedLoot)
        {
            if (item != null) Destroy(item);
        }
        spawnedLoot.Clear();
    }

    [ContextMenu("Respawn Loot")]
    public void RespawnLoot()
    {
        ClearLoot();
        SpawnLoot();
    }
}