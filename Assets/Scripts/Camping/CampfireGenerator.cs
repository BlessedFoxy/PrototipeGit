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
    public SplineContainer splineContainer;
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

        float roadLength = GetRoadLength();
        if (roadLength <= 1f)
        {
            Debug.LogError($"[CampfireGenerator] Invalid road length: {roadLength:F0}m!");
            return;
        }

        Debug.Log($"[CampfireGenerator] Road length: {roadLength:F0}m");

        float currentSpacing = startSpacing;
        float distance = currentSpacing;
        int campfireCount = 0;

        while (distance < roadLength - campfireMinDistance)
        {
            float t = distance / roadLength;
            Vector3 worldPos = GetPointOnSpline(t);

            if (worldPos == Vector3.zero)
            {
                currentSpacing = Mathf.Min(currentSpacing + spacingIncrease, maxSpacing);
                distance += currentSpacing;
                continue;
            }

            if (worldPos.y > 15f || worldPos.y < -5f)
            {
                currentSpacing = Mathf.Min(currentSpacing + spacingIncrease, maxSpacing);
                distance += currentSpacing;
                continue;
            }

            Vector3 tangent = GetTangentOnSpline(t);
            Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;
            if (right.magnitude < 0.001f) right = Vector3.right;

            float side = (campfireCount % 2 == 0) ? 1f : -1f;
            Vector3 campPos = worldPos + right * side * campfireOffset;

            if (campPos.y > 15f || campPos.y < -5f)
            {
                side = -side;
                campPos = worldPos + right * side * campfireOffset;
                if (campPos.y > 15f || campPos.y < -5f)
                {
                    currentSpacing = Mathf.Min(currentSpacing + spacingIncrease, maxSpacing);
                    distance += currentSpacing;
                    continue;
                }
            }

            GameObject camp = Instantiate(campfirePrefab, campPos, Quaternion.identity, transform);

            Campfire campfireScript = camp.GetComponent<Campfire>();
            if (campfireScript == null)
            {
                campfireScript = camp.AddComponent<Campfire>();
            }

            campfireScript.Initialize(roadGenerator, distance);
            camp.transform.forward = tangent;

            campfires.Add(camp);
            campfireCount++;

            Debug.Log($"[CampfireGenerator] Campfire #{campfireCount} at {distance:F0}m (t={t:F3}), side: {(side > 0 ? "right" : "left")}");

            currentSpacing = Mathf.Min(currentSpacing + spacingIncrease, maxSpacing);
            distance += currentSpacing;
        }

        Debug.Log($"[CampfireGenerator] ✅ Total campfires: {campfireCount} on {roadLength:F0}m road");
    }

    private float GetRoadLength()
    {
        if (splineContainer != null && splineContainer.Spline != null)
        {
            try
            {
                return SplineUtility.CalculateLength(splineContainer.Spline, splineContainer.transform.localToWorldMatrix);
            }
            catch { }
        }

        if (roadGenerator != null)
        {
            return roadGenerator.GetRoadLength();
        }

        return 0f;
    }

    private Vector3 GetPointOnSpline(float t)
    {
        if (splineContainer != null && splineContainer.Spline != null)
        {
            try
            {
                float3 pos = splineContainer.EvaluatePosition(t);
                return new Vector3(pos.x, pos.y, pos.z);
            }
            catch { }
        }

        if (roadGenerator != null)
        {
            float distance = t * roadGenerator.GetRoadLength();
            float3 pos = roadGenerator.GetRoadPoint(distance);
            return new Vector3(pos.x, pos.y, pos.z);
        }

        return Vector3.zero;
    }

    private Vector3 GetTangentOnSpline(float t)
    {
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

    void OnDrawGizmosSelected()
    {
        if (splineContainer == null || splineContainer.Spline == null) return;

        float roadLength = GetRoadLength();
        if (roadLength <= 1f) return;

        Gizmos.color = Color.yellow;
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