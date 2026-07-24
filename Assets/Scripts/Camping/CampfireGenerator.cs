using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CampfireGenerator : MonoBehaviour
{
    [Header("Ссылки")]
    public RoadGeneratorEditor roadGenerator;
    public SplineContainer splineContainer;      // ← ДОБАВЛЯЕМ прямую ссылку на сплайн
    public GameObject campfirePrefab;

    [Header("Настройки лагерей")]
    public float startSpacing = 100f;
    public float spacingIncrease = 100f;
    public float maxSpacing = 500f;
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
                if (this != null) RegenerateCampfires();
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

        // Если сплайн не назначен, берём из RoadGenerator
        if (splineContainer == null && roadGenerator != null)
        {
            splineContainer = roadGenerator.splineContainer;
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

        // ============================================
        // ✅ ПОЛУЧАЕМ ДЛИНУ ИЗ СПЛАЙНА
        // ============================================
        float roadLength = GetRoadLength();

        if (roadLength <= 1f)
        {
            Debug.LogError($"[CampfireGenerator] Invalid road length: {roadLength}m!");
            return;
        }

        Debug.Log($"[CampfireGenerator] Road length: {roadLength:F0}m");

        // ============================================
        // ✅ РАСЧЁТ РАССТОЯНИЯ МЕЖДУ ЛАГЕРЯМИ
        // ============================================
        float currentSpacing = startSpacing;
        float distance = currentSpacing;  // ← Начинаем с первого интервала!
        int campfireCount = 0;

        while (distance < roadLength - campfireMinDistance)
        {
            // ============================================
            // ✅ ПОЛУЧАЕМ ТОЧКУ НА СПЛАЙНЕ
            // ============================================
            float t = distance / roadLength;  // ← Нормализованное расстояние (0-1)

            Vector3 worldPos = GetPointOnSpline(t);

            if (worldPos == Vector3.zero)
            {
                Debug.LogWarning($"[CampfireGenerator] Failed to get point at t={t:F3}, distance={distance:F0}m");
                currentSpacing = Mathf.Min(currentSpacing + spacingIncrease, maxSpacing);
                distance += currentSpacing;
                continue;
            }

            // Проверяем высоту
            if (worldPos.y > 15f || worldPos.y < -5f)
            {
                Debug.Log($"[CampfireGenerator] Skipping campfire at {distance:F0}m (bad height: {worldPos.y:F1})");
                currentSpacing = Mathf.Min(currentSpacing + spacingIncrease, maxSpacing);
                distance += currentSpacing;
                continue;
            }

            // ============================================
            // ✅ ВЫЧИСЛЯЕМ НАПРАВЛЕНИЕ
            // ============================================
            Vector3 tangent = GetTangentOnSpline(t);
            Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;
            if (right.magnitude < 0.001f) right = Vector3.right;

            // Чередуем стороны
            float side = (campfireCount % 2 == 0) ? 1f : -1f;
            Vector3 campPos = worldPos + right * side * campfireOffset;

            // Проверяем позицию
            if (campPos.y > 15f || campPos.y < -5f)
            {
                side = -side;
                campPos = worldPos + right * side * campfireOffset;
                if (campPos.y > 15f || campPos.y < -5f)
                {
                    Debug.Log($"[CampfireGenerator] Skipping campfire at {distance:F0}m (bad position)");
                    currentSpacing = Mathf.Min(currentSpacing + spacingIncrease, maxSpacing);
                    distance += currentSpacing;
                    continue;
                }
            }

            // ============================================
            // ✅ СОЗДАЁМ ЛАГЕРЬ
            // ============================================
            GameObject camp = Instantiate(campfirePrefab, campPos, Quaternion.identity, transform);

            // Настраиваем Campfire компонент
            Campfire campfireScript = camp.GetComponent<Campfire>();
            if (campfireScript == null)
            {
                campfireScript = camp.AddComponent<Campfire>();
            }

            // Инициализируем с ссылкой на генератор и расстоянием
            campfireScript.Initialize(roadGenerator, distance);

            // Поворачиваем к дороге
            camp.transform.forward = tangent;

            campfires.Add(camp);
            campfireCount++;

            Debug.Log($"[CampfireGenerator] Campfire #{campfireCount} at {distance:F0}m (t={t:F3}), side: {(side > 0 ? "right" : "left")}, spacing: {currentSpacing:F0}m");

            // Увеличиваем расстояние для следующего лагеря
            currentSpacing = Mathf.Min(currentSpacing + spacingIncrease, maxSpacing);
            distance += currentSpacing;
        }

        Debug.Log($"[CampfireGenerator] ✅ Total campfires: {campfireCount} on {roadLength:F0}m road");
    }

    // ============================================
    // ✅ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ РАБОТЫ СО СПЛАЙНОМ
    // ============================================

    float GetRoadLength()
    {
        // Пробуем получить длину из сплайна
        if (splineContainer != null && splineContainer.Spline != null)
        {
            try
            {
                return SplineUtility.CalculateLength(splineContainer.Spline, splineContainer.transform.localToWorldMatrix);
            }
            catch
            {
                // Если ошибка - пробуем другой способ
            }
        }

        // Если есть RoadGenerator - используем его
        if (roadGenerator != null)
        {
            return roadGenerator.GetRoadLength();
        }

        return 0f;
    }

    Vector3 GetPointOnSpline(float t)
    {
        // Приоритет 1: SplineContainer
        if (splineContainer != null && splineContainer.Spline != null)
        {
            try
            {
                float3 pos = splineContainer.EvaluatePosition(t);
                return new Vector3(pos.x, pos.y, pos.z);
            }
            catch
            {
                // Если ошибка - пробуем другой способ
            }
        }

        // Приоритет 2: RoadGenerator
        if (roadGenerator != null)
        {
            float distance = t * roadGenerator.GetRoadLength();
            float3 pos = roadGenerator.GetRoadPoint(distance);
            return new Vector3(pos.x, pos.y, pos.z);
        }

        return Vector3.zero;
    }

    Vector3 GetTangentOnSpline(float t)
    {
        // Приоритет 1: SplineContainer
        if (splineContainer != null && splineContainer.Spline != null)
        {
            try
            {
                float3 tangent = splineContainer.EvaluateTangent(t);
                if (math.length(tangent) > 0.001f)
                {
                    tangent = math.normalize(tangent);
                    return new Vector3(tangent.x, tangent.y, tangent.z);
                }
            }
            catch { }
        }

        // Приоритет 2: RoadGenerator
        if (roadGenerator != null)
        {
            float roadLength = roadGenerator.GetRoadLength();
            float step = Mathf.Max(0.5f, roadLength / 100f);
            float distance = t * roadLength;

            float3 p1 = roadGenerator.GetRoadPoint(Mathf.Max(distance - step, 0.1f));
            float3 p2 = roadGenerator.GetRoadPoint(Mathf.Min(distance + step, roadLength - 0.1f));

            float3 tangent = p2 - p1;
            if (math.length(tangent) > 0.001f)
            {
                tangent = math.normalize(tangent);
                return new Vector3(tangent.x, tangent.y, tangent.z);
            }
        }

        return Vector3.forward;
    }

    public void ClearCampfires()
    {
        foreach (var obj in campfires)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(obj);
                else Destroy(obj);
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
        Debug.Log("[CampfireGenerator] 🔄 Regenerating campfires...");
        ClearCampfires();
        GenerateCampfires();
        EditorUtility.SetDirty(this);
    }

    [ContextMenu("Clear All Campfires")]
    public void ClearAllCampfires()
    {
        Debug.Log("[CampfireGenerator] 🗑️ Clearing all campfires...");
        ClearCampfires();
        EditorUtility.SetDirty(this);
    }

    // ============================================
    // ✅ ВИЗУАЛИЗАЦИЯ В РЕДАКТОРЕ
    // ============================================
    void OnDrawGizmosSelected()
    {
        if (splineContainer == null || splineContainer.Spline == null) return;

        float roadLength = GetRoadLength();
        if (roadLength <= 1f) return;

        Gizmos.color = Color.yellow;

        // Показываем все позиции для лагерей
        float spacing = startSpacing;
        float distance = spacing;
        int count = 0;

        while (distance < roadLength - campfireMinDistance)
        {
            float t = distance / roadLength;
            Vector3 pos = GetPointOnSpline(t);

            if (pos != Vector3.zero)
            {
                Gizmos.DrawWireSphere(pos, 2f);

                // Показываем смещение в стороны
                Vector3 tangent = GetTangentOnSpline(t);
                Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;
                if (right.magnitude < 0.001f) right = Vector3.right;

                float side = (count % 2 == 0) ? 1f : -1f;
                Vector3 leftPos = pos + right * side * campfireOffset;

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(leftPos, 1f);
                Gizmos.DrawLine(pos, leftPos);
                Gizmos.color = Color.yellow;
            }

            spacing = Mathf.Min(spacing + spacingIncrease, maxSpacing);
            distance += spacing;
            count++;
        }
    }
#endif
}