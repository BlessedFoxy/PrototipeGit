using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class RoadGeneratorEditor : MonoBehaviour
{
    [Header("Настройки дороги")]
    public float roadLength = 200f;
    public float roadWidth = 14f;
    public float roadHeight = 0.5f;
    public int worldSeed = 42;

    [Header("Ссылки")]
    public SplineContainer splineContainer;

    [Header("Цвет дороги")]
    public Color roadColor = new Color(0.4f, 0.35f, 0.25f);

    [Header("Повороты (рандомные)")]
    public float turnAmplitudeMin = 5f;
    public float turnAmplitudeMax = 15f;
    public float turnFrequencyMin = 0.015f;
    public float turnFrequencyMax = 0.04f;

    [Header("Направление дороги")]
    public bool enableDirectionChanges = true;
    public float directionChangeIntervalMin = 30f;
    public float directionChangeIntervalMax = 80f;
    public float turnAngleMin = 20f;
    public float turnAngleMax = 60f;
    public float straightLengthMin = 15f;
    public float straightLengthMax = 35f;

    [Header("Балансировка направления")]
    public bool enableDirectionBalance = true;
    public float maxTotalAngle = 180f;
    public float returnToStraightChance = 0.3f;

    [Header("Высота дороги")]
    public float heightAmplitudeMin = 2f;
    public float heightAmplitudeMax = 10f;
    public float heightFrequencyMin = 0.01f;
    public float heightFrequencyMax = 0.025f;
    public float heightOffsetMin = -3f;
    public float heightOffsetMax = 3f;
    public float steepnessMin = 0.5f;
    public float steepnessMax = 2f;

    [Header("Разрешение")]
    [Range(50, 500)]
    public int splineResolution = 200;
    [Range(3, 30)]
    public int widthSegments = 8;
    [Range(1, 5)]
    public int heightSegments = 2;

    [Header("Сглаживание сплайна")]
    public float splineSmoothness = 0.5f;

    [Header("Базовый плэйн")]
    public bool useBasePlane = true;
    public float basePlaneWidth = 40f;
    public float basePlaneHeight = 0.3f;
    public float basePlaneYOffset = -0.15f;
    public Color basePlaneColor = new Color(0.3f, 0.25f, 0.18f);

    [Header("Управление")]
    public bool autoGenerate = false;
    public bool generateOnStart = true;
    public bool randomizeOnGenerate = true;

    // ============================================
    // ✅ СОХРАНЕНИЕ ПАРАМЕТРОВ ДОРОГИ
    // ============================================
    [Header("Сохранённые параметры (для повторной генерации)")]
    [SerializeField] private float savedTurnAmplitude;
    [SerializeField] private float savedTurnFrequency;
    [SerializeField] private float savedHeightAmplitude;
    [SerializeField] private float savedHeightFrequency;
    [SerializeField] private float savedHeightOffset;
    [SerializeField] private float savedSteepness;
    [SerializeField] private int savedSeed;
    [SerializeField] private bool hasSavedParameters = false;

    [Header("Отладка")]
    public bool showGizmos = true;
    public bool showSplinePoints = true;
    public bool showRoadPoints = true;
    public bool showDirectionSegments = true;

    private List<float3> roadPoints;
    private GameObject roadMeshObject;
    private GameObject basePlaneMeshObject;
    private Material roadMaterial;
    private Material basePlaneMaterial;
    private bool isGenerating = false;

    private float currentTurnAmplitude;
    private float currentTurnFrequency;
    private float currentHeightAmplitude;
    private float currentHeightFrequency;
    private float currentHeightOffset;
    private float currentSteepness;

    private List<DirectionSegment> directionSegments;
    private float totalAngle = 0f;

    private class DirectionSegment
    {
        public float startDistance;
        public float endDistance;
        public float angle;
        public float turnRadius;
        public float straightLength;
        public bool isReturning;
    }

    void Awake()
    {
        if (!Application.isPlaying && autoGenerate)
        {
            GenerateRoad();
        }
    }

    void Start()
    {
        if (Application.isPlaying && generateOnStart)
        {
            Debug.Log("🚀 RoadGenerator START!");

            if (randomizeOnGenerate)
            {
                // ✅ Если нужно рандомизировать — генерируем новую
                RandomizeAndSaveParameters();
                GenerateRoad();
            }
            else if (hasSavedParameters)
            {
                // ✅ Используем сохранённые параметры
                Debug.Log("📂 Использование сохранённых параметров");
                LoadSavedParameters();
                GenerateRoad();
            }
            else
            {
                // ✅ Если нет сохранённых — генерируем с дефолтными
                Debug.Log("⚠️ Нет сохранённых параметров, генерируем новые");
                RandomizeAndSaveParameters();
                GenerateRoad();
            }
        }
    }

    void OnValidate() { }

    // ============================================
    // ✅ СОХРАНЕНИЕ И ЗАГРУЗКА ПАРАМЕТРОВ
    // ============================================

    public void RandomizeAndSaveParameters()
    {
        UnityEngine.Random.InitState(worldSeed + (int)System.DateTime.Now.Ticks);

        savedTurnAmplitude = UnityEngine.Random.Range(turnAmplitudeMin, turnAmplitudeMax);
        savedTurnFrequency = UnityEngine.Random.Range(turnFrequencyMin, turnFrequencyMax);
        savedHeightAmplitude = UnityEngine.Random.Range(heightAmplitudeMin, heightAmplitudeMax);
        savedHeightFrequency = UnityEngine.Random.Range(heightFrequencyMin, heightFrequencyMax);
        savedHeightOffset = UnityEngine.Random.Range(heightOffsetMin, heightOffsetMax);
        savedSteepness = UnityEngine.Random.Range(steepnessMin, steepnessMax);
        savedSeed = worldSeed + (int)System.DateTime.Now.Ticks;
        hasSavedParameters = true;

        Debug.Log($"💾 Сохранены параметры: Turn={savedTurnAmplitude:F1}°, Height={savedHeightAmplitude:F1}м");

        // Применяем
        LoadSavedParameters();
    }

    public void LoadSavedParameters()
    {
        if (!hasSavedParameters)
        {
            Debug.LogWarning("⚠️ Нет сохранённых параметров!");
            return;
        }

        currentTurnAmplitude = savedTurnAmplitude;
        currentTurnFrequency = savedTurnFrequency;
        currentHeightAmplitude = savedHeightAmplitude;
        currentHeightFrequency = savedHeightFrequency;
        currentHeightOffset = savedHeightOffset;
        currentSteepness = savedSteepness;

        Debug.Log($"📂 Загружены параметры: Turn={currentTurnAmplitude:F1}°, Height={currentHeightAmplitude:F1}м");
    }

    [ContextMenu("Save Current Parameters")]
    public void SaveCurrentParameters()
    {
        savedTurnAmplitude = currentTurnAmplitude;
        savedTurnFrequency = currentTurnFrequency;
        savedHeightAmplitude = currentHeightAmplitude;
        savedHeightFrequency = currentHeightFrequency;
        savedHeightOffset = currentHeightOffset;
        savedSteepness = currentSteepness;
        savedSeed = worldSeed;
        hasSavedParameters = true;

        Debug.Log("💾 Параметры сохранены!");

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Clear Saved Parameters")]
    public void ClearSavedParameters()
    {
        hasSavedParameters = false;
        Debug.Log("🗑️ Сохранённые параметры очищены");

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    // ============================================
    // ✅ ГЕНЕРАЦИЯ
    // ============================================

    public void RandomizeParameters()
    {
        UnityEngine.Random.InitState(worldSeed + (int)System.DateTime.Now.Ticks);

        currentTurnAmplitude = UnityEngine.Random.Range(turnAmplitudeMin, turnAmplitudeMax);
        currentTurnFrequency = UnityEngine.Random.Range(turnFrequencyMin, turnFrequencyMax);
        currentHeightAmplitude = UnityEngine.Random.Range(heightAmplitudeMin, heightAmplitudeMax);
        currentHeightFrequency = UnityEngine.Random.Range(heightFrequencyMin, heightFrequencyMax);
        currentHeightOffset = UnityEngine.Random.Range(heightOffsetMin, heightOffsetMax);
        currentSteepness = UnityEngine.Random.Range(steepnessMin, steepnessMax);

        Debug.Log($"🎲 Параметры: Turn={currentTurnAmplitude:F1}°, Height={currentHeightAmplitude:F1}м");
    }

    [ContextMenu("Generate Road")]
    public void GenerateRoad()
    {
        if (isGenerating) return;
        isGenerating = true;

        Debug.Log("🔄 Генерация дороги...");

        if (!hasSavedParameters || randomizeOnGenerate)
        {
            if (randomizeOnGenerate)
            {
                RandomizeAndSaveParameters();
            }
            else
            {
                RandomizeParameters();
            }
        }
        else
        {
            LoadSavedParameters();
        }

        ClearRoadMesh();

        if (enableDirectionChanges)
        {
            GenerateRoadPointsWithBalancedDirections();
        }
        else
        {
            GenerateRoadPoints();
        }

        if (roadPoints == null || roadPoints.Count < 2)
        {
            Debug.LogError("❌ Нет точек!");
            isGenerating = false;
            return;
        }

        CreateSmoothSpline();

        if (useBasePlane)
        {
            Debug.Log("🏗️ Создание базового плэйна...");
            CreateBasePlane();
        }

        CreateRoadFromSpline();

        Debug.Log($"✅ Дорога создана! Длина: {roadLength}м, Точек: {roadPoints.Count}");
        isGenerating = false;
    }

    [ContextMenu("Generate Same Road")]
    public void GenerateSameRoad()
    {
        if (!hasSavedParameters)
        {
            Debug.LogWarning("⚠️ Нет сохранённых параметров! Сначала сгенерируйте дорогу.");
            return;
        }

        Debug.Log("🔄 Перегенерация той же дороги...");
        LoadSavedParameters();
        GenerateRoad();
    }

    [ContextMenu("Generate Random Road")]
    public void GenerateRandomRoad()
    {
        RandomizeAndSaveParameters();
        GenerateRoad();
    }

    [ContextMenu("Clear Road")]
    public void ClearRoad()
    {
        Debug.Log("🗑️ Удаление дороги...");
        ClearRoadMesh();

        if (splineContainer != null)
        {
            splineContainer.Spline = new Spline();
#if UNITY_EDITOR
            EditorUtility.SetDirty(splineContainer);
#endif
        }

        roadPoints = null;
        directionSegments = null;
        Debug.Log("✅ Дорога удалена!");
    }

    // ============================================
    // ✅ ОСТАЛЬНЫЕ МЕТОДЫ (без изменений)
    // ============================================

    void GenerateRoadPointsWithBalancedDirections()
    {
        int actualResolution = Mathf.Max(splineResolution, 100);
        if (roadLength > 500) actualResolution = Mathf.Max(actualResolution, 300);
        else if (roadLength > 300) actualResolution = Mathf.Max(actualResolution, 200);

        roadPoints = new List<float3>(actualResolution);
        directionSegments = new List<DirectionSegment>();

        float step = roadLength / (actualResolution - 1);
        float distance = 0f;

        float currentAngle = 0f;
        float2 currentPosition = new float2(0f, 0f);
        totalAngle = 0f;

        float remainingDistance = roadLength;
        float totalDistance = 0f;

        int maxAttempts = 100;
        int attempts = 0;

        while (remainingDistance > 0 && attempts < maxAttempts)
        {
            attempts++;

            float interval = UnityEngine.Random.Range(directionChangeIntervalMin, directionChangeIntervalMax);
            interval = Mathf.Min(interval, remainingDistance);

            float straightLength = UnityEngine.Random.Range(straightLengthMin, straightLengthMax);
            straightLength = Mathf.Min(straightLength, interval * 0.4f);

            float angleDeg = 0f;
            bool isReturning = false;

            if (enableDirectionBalance)
            {
                float maxAllowedAngle = maxTotalAngle - Mathf.Abs(totalAngle);

                if (Mathf.Abs(totalAngle) > maxTotalAngle * 0.7f)
                {
                    isReturning = true;
                    float returnAngle = UnityEngine.Random.Range(20f, 50f);
                    angleDeg = (totalAngle > 0) ? -returnAngle : returnAngle;
                }
                else if (UnityEngine.Random.value < returnToStraightChance && Mathf.Abs(totalAngle) > 30f)
                {
                    isReturning = true;
                    float returnAngle = UnityEngine.Random.Range(15f, 40f);
                    angleDeg = (totalAngle > 0) ? -returnAngle : returnAngle;
                }
                else
                {
                    float maxAngle = Mathf.Min(turnAngleMax, maxAllowedAngle);
                    float minAngle = Mathf.Min(turnAngleMin, maxAngle);

                    if (maxAngle < 5f)
                    {
                        angleDeg = 0f;
                    }
                    else
                    {
                        angleDeg = UnityEngine.Random.Range(minAngle, maxAngle);
                        if (UnityEngine.Random.value > 0.5f) angleDeg = -angleDeg;
                    }
                }

                float newTotal = totalAngle + angleDeg;
                if (Mathf.Abs(newTotal) > maxTotalAngle)
                {
                    if (totalAngle > 0)
                        angleDeg = Mathf.Min(angleDeg, maxTotalAngle - totalAngle);
                    else
                        angleDeg = Mathf.Max(angleDeg, -maxTotalAngle - totalAngle);
                }
            }
            else
            {
                angleDeg = UnityEngine.Random.Range(turnAngleMin, turnAngleMax);
                if (UnityEngine.Random.value > 0.5f) angleDeg = -angleDeg;
            }

            totalAngle += angleDeg;
            if (Mathf.Abs(angleDeg) < 3f) angleDeg = 0f;

            float angleRad = angleDeg * Mathf.Deg2Rad;

            DirectionSegment segment = new DirectionSegment
            {
                startDistance = totalDistance,
                endDistance = totalDistance + interval,
                angle = angleRad,
                straightLength = straightLength,
                isReturning = isReturning
            };

            directionSegments.Add(segment);

            totalDistance += interval;
            remainingDistance -= interval;

            if (remainingDistance < 10f) break;
        }

        for (int i = 0; i < actualResolution; i++)
        {
            distance = i * step;
            float t = (float)i / (actualResolution - 1);

            DirectionSegment currentSegment = null;
            float segmentProgress = 0f;

            foreach (var seg in directionSegments)
            {
                if (distance >= seg.startDistance && distance < seg.endDistance)
                {
                    currentSegment = seg;
                    segmentProgress = (distance - seg.startDistance) / (seg.endDistance - seg.startDistance);
                    break;
                }
            }

            if (currentSegment == null && directionSegments.Count > 0)
            {
                currentSegment = directionSegments[directionSegments.Count - 1];
                segmentProgress = 1f;
            }

            float2 direction;
            float currentAngleAtPoint;

            if (currentSegment != null)
            {
                float angleProgress = Mathf.Clamp01(segmentProgress);
                float straightRatio = currentSegment.straightLength / (currentSegment.endDistance - currentSegment.startDistance);
                float straightProgress = Mathf.Min(angleProgress / straightRatio, 1f);
                float turnProgress = Mathf.Max(0f, (angleProgress - straightRatio) / (1f - straightRatio));
                float smoothAngle = currentSegment.angle * Mathf.SmoothStep(0f, 1f, turnProgress);
                float wobble = Mathf.Sin(distance * currentTurnFrequency * 0.5f) * currentTurnAmplitude * 0.15f;
                smoothAngle += wobble * 0.005f;

                currentAngleAtPoint = currentAngle + smoothAngle;
                direction = new float2(Mathf.Sin(currentAngleAtPoint), Mathf.Cos(currentAngleAtPoint));
            }
            else
            {
                direction = new float2(0f, 1f);
                currentAngleAtPoint = currentAngle;
            }

            float2 pos2D = currentPosition + direction * step * 0.5f;

            if (i < actualResolution - 1)
            {
                currentPosition += direction * step;
                currentAngle = currentAngleAtPoint;
            }

            float heightWave = Mathf.Sin(distance * currentHeightFrequency) * currentHeightAmplitude;
            float hillWave = Mathf.Sin(distance * currentHeightFrequency * 2.5f + 1.2f) * currentHeightAmplitude * 0.3f;
            float smoothFactor = Mathf.Sin(t * Mathf.PI) * 0.5f + 0.5f;
            float height = (heightWave + hillWave) * smoothFactor + currentHeightOffset;
            height = Mathf.Clamp(height, -15f, 25f);

            float steepnessFactor = 1f + Mathf.Sin(distance * 0.01f) * (currentSteepness - 1f) * 0.5f;
            float y = height * steepnessFactor;

            float3 point = new float3(pos2D.x, y, pos2D.y);
            roadPoints.Add(point);
        }

        Debug.Log($"📊 Сгенерировано {roadPoints.Count} точек, Сегментов: {directionSegments.Count}, Итоговый угол: {totalAngle:F1}°");
    }

    void GenerateRoadPoints()
    {
        int actualResolution = Mathf.Max(splineResolution, 100);
        if (roadLength > 500) actualResolution = Mathf.Max(actualResolution, 300);
        else if (roadLength > 300) actualResolution = Mathf.Max(actualResolution, 200);

        roadPoints = new List<float3>(actualResolution);
        float step = roadLength / (actualResolution - 1);

        float phase1 = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float phase2 = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float phase3 = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float phaseHeight = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        for (int i = 0; i < actualResolution; i++)
        {
            float distance = i * step;
            float t = (float)i / (actualResolution - 1);

            float x = Mathf.Sin(distance * currentTurnFrequency + phase1) * currentTurnAmplitude;
            x += Mathf.Sin(distance * currentTurnFrequency * 1.7f + phase2) * currentTurnAmplitude * 0.2f;
            x += Mathf.Sin(distance * currentTurnFrequency * 0.5f + phase3) * currentTurnAmplitude * 0.1f;
            x = Mathf.Clamp(x, -30f, 30f);

            float heightWave = Mathf.Sin(distance * currentHeightFrequency + phaseHeight) * currentHeightAmplitude;
            float hillWave = Mathf.Sin(distance * currentHeightFrequency * 2.5f + phaseHeight + 1.2f) * currentHeightAmplitude * 0.3f;
            float smoothFactor = Mathf.Sin(t * Mathf.PI) * 0.5f + 0.5f;
            float height = (heightWave + hillWave) * smoothFactor + currentHeightOffset;
            height = Mathf.Clamp(height, -15f, 25f);

            float steepnessFactor = 1f + Mathf.Sin(distance * 0.01f + phase3) * (currentSteepness - 1f) * 0.5f;
            float y = height * steepnessFactor;
            float z = distance;

            roadPoints.Add(new float3(x, y, z));
        }

        Debug.Log($"📊 Сгенерировано {roadPoints.Count} точек");
    }

    void CreateSmoothSpline()
    {
        if (splineContainer == null)
        {
            splineContainer = GetComponent<SplineContainer>();
            if (splineContainer == null)
                splineContainer = gameObject.AddComponent<SplineContainer>();
        }

        var spline = new Spline();

        int step = Mathf.Max(1, roadPoints.Count / splineResolution);
        if (step <= 1) step = 1;

        for (int i = 0; i < roadPoints.Count; i += step)
        {
            float3 pos = roadPoints[i];
            pos.y += 0.05f;

            float3 tangentIn, tangentOut;

            if (i == 0)
            {
                float3 nextPos = roadPoints[Mathf.Min(i + step, roadPoints.Count - 1)];
                tangentIn = (nextPos - pos) * splineSmoothness;
                tangentOut = (nextPos - pos) * splineSmoothness;
            }
            else if (i >= roadPoints.Count - 1)
            {
                float3 prevPos = roadPoints[Mathf.Max(i - step, 0)];
                tangentIn = (pos - prevPos) * splineSmoothness;
                tangentOut = (pos - prevPos) * splineSmoothness;
            }
            else
            {
                float3 prevPos = roadPoints[Mathf.Max(i - step, 0)];
                float3 nextPos = roadPoints[Mathf.Min(i + step, roadPoints.Count - 1)];
                float3 tangent = (nextPos - prevPos) * 0.5f;
                tangentIn = -tangent * splineSmoothness;
                tangentOut = tangent * splineSmoothness;
            }

            spline.Add(new BezierKnot(pos, tangentIn, tangentOut));
        }

        if ((roadPoints.Count - 1) % step != 0)
        {
            float3 lastPos = roadPoints[roadPoints.Count - 1];
            lastPos.y += 0.05f;
            float3 prevPos = roadPoints[Mathf.Max(roadPoints.Count - 1 - step, 0)];
            float3 tangent = (lastPos - prevPos) * splineSmoothness;
            spline.Add(new BezierKnot(lastPos, -tangent, tangent));
        }

        splineContainer.Spline = spline;

#if UNITY_EDITOR
        EditorUtility.SetDirty(splineContainer);
#endif

        Debug.Log($"🛤️ Сплайн создан: {spline.Count} узлов");
    }

    void CreateRoadFromSpline()
    {
        if (splineContainer == null || splineContainer.Spline == null)
        {
            Debug.LogError("❌ Нет сплайна!");
            return;
        }

        if (roadMeshObject != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(roadMeshObject);
            else Destroy(roadMeshObject);
#else
            Destroy(roadMeshObject);
#endif
            roadMeshObject = null;
        }

        roadMeshObject = new GameObject("Road_Mesh");
        roadMeshObject.transform.SetParent(transform);
        roadMeshObject.transform.localPosition = Vector3.zero;

#if UNITY_EDITOR
        if (!Application.isPlaying) roadMeshObject.isStatic = true;
#endif

        MeshFilter filter = roadMeshObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = roadMeshObject.AddComponent<MeshRenderer>();

        roadMaterial = CreateRoadMaterial();
        renderer.material = roadMaterial;

        int lengthPoints = Mathf.Max(splineResolution * 2, 200);
        if (roadLength > 500) lengthPoints = Mathf.Max(lengthPoints, 500);
        else if (roadLength > 300) lengthPoints = Mathf.Max(lengthPoints, 400);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();

        float halfWidth = roadWidth / 2f;
        float halfHeight = roadHeight / 2f;

        int vertsPerRow = (widthSegments + 1) * 2;

        for (int i = 0; i <= lengthPoints; i++)
        {
            float t = (float)i / lengthPoints;
            float3 position = splineContainer.Spline.EvaluatePosition(t);
            float3 tangent = splineContainer.Spline.EvaluateTangent(t);

            if (math.length(tangent) < 0.001f) tangent = new float3(0, 0, 1);
            tangent = math.normalize(tangent);

            float3 up = new float3(0f, 1f, 0f);
            float3 right = math.normalize(math.cross(tangent, up));

            if (math.lengthsq(right) < 0.001f)
            {
                right = new float3(1f, 0f, 0f);
            }

            up = math.normalize(math.cross(right, tangent));

            for (int j = 0; j <= widthSegments; j++)
            {
                float w = (float)j / widthSegments;
                float widthOffset = (w - 0.5f) * roadWidth;

                float3 pos = position + right * widthOffset + up * halfHeight;
                vertices.Add(pos);
                uv.Add(new Vector2(t, w));
                normals.Add(up);
            }

            for (int j = 0; j <= widthSegments; j++)
            {
                float w = (float)j / widthSegments;
                float widthOffset = (w - 0.5f) * roadWidth;

                float3 pos = position + right * widthOffset - up * halfHeight;
                vertices.Add(pos);
                uv.Add(new Vector2(t, w));
                normals.Add(-up);
            }
        }

        int bottomOffset = widthSegments + 1;

        for (int i = 0; i < lengthPoints; i++)
        {
            int rowStart = i * vertsPerRow;
            int nextRowStart = (i + 1) * vertsPerRow;

            for (int j = 0; j < widthSegments; j++)
            {
                int a = rowStart + j;
                int b = rowStart + j + 1;
                int c = nextRowStart + j;
                int d = nextRowStart + j + 1;

                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }

            for (int j = 0; j < widthSegments; j++)
            {
                int a = rowStart + bottomOffset + j;
                int b = rowStart + bottomOffset + j + 1;
                int c = nextRowStart + bottomOffset + j;
                int d = nextRowStart + bottomOffset + j + 1;

                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
            }

            int leftTopA = rowStart;
            int leftTopB = nextRowStart;
            int leftBottomA = rowStart + bottomOffset;
            int leftBottomB = nextRowStart + bottomOffset;

            triangles.Add(leftTopA); triangles.Add(leftBottomA); triangles.Add(leftTopB);
            triangles.Add(leftTopB); triangles.Add(leftBottomA); triangles.Add(leftBottomB);

            int rightTopA = rowStart + widthSegments;
            int rightTopB = nextRowStart + widthSegments;
            int rightBottomA = rowStart + bottomOffset + widthSegments;
            int rightBottomB = nextRowStart + bottomOffset + widthSegments;

            triangles.Add(rightTopA); triangles.Add(rightTopB); triangles.Add(rightBottomA);
            triangles.Add(rightTopB); triangles.Add(rightBottomB); triangles.Add(rightBottomA);
        }

        int startIdx = 0;
        for (int j = 0; j < widthSegments; j++)
        {
            int topA = startIdx + j;
            int topB = startIdx + j + 1;
            int bottomA = startIdx + bottomOffset + j;
            int bottomB = startIdx + bottomOffset + j + 1;

            triangles.Add(topA); triangles.Add(topB); triangles.Add(bottomA);
            triangles.Add(topB); triangles.Add(bottomB); triangles.Add(bottomA);
        }

        int endIdx = lengthPoints * vertsPerRow;
        for (int j = 0; j < widthSegments; j++)
        {
            int topA = endIdx + j;
            int topB = endIdx + j + 1;
            int bottomA = endIdx + bottomOffset + j;
            int bottomB = endIdx + bottomOffset + j + 1;

            triangles.Add(topA); triangles.Add(bottomA); triangles.Add(topB);
            triangles.Add(topB); triangles.Add(bottomA); triangles.Add(bottomB);
        }

        Mesh mesh = new Mesh();
        mesh.name = "Road3DMesh";
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.normals = normals.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.Optimize();

        filter.mesh = mesh;

        MeshCollider collider = roadMeshObject.GetComponent<MeshCollider>();
        if (collider == null) collider = roadMeshObject.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = false;

#if UNITY_EDITOR
        EditorUtility.SetDirty(roadMeshObject);
#endif

        Debug.Log($"🏗️ Меш создан: {vertices.Count} вершин, {triangles.Count / 3} треугольников");
    }

    void ClearRoadMesh()
    {
        if (roadMeshObject != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(roadMeshObject);
            else Destroy(roadMeshObject);
#else
            Destroy(roadMeshObject);
#endif
            roadMeshObject = null;
        }

        if (basePlaneMeshObject != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(basePlaneMeshObject);
            else Destroy(basePlaneMeshObject);
#else
            Destroy(basePlaneMeshObject);
#endif
            basePlaneMeshObject = null;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "Road_Mesh" || child.name == "BasePlane_Mesh")
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(child.gameObject);
                else Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }
    }

    void CreateBasePlane()
    {
        if (roadPoints == null || roadPoints.Count < 2)
        {
            Debug.LogError("❌ Нет точек для базового плэйна!");
            return;
        }

        if (basePlaneMeshObject != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(basePlaneMeshObject);
            else Destroy(basePlaneMeshObject);
#else
            Destroy(basePlaneMeshObject);
#endif
            basePlaneMeshObject = null;
        }

        basePlaneMeshObject = new GameObject("BasePlane_Mesh");
        basePlaneMeshObject.transform.SetParent(transform);
        basePlaneMeshObject.transform.localPosition = Vector3.zero;

#if UNITY_EDITOR
        if (!Application.isPlaying) basePlaneMeshObject.isStatic = true;
#endif

        MeshFilter filter = basePlaneMeshObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = basePlaneMeshObject.AddComponent<MeshRenderer>();

        basePlaneMaterial = CreateBasePlaneMaterial();
        renderer.material = basePlaneMaterial;

        Mesh mesh = new Mesh();
        mesh.name = "BasePlaneMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();

        int widthSegs = 14;
        float halfWidth = basePlaneWidth / 2f;
        float halfHeight = basePlaneHeight / 2f;
        float yOffset = basePlaneYOffset;
        float overlap = 0.5f;
        int actualPoints = roadPoints.Count;

        for (int i = 0; i < actualPoints - 1; i++)
        {
            float3 p1 = roadPoints[i];
            float3 p2 = roadPoints[i + 1];

            float3 direction = p2 - p1;
            float length = math.length(direction);

            if (length < 0.01f) continue;

            float3 forward = math.normalize(direction);
            float3 up = new float3(0f, 1f, 0f);
            float3 right = math.normalize(math.cross(forward, up));

            if (math.lengthsq(right) < 0.001f)
                right = new float3(1f, 0f, 0f);

            float3 center = (p1 + p2) / 2f;
            float halfLen = length / 2f + overlap;

            float t = (float)i / (actualPoints - 1);

            for (int j = 0; j <= widthSegs; j++)
            {
                float w = (float)j / widthSegs;
                float widthOffset = (w - 0.5f) * basePlaneWidth;

                float3 posTop = center + forward * (-halfLen) + right * widthOffset + up * (halfHeight + yOffset);
                vertices.Add(posTop);
                normals.Add(Vector3.up);
                uv.Add(new Vector2(t, w));

                float3 posBottom = center + forward * (-halfLen) + right * widthOffset + up * (-halfHeight + yOffset);
                vertices.Add(posBottom);
                normals.Add(Vector3.down);
                uv.Add(new Vector2(t, w));
            }
        }

        int vertsPerRow = widthSegs + 1;

        for (int i = 0; i < actualPoints - 2; i++)
        {
            for (int j = 0; j < widthSegs; j++)
            {
                int baseIdx = i * vertsPerRow * 2 + j * 2;

                int a = baseIdx;
                int b = baseIdx + 2;
                int c = baseIdx + vertsPerRow * 2;
                int d = baseIdx + vertsPerRow * 2 + 2;

                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);

                int a2 = baseIdx + 1;
                int b2 = baseIdx + 3;
                int c2 = baseIdx + vertsPerRow * 2 + 1;
                int d2 = baseIdx + vertsPerRow * 2 + 3;

                triangles.Add(a2); triangles.Add(b2); triangles.Add(c2);
                triangles.Add(b2); triangles.Add(d2); triangles.Add(c2);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.Optimize();

        filter.mesh = mesh;

#if UNITY_EDITOR
        EditorUtility.SetDirty(basePlaneMeshObject);
#endif

        Debug.Log($"🏗️ Базовый плэйн создан: {mesh.vertexCount} вершин");
    }

    Material CreateBasePlaneMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");

        Material mat = new Material(shader);
        mat.color = basePlaneColor;
        mat.enableInstancing = true;

        if (shader.name.Contains("Universal"))
        {
            mat.SetFloat("_Smoothness", 0.3f);
            mat.SetFloat("_Metallic", 0f);
        }

        return mat;
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

        return mat;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (showRoadPoints && roadPoints != null)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < roadPoints.Count; i += Mathf.Max(1, roadPoints.Count / 50))
            {
                Gizmos.DrawSphere(roadPoints[i], 0.3f);
            }
        }

        if (showSplinePoints && splineContainer != null && splineContainer.Spline != null)
        {
            Gizmos.color = Color.blue;
            var spline = splineContainer.Spline;
            int steps = 200;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float3 pos = spline.EvaluatePosition(t);
                Gizmos.DrawSphere(pos, 0.15f);
            }

            Gizmos.color = Color.green;
            foreach (var knot in spline.Knots)
            {
                Gizmos.DrawSphere(knot.Position, 0.5f);
            }
        }

        if (showDirectionSegments && directionSegments != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var seg in directionSegments)
            {
                float midDist = (seg.startDistance + seg.endDistance) / 2f;
                int idx = Mathf.FloorToInt(midDist / roadLength * roadPoints.Count);
                idx = Mathf.Clamp(idx, 0, roadPoints.Count - 1);
                if (idx < roadPoints.Count)
                {
                    Gizmos.DrawWireSphere(roadPoints[idx], 2f);
                    if (idx + 1 < roadPoints.Count)
                    {
                        Gizmos.DrawLine(roadPoints[idx], roadPoints[idx + 1]);
                    }
                }
            }
        }
    }

    public float3 GetRoadPoint(float distance)
    {
        if (roadPoints == null || roadPoints.Count == 0)
            return float3.zero;

        float t = Mathf.Clamp01(distance / roadLength);
        int idx = Mathf.FloorToInt(t * (roadPoints.Count - 1));
        idx = Mathf.Clamp(idx, 0, roadPoints.Count - 1);
        return roadPoints[idx];
    }

    public float GetRoadLength() => roadLength;
}