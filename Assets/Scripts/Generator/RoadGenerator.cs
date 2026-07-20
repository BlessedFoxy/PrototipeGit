using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

public class RoadGenerator : MonoBehaviour
{
    [Header("Настройки дороги")]
    public float roadLength = 200f;
    public float roadWidth = 14f;
    public float roadHeight = 0.5f;
    public int resolution = 30;
    public int worldSeed = 42;

    [Header("Ссылки")]
    public SplineContainer splineContainer;

    [Header("Цвет дороги")]
    public Color roadColor = new Color(0.4f, 0.35f, 0.25f);

    [Header("Повороты и подъёмы")]
    public float turnAmplitude = 8f;
    public float turnFrequency = 0.03f;
    public float heightAmplitude = 2f;
    public float heightFrequency = 0.02f;

    private float3[] roadPoints;
    private GameObject roadMesh;
    private Material roadMaterial;

    void Start()
    {
        Debug.Log("🚀 RoadGenerator START!");

        // Автоматически увеличиваем resolution для длинных дорог
        if (roadLength > 500)
        {
            resolution = Mathf.Max(resolution, 100);
        }
        else if (roadLength > 200)
        {
            resolution = Mathf.Max(resolution, 60);
        }

        UnityEngine.Random.InitState(worldSeed);
        GenerateRoad();
    }

    void GenerateRoad()
    {
        GenerateRoadPoints();

        if (roadPoints == null || roadPoints.Length < 2)
        {
            Debug.LogError("❌ Нет точек!");
            return;
        }

        CreateCatmullRomSpline();
        CreateRoadMesh();

        Debug.Log($"🛤️ Дорога создана! Длина: {roadLength}м, Ширина: {roadWidth}м");
    }

    void GenerateRoadPoints()
    {
        roadPoints = new float3[resolution];

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            float distance = t * roadLength;

            float x = Mathf.Sin(distance * turnFrequency + 0.3f) * turnAmplitude;
            x += Mathf.Sin(distance * turnFrequency * 2.3f + 1.2f) * turnAmplitude * 0.3f;
            x = Mathf.Clamp(x, -20f, 20f);

            float y = Mathf.Sin(distance * heightFrequency + 0.5f) * heightAmplitude;
            y += Mathf.Sin(distance * heightFrequency * 2.7f + 1.8f) * heightAmplitude * 0.3f;
            y = Mathf.Clamp(y, -5f, 10f);

            float z = distance;

            roadPoints[i] = new float3(x, y, z);
        }

        Debug.Log($"📊 Сгенерировано {roadPoints.Length} точек");
    }

    void CreateCatmullRomSpline()
    {
        if (splineContainer == null)
        {
            splineContainer = GetComponent<SplineContainer>();
            if (splineContainer == null)
                splineContainer = gameObject.AddComponent<SplineContainer>();
        }

        var spline = new Spline();

        for (int i = 0; i < roadPoints.Length; i++)
        {
            float3 pos = roadPoints[i];
            pos.y += 0.1f;
            spline.Add(new BezierKnot(pos, float3.zero, float3.zero));
        }

        splineContainer.Spline = spline;

        Debug.Log($"🛤️ Сплайн создан: {spline.Count} узлов");
    }

    void CreateRoadMesh()
    {
        if (roadPoints == null || roadPoints.Length < 2)
        {
            Debug.LogError("❌ Нет точек для дороги!");
            return;
        }

        if (roadMesh != null)
            DestroyImmediate(roadMesh);

        roadMesh = new GameObject("Road_Mesh");
        roadMesh.transform.SetParent(transform);
        roadMesh.transform.localPosition = Vector3.zero;

        roadMaterial = CreateRoadMaterial();

        List<CombineInstance> combines = new List<CombineInstance>();

        float segmentLength = 10f;
        float overlap = 2f;
        float effectiveLength = segmentLength - overlap;

        float totalDistance = 0f;
        int segmentCount = 0;

        while (totalDistance < roadLength)
        {
            float endDistance = Mathf.Min(totalDistance + segmentLength, roadLength);

            float3 p1 = GetPointAtDistance(totalDistance);
            float3 p2 = GetPointAtDistance(endDistance);

            if (math.distance(p1, p2) < 0.1f)
            {
                totalDistance += effectiveLength;
                continue;
            }

            Mesh segmentMesh = CreateRoadSegment(p1, p2);

            CombineInstance ci = new CombineInstance();
            ci.mesh = segmentMesh;
            ci.transform = Matrix4x4.identity;
            combines.Add(ci);

            segmentCount++;
            totalDistance += effectiveLength;
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.name = "RoadMesh";
        combinedMesh.CombineMeshes(combines.ToArray(), true, false);
        combinedMesh.RecalculateBounds();
        combinedMesh.RecalculateNormals();
        combinedMesh.Optimize();

        MeshFilter filter = roadMesh.AddComponent<MeshFilter>();
        filter.mesh = combinedMesh;

        MeshRenderer renderer = roadMesh.AddComponent<MeshRenderer>();
        renderer.material = roadMaterial;

        MeshCollider collider = roadMesh.AddComponent<MeshCollider>();
        collider.sharedMesh = combinedMesh;
        collider.convex = false;

        Debug.Log($"🏗️ Дорога: {segmentCount} сегментов, ширина {roadWidth}м");
    }

    float3 GetPointAtDistance(float distance)
    {
        float t = Mathf.Clamp01(distance / roadLength);
        int idx = Mathf.FloorToInt(t * (roadPoints.Length - 1));
        idx = Mathf.Clamp(idx, 0, roadPoints.Length - 1);

        float t2 = (t * (roadPoints.Length - 1)) - idx;
        int nextIdx = Mathf.Min(idx + 1, roadPoints.Length - 1);

        return math.lerp(roadPoints[idx], roadPoints[nextIdx], t2);
    }

    Mesh CreateRoadSegment(float3 p1, float3 p2)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Segment";

        float3 direction = p2 - p1;
        float length = math.length(direction);

        if (length < 0.1f)
        {
            length = 1f;
            direction = new float3(0, 0, 1);
        }

        float3 forward = math.normalize(direction);
        float3 up = new float3(0f, 1f, 0f);
        float3 right = math.normalize(math.cross(forward, up));

        float3 center = (p1 + p2) / 2f;
        center.y += roadHeight / 2f;

        float halfWidth = roadWidth / 2f;
        float halfHeight = roadHeight / 2f;
        float halfLength = length / 2f;

        List<Vector3> vertices = new List<Vector3>();

        // Верх
        vertices.Add(center - forward * halfLength - right * halfWidth + up * halfHeight);
        vertices.Add(center - forward * halfLength + right * halfWidth + up * halfHeight);
        vertices.Add(center + forward * halfLength + right * halfWidth + up * halfHeight);
        vertices.Add(center + forward * halfLength - right * halfWidth + up * halfHeight);

        // Низ
        vertices.Add(center - forward * halfLength - right * halfWidth - up * halfHeight);
        vertices.Add(center - forward * halfLength + right * halfWidth - up * halfHeight);
        vertices.Add(center + forward * halfLength + right * halfWidth - up * halfHeight);
        vertices.Add(center + forward * halfLength - right * halfWidth - up * halfHeight);

        List<int> triangles = new List<int>();

        // Верх
        triangles.AddRange(new int[] { 0, 1, 2, 0, 2, 3 });
        // Низ
        triangles.AddRange(new int[] { 4, 6, 5, 4, 7, 6 });
        // Перед
        triangles.AddRange(new int[] { 3, 2, 6, 3, 6, 7 });
        // Зад
        triangles.AddRange(new int[] { 0, 4, 5, 0, 5, 1 });
        // Лево
        triangles.AddRange(new int[] { 0, 3, 7, 0, 7, 4 });
        // Право
        triangles.AddRange(new int[] { 1, 5, 6, 1, 6, 2 });

        Vector3[] normals = new Vector3[vertices.Count];
        for (int i = 0; i < normals.Length; i++)
            normals[i] = Vector3.up;

        Vector2[] uv = new Vector2[vertices.Count];
        for (int i = 0; i < uv.Length; i++)
            uv[i] = new Vector2(i % 2, i / 4);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    Material CreateRoadMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");

        Material mat = new Material(shader);
        mat.color = roadColor;
        mat.enableInstancing = true;

        if (shader.name.Contains("Universal"))
        {
            mat.SetFloat("_Smoothness", 0.3f);
            mat.SetFloat("_Metallic", 0f);
        }

        Debug.Log($"🎨 Материал создан: {shader.name}, цвет: {roadColor}");
        return mat;
    }

    void OnDrawGizmos()
    {
        if (roadPoints == null) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < roadPoints.Length; i += 5)
        {
            Gizmos.DrawSphere(roadPoints[i], 0.5f);
        }
    }

    public float3 GetRoadPoint(float distance)
    {
        if (roadPoints == null || roadPoints.Length == 0)
            return float3.zero;

        float t = Mathf.Clamp01(distance / roadLength);
        int idx = Mathf.FloorToInt(t * (roadPoints.Length - 1));
        idx = Mathf.Clamp(idx, 0, roadPoints.Length - 1);
        return roadPoints[idx];
    }

    public float GetRoadLength() => roadLength;
}