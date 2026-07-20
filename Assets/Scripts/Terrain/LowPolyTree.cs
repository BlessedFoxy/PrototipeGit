using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LowPolyTree : MonoBehaviour
{
    [Header("Настройки")]
    public float trunkHeight = 2f;
    public float trunkRadius = 0.2f;
    public float crownRadius = 1.2f;
    public float crownHeight = 1.8f;
    public int segments = 6;  // Лоу-поли

    private Mesh mesh;

    void Start()
    {
        GenerateMesh();
    }

    void GenerateMesh()
    {
        mesh = new Mesh();
        mesh.name = "LowPolyTree";

        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();
        var colors = new System.Collections.Generic.List<Color>();

        // === СТВОЛ (конус) ===
        int trunkSegs = segments / 2;
        for (int i = 0; i < trunkSegs; i++)
        {
            float angle1 = (float)i / trunkSegs * Mathf.PI * 2f;
            float angle2 = (float)(i + 1) / trunkSegs * Mathf.PI * 2f;

            // Нижние точки
            float x1 = Mathf.Cos(angle1) * trunkRadius;
            float z1 = Mathf.Sin(angle1) * trunkRadius;
            float x2 = Mathf.Cos(angle2) * trunkRadius;
            float z2 = Mathf.Sin(angle2) * trunkRadius;

            // Верхние точки (уже)
            float topRadius = trunkRadius * 0.3f;
            float tx1 = Mathf.Cos(angle1) * topRadius;
            float tz1 = Mathf.Sin(angle1) * topRadius;
            float tx2 = Mathf.Cos(angle2) * topRadius;
            float tz2 = Mathf.Sin(angle2) * topRadius;

            int baseIdx = vertices.Count;

            // Нижние вершины
            vertices.Add(new Vector3(x1, 0, z1));
            vertices.Add(new Vector3(x2, 0, z2));
            vertices.Add(new Vector3(tx1, trunkHeight, tz1));
            vertices.Add(new Vector3(tx2, trunkHeight, tz2));

            // Цвета ствола
            colors.Add(new Color(0.4f, 0.25f, 0.1f));
            colors.Add(new Color(0.4f, 0.25f, 0.1f));
            colors.Add(new Color(0.5f, 0.3f, 0.15f));
            colors.Add(new Color(0.5f, 0.3f, 0.15f));

            // Треугольники
            triangles.Add(baseIdx);
            triangles.Add(baseIdx + 2);
            triangles.Add(baseIdx + 1);
            triangles.Add(baseIdx + 1);
            triangles.Add(baseIdx + 2);
            triangles.Add(baseIdx + 3);
        }

        // === КРОНА (сфера) ===
        int crownSegs = segments;
        for (int i = 0; i < crownSegs; i++)
        {
            float lat1 = (float)i / crownSegs * Mathf.PI;
            float lat2 = (float)(i + 1) / crownSegs * Mathf.PI;

            for (int j = 0; j < crownSegs; j++)
            {
                float lon1 = (float)j / crownSegs * Mathf.PI * 2f;
                float lon2 = (float)(j + 1) / crownSegs * Mathf.PI * 2f;

                Vector3 p1 = GetSpherePoint(lon1, lat1, crownRadius) + Vector3.up * (trunkHeight + crownHeight * 0.5f);
                Vector3 p2 = GetSpherePoint(lon2, lat1, crownRadius) + Vector3.up * (trunkHeight + crownHeight * 0.5f);
                Vector3 p3 = GetSpherePoint(lon1, lat2, crownRadius) + Vector3.up * (trunkHeight + crownHeight * 0.5f);
                Vector3 p4 = GetSpherePoint(lon2, lat2, crownRadius) + Vector3.up * (trunkHeight + crownHeight * 0.5f);

                int baseIdx = vertices.Count;
                vertices.Add(p1);
                vertices.Add(p2);
                vertices.Add(p3);
                vertices.Add(p4);

                Color green = new Color(0.2f, 0.6f + Random.Range(-0.1f, 0.1f), 0.15f);
                colors.Add(green);
                colors.Add(green);
                colors.Add(green);
                colors.Add(green);

                triangles.Add(baseIdx);
                triangles.Add(baseIdx + 2);
                triangles.Add(baseIdx + 1);
                triangles.Add(baseIdx + 1);
                triangles.Add(baseIdx + 2);
                triangles.Add(baseIdx + 3);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();

        GetComponent<MeshFilter>().mesh = mesh;
    }

    Vector3 GetSpherePoint(float lon, float lat, float radius)
    {
        float x = Mathf.Cos(lon) * Mathf.Sin(lat) * radius;
        float y = Mathf.Cos(lat) * radius;
        float z = Mathf.Sin(lon) * Mathf.Sin(lat) * radius;
        return new Vector3(x, y, z);
    }
}