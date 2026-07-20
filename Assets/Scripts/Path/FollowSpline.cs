using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class FollowSpline : MonoBehaviour
{
    [Header("Ссылки")]
    public SplineContainer splineContainer;

    [Header("Настройки движения")]
    public float speed = 5f;
    public float acceleration = 3f;
    public float deceleration = 5f;
    public bool autoStart = true;

    [Header("Настройки поворота")]
    public float rotationSpeed = 5f;
    public bool rotateToDirection = true;

    [Header("Отладка")]
    public float currentDistance = 0f;
    public float currentSpeed = 0f;
    public bool isFollowing = false;
    public float totalSplineLength = 0f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Start()
    {
        // Поиск SplineContainer
        if (splineContainer == null)
        {
            splineContainer = FindAnyObjectByType<SplineContainer>();
            if (splineContainer == null)
            {
                Debug.LogError("SplineContainer не найден! Создайте GameObject с компонентом SplineContainer.");
                enabled = false;
                return;
            }
            Debug.Log($"Найден SplineContainer на {splineContainer.gameObject.name}");
        }

        // Проверка, что сплайн имеет точки
        if (splineContainer.Spline == null || splineContainer.Spline.Count == 0)
        {
            Debug.LogError("Spline не имеет узлов! Добавьте точки через Edit Spline.");
            enabled = false;
            return;
        }

        // Расчет длины сплайна
        totalSplineLength = CalculateSplineLength();

        if (autoStart)
        {
            StartFollowing();
        }

        // Начальная позиция
        UpdatePositionOnSpline(0f);
    }

    void Update()
    {
        if (!isFollowing || splineContainer.Spline == null) return;

        // Управление скоростью через клавиши
        float targetSpeed = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            targetSpeed = speed;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            targetSpeed = -speed * 0.5f; // Назад медленнее
        }

        // Плавное ускорение/замедление
        if (Mathf.Abs(targetSpeed) > 0.1f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }

        // Обновление дистанции
        currentDistance += currentSpeed * Time.deltaTime;

        // Зацикливание (если нужно)
        if (currentDistance > totalSplineLength)
        {
            currentDistance = 0f;
        }
        else if (currentDistance < 0f)
        {
            currentDistance = totalSplineLength;
        }

        // Обновление позиции
        UpdatePositionOnSpline(currentDistance);
    }

    void UpdatePositionOnSpline(float distance)
    {
        if (splineContainer.Spline == null || totalSplineLength == 0) return;

        // Нормализуем дистанцию в t (0-1)
        float t = Mathf.Clamp01(distance / totalSplineLength);

        // Получаем позицию и касательную
        float3 position = splineContainer.Spline.EvaluatePosition(t);
        float3 tangent = splineContainer.Spline.EvaluateTangent(t);

        targetPosition = position;

        // Применяем позицию
        transform.position = targetPosition;

        // Поворот
        if (rotateToDirection && math.lengthsq(tangent) > 0.001f)
        {
            // Игнорируем наклон по Y для стабильности
            Vector3 flatTangent = new Vector3(tangent.x, 0f, tangent.z).normalized;
            if (flatTangent != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(flatTangent, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    float CalculateSplineLength()
    {
        if (splineContainer.Spline == null || splineContainer.Spline.Count < 2) return 0f;

        float totalLength = 0f;
        int segments = splineContainer.Spline.Count * 10;

        for (int i = 0; i < segments; i++)
        {
            float t1 = (float)i / segments;
            float t2 = (float)(i + 1) / segments;

            float3 p1 = splineContainer.Spline.EvaluatePosition(t1);
            float3 p2 = splineContainer.Spline.EvaluatePosition(t2);

            totalLength += math.distance(p1, p2);
        }

        return totalLength;
    }

    // Публичные методы
    public void StartFollowing()
    {
        isFollowing = true;
        currentSpeed = 0f;
    }

    public void StopFollowing()
    {
        isFollowing = false;
        currentSpeed = 0f;
    }

    public void ResetPosition()
    {
        currentDistance = 0f;
        isFollowing = false;
        currentSpeed = 0f;
        UpdatePositionOnSpline(0f);
    }

    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0f, newSpeed);
    }

    public Vector3 GetTargetPosition() => targetPosition;
    public float GetCurrentDistance() => currentDistance;
    public float GetNormalizedDistance() => totalSplineLength > 0 ? currentDistance / totalSplineLength : 0f;
    public bool IsFollowing() => isFollowing;

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        if (splineContainer == null || splineContainer.Spline == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i <= 100; i++)
        {
            float t = (float)i / 100;
            Vector3 point = splineContainer.Spline.EvaluatePosition(t);
            Gizmos.DrawSphere(point, 0.2f);
        }

        // Показываем текущую позицию
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetPosition, 0.5f);
    }
}