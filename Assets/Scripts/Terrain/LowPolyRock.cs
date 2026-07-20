using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LowPolyRock : MonoBehaviour
{
    [Header("Настройки")]
    public float size = 1f;
    public int segments = 8;

    void Start()
    {
        GenerateMesh();
    }

    void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "LowPolyRock";

        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        // Сфера с искажениями
        for (int i = 0; i < segments; i++)
        {
            float lat1 = (float)i / segments * Mathf.PI;
            float lat2 = (float)(i + 1) / segments * Mathf.PI;

            for (int j = 0; j < segments; j++)
            {
                float lon1 = (float)j / segments * Mathf.PI * 2f;
                float lon2 = (float)(j + 1) / segments * Mathf.PI * 2f;

                float r1 = size * (0.8f + Random.Range(-0.2f, 0.2f));
                float r2 = size * (0.8f + Random.Range(-0.2f, 0.2f));
                float r3 = size * (0.8f + Random.Range(-0.2f, 0.2f));
                float r4 = size * (0.8f + Random.Range(-0.2f, 0.2f));

                Vector3 p1 = GetSpherePoint(lon1, lat1, r1);
                Vector3 p2 = GetSpherePoint(lon2, lat1, r2);
                Vector3 p3 = GetSpherePoint(lon1, lat2, r3);
                Vector3 p4 = GetSpherePoint(lon2, lat2, r4);

                int baseIdx = vertices.Count;
                vertices.Add(p1);
                vertices.Add(p2);
                vertices.Add(p3);
                vertices.Add(p4);

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