using UnityEngine;

/// <summary>
/// Этот скрипт корректирует высоту персонажа,
/// привязывая его к поверхности ландшафта.
/// НЕ влияет на движение — только на высоту!
/// </summary>
[RequireComponent(typeof(TrailWalker))]
public class TrailHeightAdjuster : MonoBehaviour
{
    [Header("Настройки Raycast")]
    public LayerMask groundLayer = -1;        // Все слои
    public float raycastDistance = 30f;       // Дальность луча
    public float heightOffset = 0.5f;         // Смещение над землёй
    public float smoothSpeed = 8f;            // Плавность прилипания

    [Header("Отладка")]
    public bool showDebugRay = true;

    private TrailWalker trailWalker;
    private Transform targetTransform;
    private float currentHeight = 0f;
    private Vector3 currentPosition;
    private float lastUpdateTime = 0f;

    void Start()
    {
        trailWalker = GetComponent<TrailWalker>();
        targetTransform = transform;
        currentHeight = targetTransform.position.y;
        currentPosition = targetTransform.position;
    }

    void Update()
    {
        // Проверяем, не слишком ли часто обновляем (оптимизация)
        if (Time.time - lastUpdateTime < 0.02f) return;
        lastUpdateTime = Time.time;

        // Получаем текущую позицию от TrailWalker (или напрямую)
        Vector3 currentPos = targetTransform.position;

        // Запускаем луч вниз
        Vector3 rayOrigin = new Vector3(currentPos.x, currentPos.y + 10f, currentPos.z);

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastDistance, groundLayer))
        {
            // Нашли поверхность — плавно прилипаем
            float targetHeight = hit.point.y + heightOffset;
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, smoothSpeed * Time.deltaTime);

            // Визуализация
            if (showDebugRay)
            {
                Debug.DrawLine(rayOrigin, hit.point, Color.green);
                Debug.DrawLine(hit.point, new Vector3(hit.point.x, targetHeight, hit.point.z), Color.yellow);
            }
        }
        else
        {
            // Не нашли поверхность — используем текущую высоту
            currentHeight = Mathf.Lerp(currentHeight, currentPos.y, smoothSpeed * Time.deltaTime);

            if (showDebugRay)
            {
                Debug.DrawLine(rayOrigin, new Vector3(currentPos.x, currentPos.y - 10f, currentPos.z), Color.red);
            }
        }

        // Применяем только высоту! Остальные координаты НЕ трогаем.
        targetTransform.position = new Vector3(
            currentPos.x,      // X — как у TrailWalker
            currentHeight,     // Y — скорректированный
            currentPos.z       // Z — как у TrailWalker
        );
    }

    // Метод для ручного обновления (если нужно)
    public void SnapToGround()
    {
        Vector3 currentPos = targetTransform.position;
        Vector3 rayOrigin = new Vector3(currentPos.x, currentPos.y + 10f, currentPos.z);

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastDistance, groundLayer))
        {
            currentHeight = hit.point.y + heightOffset;
            targetTransform.position = new Vector3(currentPos.x, currentHeight, currentPos.z);
        }
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        if (!showDebugRay) return;

        Vector3 currentPos = transform.position;
        Vector3 rayOrigin = new Vector3(currentPos.x, currentPos.y + 10f, currentPos.z);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(rayOrigin, 0.3f);
        Gizmos.DrawLine(rayOrigin, new Vector3(rayOrigin.x, rayOrigin.y - raycastDistance, rayOrigin.z));
    }
}