using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    [Header("Ссылки")]
    public SplineContainer targetSpline;     // Сюда перетащи SplineContainer
    public GameObject itemPrefab;            // Сюда перетащи префаб Item_Prefab

    [Header("Настройки спавна")]
    [Range(1, 100)]
    public int itemsCount = 15;              // Сколько предметов разбросать
    public float heightOffset = 0.2f;        // Насколько поднять над землей

    [Header("Разброс от дороги")]
    public bool useSideOffset = true;        // Включить разброс в стороны
    public float sideOffsetRange = 2f;       // Насколько далеко влево/вправо от сплайна (в метрах)

    [Header("Отладка")]
    public bool showGizmos = true;
    public bool spawnOnStart = false;        // Спавнить ли при запуске игры

    private List<GameObject> spawnedItems = new List<GameObject>();

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnItems();
        }
    }

    [ContextMenu("Spawn Items")]
    public void SpawnItems()
    {
        if (targetSpline == null)
        {
            Debug.LogError("[ItemSpawner] ❌ Не назначен SplineContainer!");
            return;
        }
        if (itemPrefab == null)
        {
            Debug.LogError("[ItemSpawner] ❌ Не назначен префаб предмета!");
            return;
        }

        ClearItems();

        if (targetSpline.Spline == null || targetSpline.Spline.Count < 2) return;
        float totalLength = CalculateSplineLength();

        for (int i = 0; i < itemsCount; i++)
        {
            float randomDistance = UnityEngine.Random.Range(0f, totalLength);
            float normalizedT = Mathf.Clamp01(randomDistance / totalLength);

            float3 pos = targetSpline.Spline.EvaluatePosition(normalizedT);
            float3 tangent = targetSpline.Spline.EvaluateTangent(normalizedT);

            Vector3 spawnPosition = (Vector3)pos + Vector3.up * heightOffset;

            // ✅ РАЗБРОС В СТОРОНЫ ОТ ДОРОГИ
            if (useSideOffset)
            {
                // Находим перпендикулярное направление от дороги (влево/вправо)
                Vector3 flatTangent1 = new Vector3(tangent.x, 0f, tangent.z).normalized;
                Vector3 rightDirection = Vector3.Cross(Vector3.up, flatTangent1).normalized;

                // Случайное смещение влево или вправо
                float randomSide = UnityEngine.Random.Range(-sideOffsetRange, sideOffsetRange);
                spawnPosition += rightDirection * randomSide;
            }

            // Создаем предмет
            GameObject newItem = Instantiate(itemPrefab, transform);
            newItem.transform.position = spawnPosition;

            // Поворачиваем по направлению дороги (избегаем конфликта имен)
            Vector3 tangentDir = new Vector3(tangent.x, 0f, tangent.z).normalized;
            if (tangentDir != Vector3.zero)
            {
                newItem.transform.rotation = Quaternion.LookRotation(tangentDir);
            }

            spawnedItems.Add(newItem);
        }

        Debug.Log($"[ItemSpawner] ✅ Сгенерировано {itemsCount} предметов с разбросом!");
    }

    [ContextMenu("Clear Items")]
    public void ClearItems()
    {
        foreach (GameObject item in spawnedItems)
        {
            if (item != null) DestroyImmediate(item);
        }
        spawnedItems.Clear();
        Debug.Log("[ItemSpawner] 🧹 Все предметы очищены!");
    }

    private float CalculateSplineLength()
    {
        if (targetSpline.Spline == null || targetSpline.Spline.Count < 2) return 100f;
        float total = 0f;
        int segments = 200;
        for (int i = 0; i < segments; i++)
        {
            float t1 = (float)i / segments;
            float t2 = (float)(i + 1) / segments;
            float3 p1 = targetSpline.Spline.EvaluatePosition(t1);
            float3 p2 = targetSpline.Spline.EvaluatePosition(t2);
            total += math.distance(p1, p2);
        }
        return total > 1f ? total : 100f;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || targetSpline == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        if (targetSpline.Spline != null)
        {
            for (int i = 0; i < 100; i++)
            {
                float t = (float)i / 100;
                Vector3 pos = targetSpline.Spline.EvaluatePosition(t);
                Vector3 offsetPos = pos + Vector3.up * heightOffset;

                if (useSideOffset)
                {
                    Vector3 tangent = targetSpline.Spline.EvaluateTangent(t);
                    Vector3 flatTangent1 = new Vector3(tangent.x, 0f, tangent.z).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, flatTangent1).normalized;

                    Gizmos.DrawLine(offsetPos - right * sideOffsetRange, offsetPos + right * sideOffsetRange);
                }
                else
                {
                    Gizmos.DrawSphere(offsetPos, 0.3f);
                }
            }
        }
    }
}