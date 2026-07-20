using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DecorationGenerator : MonoBehaviour
{
    [Header("Ссылки")]
    public SplineContainer splineContainer;

    public GameObject[] treePrefabs;
    public GameObject[] rockPrefabs;
    public GameObject[] lootPrefabs;

    [Header("🌳 ДЕРЕВЬЯ ВДОЛЬ ДОРОГИ (THE TRAIL 22 CABS)")]
    public float treesPerMeter = 3.0f;              // Очень густо
    public float treeWallWidth = 1.5f;              // Минимальный разброс
    public float treeWallOffset = 0.1f;             // Почти вплотную к дороге!
    public float treeScaleMin = 0.8f;
    public float treeScaleMax = 1.4f;
    public float treeYOffset = 0.0f;

    [Header("🌳 Второй ряд (чуть дальше)")]
    public bool useSecondRow = true;
    public float secondRowOffset = 2.5f;            // Близко к первому ряду
    public float secondRowDensity = 0.9f;

    [Header("🌳 Третий ряд (редкий)")]
    public bool useThirdRow = true;
    public float thirdRowOffset = 5.0f;
    public float thirdRowDensity = 0.3f;

    [Header("Камни и лут")]
    public float lootOffsetMin = 1.5f;
    public float lootOffsetMax = 2.5f;
    public float lootYOffset = 0.3f;
    public float lootSpacing = 25f;
    public float rockOffsetMin = 1.0f;
    public float rockOffsetMax = 2.0f;
    public float rockDensity = 0.02f;

    [Header("🎯 Проверка коллизий")]
    public float checkRadius = 1.2f;
    public LayerMask checkLayers = ~0;

    [Header("Управление")]
    public bool autoGenerateInEditor = true;
    public bool randomizeOnGenerate = true;
    public int worldSeed = 42;

    // ============================================
    // СОХРАНЕНИЕ ДЛЯ PLAY MODE
    // ============================================
    [Header("Сохранённые данные")]
    [SerializeField] private List<DecorationData> savedDecorations = new List<DecorationData>();
    [SerializeField] private bool hasSavedData = false;

    [System.Serializable]
    public class DecorationData
    {
        public Vector3 position;
        public Vector3 scale;
        public Quaternion rotation;
        public int prefabIndex;
        public string prefabType;
    }

    private List<GameObject> decorations = new List<GameObject>();
    private float roadWidth = 5f;
    private System.Random random;
    private List<float> usedLootPositions = new List<float>();
    private List<Vector3> usedTreePositions = new List<Vector3>();
    private Collider[] hitColliders = new Collider[10];
    private bool isGenerating = false;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (autoGenerateInEditor && !Application.isPlaying)
        {
            EditorApplication.delayCall += () => {
                if (this != null) RegenerateDecorations();
            };
        }
    }
#endif

    void Start()
    {
        if (splineContainer == null)
        {
            splineContainer = FindAnyObjectByType<SplineContainer>();
            if (splineContainer == null)
            {
                Debug.LogError("[DecorationGenerator] SplineContainer не найден!");
                return;
            }
        }

        // Загружаем сохранённые декорации или генерируем новые
        if (!randomizeOnGenerate && hasSavedData && savedDecorations.Count > 0)
        {
            LoadDecorations();
        }
        else
        {
            int currentSeed = Application.isPlaying ? (worldSeed + 5000) : (worldSeed + 5000 + System.Environment.TickCount);
            random = new System.Random(currentSeed);
            GenerateDecorations();
        }
    }

    public void ClearDecorations()
    {
        List<GameObject> toRemove = new List<GameObject>(decorations);
        foreach (var obj in toRemove)
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
        decorations.Clear();
        usedLootPositions.Clear();
        usedTreePositions.Clear();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            RemoveAllChildrenWithNames();
            EditorUtility.SetDirty(this);
        }
#endif
    }

    void RemoveAllChildrenWithNames()
    {
#if UNITY_EDITOR
        List<Transform> toRemove = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            string name = child.name;
            if (name.Contains("Tree") || name.Contains("Rock") || name.Contains("Loot") || name.Contains("(Clone)"))
            {
                toRemove.Add(child);
            }
        }
        foreach (var child in toRemove)
        {
            if (child != null && child.gameObject != null) DestroyImmediate(child.gameObject);
        }
#endif
    }

    // ============================================
    // ПРОВЕРКА КОЛЛИЗИЙ
    // ============================================
    bool IsPositionFree(Vector3 position, float radius)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(position, radius, hitColliders, checkLayers);
        for (int i = 0; i < hitCount; i++)
        {
            string name = hitColliders[i].gameObject.name;
            if (name.Contains("Tree") || name.Contains("Rock") || name.Contains("Loot") || name.Contains("(Clone)"))
                continue;
            if (name.Contains("Road_Mesh") || name.Contains("BasePlane_Mesh"))
                continue;
            return false;
        }

        foreach (var pos in usedTreePositions)
        {
            if (Vector3.Distance(pos, position) < radius * 1.2f)
                return false;
        }

        return true;
    }

    Vector3 GetGroundPosition(Vector3 position, float yOffset = 0f)
    {
        RaycastHit hit;
        float raycastDistance = 20f;
        Vector3 rayOrigin = new Vector3(position.x, position.y + 5f, position.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastDistance))
        {
            return new Vector3(position.x, hit.point.y + yOffset, position.z);
        }
        return position;
    }

    Vector3 ToVector3(float3 v)
    {
        return new Vector3(v.x, v.y, v.z);
    }

    // ============================================
    // 🌳 ГЛАВНАЯ ГЕНЕРАЦИЯ — THE TRAIL 22 CABS СТИЛЬ
    // ============================================

    public void GenerateDecorations()
    {
        if (isGenerating) return;
        isGenerating = true;

        if (splineContainer == null || splineContainer.Spline == null)
        {
            Debug.LogError("[DecorationGenerator] Сплайн отсутствует!");
            isGenerating = false;
            return;
        }

        ClearDecorations();
        savedDecorations.Clear();

        int currentSeed = Application.isPlaying ? (worldSeed + 5000) : (worldSeed + 5000 + System.Environment.TickCount);
        random = new System.Random(currentSeed);

        float roadLength = SplineUtility.CalculateLength(splineContainer.Spline, splineContainer.transform.localToWorldMatrix);

        if (roadLength <= 1f)
        {
            Debug.LogWarning("[DecorationGenerator] Длина дороги слишком мала!");
            isGenerating = false;
            return;
        }

        int treeCount = 0, rockCount = 0, lootCount = 0;
        int skippedCount = 0;

        // ============================================
        // 🌳 ПЕРВЫЙ РЯД — ВПЛОТНУЮ К ДОРОГЕ
        // ============================================
        int totalTrees = Mathf.Max(1, Mathf.FloorToInt(roadLength * treesPerMeter * 2f));
        float treeSegment = roadLength / totalTrees;

        for (int i = 0; i < totalTrees; i++)
        {
            float baseDist = i * treeSegment;
            float jitter = (float)(random.NextDouble() * treeSegment * 0.5f);
            float dist = Mathf.Clamp(baseDist + jitter, 0.3f, roadLength - 0.3f);

            float t = dist / roadLength;
            Vector3 pos = ToVector3(splineContainer.EvaluatePosition(t));
            Vector3 tangent = ToVector3(splineContainer.EvaluateTangent(t));

            if (pos.y > 15f || pos.y < -5f) continue;

            Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;
            if (right.magnitude < 0.001f) right = Vector3.right;

            float side = random.NextDouble() > 0.5f ? -1f : 1f;

            // 🔥 ГЛАВНОЕ: деревья прямо у дороги
            float randomOffset = (float)(random.NextDouble() - 0.5) * treeWallWidth;
            float offset = roadWidth / 2f + treeWallOffset + randomOffset;

            // Минимальное расстояние от края дороги
            if (offset < roadWidth / 2f + 0.2f)
            {
                offset = roadWidth / 2f + 0.3f + (float)random.NextDouble() * 0.5f;
            }

            Vector3 spawnPos = pos + right * side * offset + new Vector3(0f, treeYOffset, 0f);

            // Опускаем на землю
            Vector3 groundPos = GetGroundPosition(spawnPos, treeYOffset);
            spawnPos = groundPos;

            if (spawnPos.y > 15f || spawnPos.y < -3f) continue;

            // Проверка коллизий
            if (!IsPositionFree(spawnPos, checkRadius))
            {
                // Пробуем сместить
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    Vector3 testPos = spawnPos + new Vector3(
                        (float)(random.NextDouble() - 0.5) * 1.2f,
                        0f,
                        (float)(random.NextDouble() - 0.5) * 1.2f
                    );
                    if (IsPositionFree(testPos, checkRadius * 0.8f))
                    {
                        spawnPos = testPos;
                        break;
                    }
                }
                if (!IsPositionFree(spawnPos, checkRadius * 0.8f))
                {
                    skippedCount++;
                    continue;
                }
            }

            usedTreePositions.Add(spawnPos);

            if (treePrefabs.Length > 0)
            {
                int prefabIndex = random.Next(0, treePrefabs.Length);
                float scale = (float)(treeScaleMin + random.NextDouble() * (treeScaleMax - treeScaleMin));
                float rotation = (float)random.NextDouble() * 360f;

                GameObject tree = Instantiate(treePrefabs[prefabIndex], spawnPos, Quaternion.Euler(0, rotation, 0), transform);
                tree.transform.localScale = Vector3.one * scale;
                decorations.Add(tree);
                treeCount++;

                savedDecorations.Add(new DecorationData
                {
                    position = spawnPos,
                    scale = Vector3.one * scale,
                    rotation = Quaternion.Euler(0, rotation, 0),
                    prefabIndex = prefabIndex,
                    prefabType = "tree"
                });
            }
        }

        // ============================================
        // 🌳 ВТОРОЙ РЯД (чуть дальше)
        // ============================================
        if (useSecondRow && totalTrees > 0)
        {
            int secondRowCount = Mathf.Max(1, Mathf.FloorToInt(totalTrees * secondRowDensity));
            float secondRowSegment = roadLength / secondRowCount;

            for (int i = 0; i < secondRowCount; i++)
            {
                float baseDist = i * secondRowSegment;
                float jitter = (float)(random.NextDouble() * secondRowSegment * 0.5f);
                float dist = Mathf.Clamp(baseDist + jitter, 0.3f, roadLength - 0.3f);

                float t = dist / roadLength;
                Vector3 pos = ToVector3(splineContainer.EvaluatePosition(t));
                Vector3 tangent = ToVector3(splineContainer.EvaluateTangent(t));

                if (pos.y > 15f || pos.y < -5f) continue;

                Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;
                if (right.magnitude < 0.001f) right = Vector3.right;

                float side = random.NextDouble() > 0.5f ? -1f : 1f;
                float randomOffset = (float)(random.NextDouble() - 0.5) * treeWallWidth * 0.6f;
                float offset = roadWidth / 2f + secondRowOffset + randomOffset;

                if (offset < roadWidth / 2f + 0.5f)
                {
                    offset = roadWidth / 2f + 0.8f + (float)random.NextDouble() * 0.5f;
                }

                Vector3 spawnPos = pos + right * side * offset + new Vector3(0f, treeYOffset, 0f);

                Vector3 groundPos = GetGroundPosition(spawnPos, treeYOffset);
                spawnPos = groundPos;

                if (spawnPos.y > 15f || spawnPos.y < -3f) continue;

                if (!IsPositionFree(spawnPos, checkRadius * 0.7f))
                {
                    skippedCount++;
                    continue;
                }

                usedTreePositions.Add(spawnPos);

                if (treePrefabs.Length > 0)
                {
                    int prefabIndex = random.Next(0, treePrefabs.Length);
                    float scale = (float)(treeScaleMin * 0.85f + random.NextDouble() * (treeScaleMax - treeScaleMin) * 0.85f);
                    float rotation = (float)random.NextDouble() * 360f;

                    GameObject tree = Instantiate(treePrefabs[prefabIndex], spawnPos, Quaternion.Euler(0, rotation, 0), transform);
                    tree.transform.localScale = Vector3.one * scale;
                    decorations.Add(tree);
                    treeCount++;

                    savedDecorations.Add(new DecorationData
                    {
                        position = spawnPos,
                        scale = Vector3.one * scale,
                        rotation = Quaternion.Euler(0, rotation, 0),
                        prefabIndex = prefabIndex,
                        prefabType = "tree"
                    });
                }
            }
        }

        // ============================================
        // 🌳 ТРЕТИЙ РЯД (редкий, для глубины)
        // ============================================
        if (useThirdRow && totalTrees > 0)
        {
            int thirdRowCount = Mathf.Max(1, Mathf.FloorToInt(totalTrees * thirdRowDensity));
            float thirdRowSegment = roadLength / thirdRowCount;

            for (int i = 0; i < thirdRowCount; i++)
            {
                float baseDist = i * thirdRowSegment;
                float jitter = (float)(random.NextDouble() * thirdRowSegment * 0.4f);
                float dist = Mathf.Clamp(baseDist + jitter, 0.3f, roadLength - 0.3f);

                float t = dist / roadLength;
                Vector3 pos = ToVector3(splineContainer.EvaluatePosition(t));
                Vector3 tangent = ToVector3(splineContainer.EvaluateTangent(t));

                if (pos.y > 15f || pos.y < -5f) continue;

                Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;
                if (right.magnitude < 0.001f) right = Vector3.right;

                float side = random.NextDouble() > 0.5f ? -1f : 1f;
                float randomOffset = (float)(random.NextDouble() - 0.5) * treeWallWidth * 0.4f;
                float offset = roadWidth / 2f + thirdRowOffset + randomOffset;

                Vector3 spawnPos = pos + right * side * offset + new Vector3(0f, treeYOffset, 0f);

                Vector3 groundPos = GetGroundPosition(spawnPos, treeYOffset);
                spawnPos = groundPos;

                if (spawnPos.y > 15f || spawnPos.y < -3f) continue;

                if (!IsPositionFree(spawnPos, checkRadius * 0.5f)) continue;

                usedTreePositions.Add(spawnPos);

                if (treePrefabs.Length > 0)
                {
                    int prefabIndex = random.Next(0, treePrefabs.Length);
                    float scale = (float)(treeScaleMin * 0.6f + random.NextDouble() * (treeScaleMax - treeScaleMin) * 0.6f);
                    float rotation = (float)random.NextDouble() * 360f;

                    GameObject tree = Instantiate(treePrefabs[prefabIndex], spawnPos, Quaternion.Euler(0, rotation, 0), transform);
                    tree.transform.localScale = Vector3.one * scale;
                    decorations.Add(tree);
                    treeCount++;

                    savedDecorations.Add(new DecorationData
                    {
                        position = spawnPos,
                        scale = Vector3.one * scale,
                        rotation = Quaternion.Euler(0, rotation, 0),
                        prefabIndex = prefabIndex,
                        prefabType = "tree"
                    });
                }
            }
        }

        // ============================================
        // КАМНИ
        // ============================================
        int totalRocks = Mathf.Max(1, Mathf.FloorToInt(roadLength * rockDensity * 3f));
        float rockSegment = roadLength / totalRocks;

        for (int i = 0; i < totalRocks; i++)
        {
            float baseDist = i * rockSegment;
            float jitter = (float)(random.NextDouble() * rockSegment);
            float dist = Mathf.Clamp(baseDist + jitter, 0.5f, roadLength - 0.5f);

            float t = dist / roadLength;
            Vector3 pos = ToVector3(splineContainer.EvaluatePosition(t));
            Vector3 tangent = ToVector3(splineContainer.EvaluateTangent(t));

            if (pos.y > 15f || pos.y < -5f) continue;

            Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;
            if (right.magnitude < 0.001f) right = Vector3.right;

            float side = random.NextDouble() > 0.5f ? -1f : 1f;
            float offset = roadWidth / 2f + (float)(rockOffsetMin + random.NextDouble() * (rockOffsetMax - rockOffsetMin));
            Vector3 spawnPos = pos + right * side * offset + new Vector3(0f, -0.1f, 0f);

            Vector3 groundPos = GetGroundPosition(spawnPos, -0.1f);
            spawnPos = groundPos;

            if (rockPrefabs.Length > 0)
            {
                int prefabIndex = random.Next(0, rockPrefabs.Length);
                float scale = (float)(0.3f + random.NextDouble() * 0.7f);

                GameObject rock = Instantiate(rockPrefabs[prefabIndex], spawnPos,
                    Quaternion.Euler((float)random.NextDouble() * 360f, (float)random.NextDouble() * 360f, (float)random.NextDouble() * 360f), transform);
                rock.transform.localScale = Vector3.one * scale;
                decorations.Add(rock);
                rockCount++;

                savedDecorations.Add(new DecorationData
                {
                    position = spawnPos,
                    scale = Vector3.one * scale,
                    rotation = Quaternion.Euler((float)random.NextDouble() * 360f, (float)random.NextDouble() * 360f, (float)random.NextDouble() * 360f),
                    prefabIndex = prefabIndex,
                    prefabType = "rock"
                });
            }
        }

        // ============================================
        // ЛУТ
        // ============================================
        int totalLoot = Mathf.Max(1, Mathf.FloorToInt(roadLength / lootSpacing));

        for (int i = 0; i < totalLoot; i++)
        {
            float baseDist = (float)i / totalLoot * roadLength;
            float jitter = (float)(random.NextDouble() - 0.5) * (lootSpacing * 0.5f);
            float dist = Mathf.Clamp(baseDist + jitter, 1f, roadLength - 1f);

            bool tooClose = false;
            foreach (float usedDist in usedLootPositions)
            {
                if (Mathf.Abs(dist - usedDist) < lootSpacing * 0.4f)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            float t = dist / roadLength;
            Vector3 pos = ToVector3(splineContainer.EvaluatePosition(t));
            Vector3 tangent = ToVector3(splineContainer.EvaluateTangent(t));

            if (pos.y > 15f || pos.y < -5f) continue;

            Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;
            if (right.magnitude < 0.001f) right = Vector3.right;

            float side = random.NextDouble() > 0.5f ? -1f : 1f;
            float lootOffset = (float)(lootOffsetMin + random.NextDouble() * (lootOffsetMax - lootOffsetMin));
            Vector3 spawnPos = pos + right * side * lootOffset + new Vector3(0f, lootYOffset, 0f);

            Vector3 groundPos = GetGroundPosition(spawnPos, lootYOffset);
            spawnPos = groundPos;

            if (lootPrefabs.Length > 0)
            {
                int prefabIndex = random.Next(0, lootPrefabs.Length);
                GameObject loot = Instantiate(lootPrefabs[prefabIndex], spawnPos, Quaternion.identity, transform);

                var interactable = loot.GetComponent<LootInteractable>();
                if (interactable != null)
                {
                    interactable.Initialize("Сокровище");
                }

                decorations.Add(loot);
                usedLootPositions.Add(dist);
                lootCount++;

                savedDecorations.Add(new DecorationData
                {
                    position = spawnPos,
                    scale = Vector3.one,
                    rotation = Quaternion.identity,
                    prefabIndex = prefabIndex,
                    prefabType = "loot"
                });
            }
        }

        hasSavedData = true;

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif

        Debug.Log($"[DecorationGenerator] 🌳 Деревья: {treeCount}, Камни: {rockCount}, Лут: {lootCount}, Пропущено: {skippedCount}");
        isGenerating = false;
    }

    // ============================================
    // ЗАГРУЗКА СОХРАНЁННЫХ ДЕКОРАЦИЙ
    // ============================================

    private void LoadDecorations()
    {
        Debug.Log($"[DecorationGenerator] 📂 Загрузка {savedDecorations.Count} декораций...");

        foreach (var data in savedDecorations)
        {
            GameObject[] prefabs = GetPrefabArray(data.prefabType);
            if (prefabs == null || data.prefabIndex >= prefabs.Length) continue;

            GameObject prefab = prefabs[data.prefabIndex];
            if (prefab == null) continue;

            GameObject obj = Instantiate(prefab, data.position, data.rotation, transform);
            obj.transform.localScale = data.scale;
            decorations.Add(obj);
        }

        Debug.Log($"[DecorationGenerator] 📂 Загружено {decorations.Count} декораций");
    }

    private GameObject[] GetPrefabArray(string type)
    {
        switch (type)
        {
            case "tree": return treePrefabs;
            case "rock": return rockPrefabs;
            case "loot": return lootPrefabs;
            default: return null;
        }
    }

    // ============================================
    // EDITOR КОМАНДЫ
    // ============================================

#if UNITY_EDITOR
    [ContextMenu("Regenerate Decorations")]
    public void RegenerateDecorations()
    {
        Debug.Log("[DecorationGenerator] 🔄 Перегенерация...");
        randomizeOnGenerate = true;
        GenerateDecorations();
        EditorUtility.SetDirty(this);
    }

    [ContextMenu("Clear All Decorations")]
    public void ClearAllDecorations()
    {
        Debug.Log("[DecorationGenerator] 🗑️ Очистка...");
        ClearDecorations();
        savedDecorations.Clear();
        hasSavedData = false;
        EditorUtility.SetDirty(this);
    }

    [ContextMenu("Save Current Decorations")]
    public void SaveCurrentDecorations()
    {
        if (decorations.Count == 0)
        {
            Debug.LogWarning("[DecorationGenerator] Нет декораций для сохранения!");
            return;
        }

        savedDecorations.Clear();

        foreach (var obj in decorations)
        {
            if (obj == null) continue;

            string name = obj.name;
            string type = "tree";
            if (name.Contains("Rock")) type = "rock";
            else if (name.Contains("Loot")) type = "loot";

            int prefabIndex = -1;
            GameObject[] prefabs = GetPrefabArray(type);
            if (prefabs != null)
            {
                string cleanName = name.Replace("(Clone)", "").Trim();
                for (int i = 0; i < prefabs.Length; i++)
                {
                    if (prefabs[i] != null && prefabs[i].name == cleanName)
                    {
                        prefabIndex = i;
                        break;
                    }
                }
            }

            if (prefabIndex == -1) continue;

            savedDecorations.Add(new DecorationData
            {
                position = obj.transform.position,
                scale = obj.transform.localScale,
                rotation = obj.transform.rotation,
                prefabIndex = prefabIndex,
                prefabType = type
            });
        }

        hasSavedData = true;
        EditorUtility.SetDirty(this);
        Debug.Log($"[DecorationGenerator] 💾 Сохранено {savedDecorations.Count} декораций");
    }

    void OnDrawGizmosSelected()
    {
        if (splineContainer == null || splineContainer.Spline == null) return;

        Gizmos.color = new Color(0, 1, 0, 0.3f);
        float roadLength = SplineUtility.CalculateLength(splineContainer.Spline, splineContainer.transform.localToWorldMatrix);

        // Показываем зону посадки деревьев
        for (float dist = 0; dist < roadLength; dist += 3f)
        {
            float t = dist / roadLength;
            Vector3 pos = ToVector3(splineContainer.EvaluatePosition(t));
            Vector3 tangent = ToVector3(splineContainer.EvaluateTangent(t));
            Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;
            if (right.magnitude < 0.001f) right = Vector3.right;

            // Левая сторона
            Vector3 leftPos = pos + right * (roadWidth / 2f + 0.5f);
            Gizmos.DrawWireSphere(leftPos, 0.5f);

            // Правая сторона
            Vector3 rightPos = pos - right * (roadWidth / 2f + 0.5f);
            Gizmos.DrawWireSphere(rightPos, 0.5f);
        }
    }
#endif

    void OnDestroy()
    {
        ClearDecorations();
    }
}