using Unity.Mathematics;
using UnityEngine;

public class FogGenerator : MonoBehaviour
{
    [Header("Ссылки")]
    public RoadGenerator roadGenerator;

    [Header("Настройки тумана")]
    public float fogWidth = 20f;           // Ширина тумана от дороги
    public float fogDensity = 0.05f;
    public float fogHeight = 3f;
    public Color fogColor = new Color(0.6f, 0.65f, 0.7f, 0.3f);

    [Header("Префабы")]
    public GameObject fogPlanePrefab;      // Прозрачная плоскость с туманом

    private void Start()
    {
        if (roadGenerator == null)
        {
            roadGenerator = GetComponent<RoadGenerator>();
            if (roadGenerator == null)
            {
                Debug.LogError("❌ RoadGenerator не найден!");
                return;
            }
        }

        GenerateFog();
    }

    void GenerateFog()
    {
        float roadLength = roadGenerator.GetRoadLength();
        float segmentLength = 5f;
        int segments = Mathf.CeilToInt(roadLength / segmentLength);

        int fogCount = 0;

        for (int i = 0; i < segments; i++)
        {
            float distance = i * segmentLength;
            float t = Mathf.Clamp01(distance / roadLength);

            // Получаем позицию на дороге
            float3 pos = roadGenerator.GetRoadPoint(distance);

            // Направление дороги
            float3 tangent = GetTangentAtDistance(distance);
            float3 right = math.normalize(math.cross(tangent, new float3(0f, 1f, 0f)));

            // === ТУМАН СЛЕВА ===
            CreateFogPlane(pos + right * (fogWidth / 2f), distance, -1f);
            fogCount++;

            // === ТУМАН СПРАВА ===
            CreateFogPlane(pos - right * (fogWidth / 2f), distance, 1f);
            fogCount++;
        }

        Debug.Log($"🌫️ Туман создан: {fogCount} плоскостей");
    }

    void CreateFogPlane(float3 position, float distance, float side)
    {
        if (fogPlanePrefab == null)
        {
            // Создаём простую плоскость если нет префаба
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = $"Fog_{distance:F0}_{side}";
            plane.transform.SetParent(transform);
            plane.transform.position = position;
            plane.transform.localScale = new Vector3(8f, 1f, 5f);

            // Поворачиваем вдоль дороги
            float3 tangent = GetTangentAtDistance(distance);
            plane.transform.forward = tangent;

            // Делаем прозрачным
            Renderer renderer = plane.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = fogColor;
            mat.SetFloat("_Surface", 1); // Transparent
            renderer.material = mat;

            // Отключаем коллайдер
            Destroy(plane.GetComponent<Collider>());

            return;
        }

        // Используем префаб
        GameObject fog = Instantiate(fogPlanePrefab, position, Quaternion.identity, transform);
        fog.transform.forward = GetTangentAtDistance(distance);
    }

    float3 GetTangentAtDistance(float distance)
    {
        float roadLength = roadGenerator.GetRoadLength();
        float step = 1f / roadLength;

        float3 p1 = roadGenerator.GetRoadPoint(distance);
        float3 p2 = roadGenerator.GetRoadPoint(Mathf.Min(distance + step, roadLength));

        return math.normalize(p2 - p1);
    }
}