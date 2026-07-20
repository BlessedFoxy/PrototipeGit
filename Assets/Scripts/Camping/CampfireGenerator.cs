using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CampfireGenerator : MonoBehaviour
{
    [Header("Ссылки")]
    public RoadGeneratorEditor roadGenerator;
    public GameObject campfirePrefab;

    [Header("Настройки лагерей")]
    public float startSpacing = 100f;        // Первый лагерь через 100м
    public float spacingIncrease = 100f;     // Каждый раз +100м
    public float maxSpacing = 500f;          // Максимальное расстояние
    public float campfireOffset = 10f;
    public float campfireMinDistance = 30f;

    [Header("Управление в редакторе")]
    public bool autoGenerateInEditor = true;

    private List<GameObject> campfires = new List<GameObject>();

#if UNITY_EDITOR
    void OnValidate()
    {
        if (autoGenerateInEditor && !Application.isPlaying)
        {
            EditorApplication.delayCall += () => {
                if (this != null)
                {
                    RegenerateCampfires();
                }
            };
        }
    }
#endif

    void Start()
    {
        if (roadGenerator == null)
        {
            roadGenerator = GetComponent<RoadGeneratorEditor>();
            if (roadGenerator == null)
            {
                Debug.LogError("[CampfireGenerator] RoadGenerator not found!");
                return;
            }
        }

        GenerateCampfires();
    }

    public void GenerateCampfires()
    {
        ClearCampfires();

        if (campfirePrefab == null)
        {
            Debug.LogWarning("[CampfireGenerator] No campfire prefab assigned!");
            return;
        }

        if (roadGenerator == null)
        {
            Debug.LogError("[CampfireGenerator] RoadGenerator is null!");
            return;
        }

        float roadLength = roadGenerator.GetRoadLength();
        Debug.Log($"[CampfireGenerator] Road length: {roadLength}m");

        // ============================================
        // РАСЧЁТ РАССТОЯНИЯ МЕЖДУ ЛАГЕРЯМИ
        // ============================================
        float currentSpacing = startSpacing;
        float distance = 0f;
        int campfireCount = 0;

        while (distance < roadLength - campfireMinDistance)
        {
            // Увеличиваем дистанцию
            distance += currentSpacing;

            // Проверяем, не вышли ли за пределы дороги
            if (distance > roadLength - campfireMinDistance)
                break;

            // Получаем точку на дороге
            float3 pos = roadGenerator.GetRoadPoint(distance);
            pos.y += 0.5f;

            // Направление
            float3 tangent = GetTangentAtDistance(distance);
            float3 right = math.normalize(math.cross(tangent, new float3(0f, 1f, 0f)));
            if (math.lengthsq(right) < 0.001f)
                right = new float3(1f, 0f, 0f);

            // Чередуем стороны
            float side = (campfireCount % 2 == 0) ? 1f : -1f;
            float3 campPos = pos + right * side * campfireOffset;

            if (campPos.y > 15f || campPos.y < -3f)
            {
                // Если позиция плохая — пробуем другую сторону
                side = -side;
                campPos = pos + right * side * campfireOffset;
                if (campPos.y > 15f || campPos.y < -3f)
                {
                    // Если всё равно плохо — пропускаем
                    Debug.Log($"[CampfireGenerator] Skipping campfire at {distance:F0}m (bad position)");
                    // Увеличиваем spacing для следующего
                    currentSpacing = Mathf.Min(currentSpacing + spacingIncrease, maxSpacing);
                    continue;
                }
            }

            // Создаём лагерь
            GameObject camp = Instantiate(campfirePrefab, campPos, Quaternion.identity, transform);
            camp.transform.forward = tangent;
            campfires.Add(camp);
            campfireCount++;

            Debug.Log($"[CampfireGenerator] Campfire #{campfireCount} at {distance:F0}m, spacing: {currentSpacing:F0}m, side: {(side > 0 ? "right" : "left")}");

            // Увеличиваем расстояние для следующего лагеря
            currentSpacing = Mathf.Min(currentSpacing + spacingIncrease, maxSpacing);
        }

        Debug.Log($"[CampfireGenerator] Total campfires: {campfireCount}");
        Debug.Log($"[CampfireGenerator] Final spacing: {currentSpacing:F0}m");
    }

    float3 GetTangentAtDistance(float distance)
    {
        float roadLength = roadGenerator.GetRoadLength();
        float step = Mathf.Max(0.5f, roadLength / 100f);

        float3 p1 = roadGenerator.GetRoadPoint(distance);
        float3 p2 = roadGenerator.GetRoadPoint(Mathf.Min(distance + step, roadLength - 0.1f));

        float3 tangent = p2 - p1;

        if (math.lengthsq(tangent) < 0.0001f)
        {
            float3 p0 = roadGenerator.GetRoadPoint(Mathf.Max(distance - step, 0.1f));
            tangent = p1 - p0;
        }

        if (math.lengthsq(tangent) < 0.0001f)
            return new float3(0f, 0f, 1f);

        return math.normalize(tangent);
    }

    public void ClearCampfires()
    {
        foreach (var obj in campfires)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(obj);
                else
                    Destroy(obj);
#else
                Destroy(obj);
#endif
            }
        }
        campfires.Clear();
    }

#if UNITY_EDITOR
    [ContextMenu("Regenerate Campfires")]
    public void RegenerateCampfires()
    {
        Debug.Log("[CampfireGenerator] Regenerating...");
        ClearCampfires();
        GenerateCampfires();
        EditorUtility.SetDirty(this);
    }

    [ContextMenu("Clear All Campfires")]
    public void ClearAllCampfires()
    {
        Debug.Log("[CampfireGenerator] Clearing all...");
        ClearCampfires();
        EditorUtility.SetDirty(this);
    }
#endif
}