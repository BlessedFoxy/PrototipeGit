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

    private Animator animator;                    // ← ДОБАВЛЕНО
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

        // ← ДОБАВЛЕНО: получаем Animator
        animator = GetComponent<Animator>();

        splineLength = CalculateSplineLength();
        targetSpeed = defaultWalkSpeed;
        currentSpeed = defaultWalkSpeed;

        // ← ДОБАВЛЕНО: начальная настройка анимаций
        if (animator != null)
        {
            animator.SetBool("IsSitting", false);
            animator.SetBool("IsWalking", true);
            animator.SetFloat("Speed", currentSpeed);
        }

        Debug.Log($"Длина тропы: {splineLength} метров");
    }

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

        // ============================================
        // 1. УПРАВЛЕНИЕ (НЕ ТРОГАЮ!)
        // ============================================
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            targetSpeed = Input.GetKey(KeyCode.LeftShift) ? maxRunSpeed : defaultWalkSpeed;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            targetSpeed = 0f;
        }

        // ============================================
        // 2. ПЛАВНОЕ ИЗМЕНЕНИЕ СКОРОСТИ (НЕ ТРОГАЮ!)
        // ============================================
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

        // ============================================
        // 3. ДВИЖЕНИЕ ПО СПЛАЙНУ (НЕ ТРОГАЮ!)
        // ============================================
        currentDistance += currentSpeed * Time.deltaTime;

        if (currentDistance >= splineLength)
        {
            currentDistance = 0f;
        }

        // ============================================
        // 4. ПОЗИЦИЯ И ПОВОРОТ (НЕ ТРОГАЮ!)
        // ============================================
        float t = currentDistance / splineLength;

        float3 position = trailSpline.Spline.EvaluatePosition(t);
        float3 tangent = trailSpline.Spline.EvaluateTangent(t);

        transform.position = (Vector3)position + Vector3.up * heightOffset;

        Vector3 flatTangent = new Vector3(tangent.x, 0f, tangent.z).normalized;
        if (flatTangent != Vector3.zero)
        {
            transform.forward = flatTangent;
        }

        // ============================================
        // 5. 🔥 ТОЛЬКО ДОБАВЛЕНО: УПРАВЛЕНИЕ АНИМАЦИЕЙ
        // ============================================
        if (animator != null)
        {
            float speed = Mathf.Abs(currentSpeed);

            // Обновляем параметры анимации
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsWalking", speed > 0.05f);

            // Если скорость = 0 и мы не сидим → переходим в Idle
            if (speed < 0.05f && !animator.GetBool("IsSitting"))
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Walk"))
                {
                    animator.Play("Idle", 0, 0f);
                }
            }
        }
    }

    // ============================================
    // ПУБЛИЧНЫЕ МЕТОДЫ (ДОБАВЛЕНЫ ДЛЯ КОСТРА)
    // ============================================

    public void SetSpeed(float speed)
    {
        targetSpeed = Mathf.Clamp(speed, 0f, maxRunSpeed);
    }

    public void SetSitting(bool sitting)
    {
        if (animator != null)
        {
            animator.SetBool("IsSitting", sitting);
        }
    }

    public float GetCurrentSpeed() => currentSpeed;
    public float GetCurrentDistance()
    {
        return currentDistance;
    }
    public void SetDistance(float distance)
    {
        currentDistance = Mathf.Clamp(distance, 0f, splineLength);
        Debug.Log($"[TrailWalker] Дистанция установлена: {currentDistance:F1}m");
    }
}

