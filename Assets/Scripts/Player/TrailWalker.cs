using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class TrailWalker : MonoBehaviour
{
    [Header("Настройки")]
    public SplineContainer trailSpline;
    public float defaultWalkSpeed = 2f;
    public float maxRunSpeed = 6f;
    public float heightOffset = 0.5f;

    [Header("Текущая скорость")]
    public float currentSpeed;

    private float targetSpeed;
    private float currentDistance = 0f;
    private float splineLength;

    void Start()
    {
        if (trailSpline == null)
        {
            Debug.LogError("Trail Spline не назначен!");
            return;
        }

        // ✅ ИСПРАВЛЕНО: расчёт длины сплайна
        splineLength = CalculateSplineLength();
        targetSpeed = defaultWalkSpeed;
        currentSpeed = defaultWalkSpeed;
        Debug.Log($"Длина тропы: {splineLength} метров");
    }

    // ✅ НОВЫЙ МЕТОД ДЛЯ РАСЧЁТА ДЛИНЫ СПЛАЙНА
    private float CalculateSplineLength()
    {
        if (trailSpline == null || trailSpline.Spline == null) return 100f;

        var spline = trailSpline.Spline;
        if (spline.Count < 2) return 100f;

        float total = 0f;
        int segments = 200;

        for (int i = 0; i < segments; i++)
        {
            float t1 = (float)i / segments;
            float t2 = (float)(i + 1) / segments;

            float3 p1 = spline.EvaluatePosition(t1);
            float3 p2 = spline.EvaluatePosition(t2);

            total += math.distance(p1, p2);
        }

        return total > 1f ? total : 100f;
    }

    void Update()
    {
        if (trailSpline == null || splineLength <= 0f) return;

        // 1. Управление с клавиатуры (для тестов)
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            targetSpeed = Input.GetKey(KeyCode.LeftShift) ? maxRunSpeed : defaultWalkSpeed;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            targetSpeed = 0f;
        }

        // 2. Плавное изменение скорости
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

        // 3. Движение по сплайну
        currentDistance += currentSpeed * Time.deltaTime;

        // Зацикливание
        if (currentDistance >= splineLength)
        {
            currentDistance = 0f;
        }

        // 4. Получаем позицию и направление
        float t = currentDistance / splineLength;

        // ✅ ИСПРАВЛЕНО: используем EvaluatePosition и EvaluateTangent
        float3 position = trailSpline.Spline.EvaluatePosition(t);
        float3 tangent = trailSpline.Spline.EvaluateTangent(t);

        // Двигаем персонажа
        transform.position = (Vector3)position + Vector3.up * heightOffset;

        // Поворот
        Vector3 flatTangent = new Vector3(tangent.x, 0f, tangent.z).normalized;
        if (flatTangent != Vector3.zero)
        {
            transform.forward = flatTangent;
        }
    }

    // === МЕТОД ДЛЯ УПРАВЛЕНИЯ СКОРОСТЬЮ ИЗ CAMERA FOLLOW ===
    public void SetSpeed(float speed)
    {
        targetSpeed = Mathf.Clamp(speed, 0f, maxRunSpeed);
    }

    public float GetCurrentSpeed() => currentSpeed;
    public float GetCurrentDistance() => currentDistance;
}