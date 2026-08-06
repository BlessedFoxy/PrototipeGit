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

    [Header("Возврат на дорогу")]
    public float returnToRoadSpeed = 2f;

    [Header("Текущая скорость")]
    public float currentSpeed;

    // Флаг блокировки от инвентаря
    public bool isInventoryOpen = false;

    private Animator animator;
    private float targetSpeed;
    private float currentDistance = 0f;
    private float splineLength;

    private bool _isSitting = false;
    private bool _isReturningToRoad = false;

    void Start()
    {
        if (trailSpline == null)
        {
            Debug.LogError("Trail Spline не назначен!");
            return;
        }

        animator = GetComponent<Animator>();
        splineLength = CalculateSplineLength();
        targetSpeed = defaultWalkSpeed;
        currentSpeed = defaultWalkSpeed;

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

        // ✅ ЕСЛИ ИНВЕНТАРЬ ОТКРЫТ — МГНОВЕННО ОСТАНАВЛИВАЕМ ВСЁ ДВИЖЕНИЕ
        if (isInventoryOpen)
        {
            currentSpeed = 0f;
            targetSpeed = 0f;

            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetBool("IsWalking", false);
            }
            return;
        }

        // Если сидит, скрипт полностью отключен
        if (_isSitting) return;

        // 1. УПРАВЛЕНИЕ
        if (!_isReturningToRoad)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                targetSpeed = Input.GetKey(KeyCode.LeftShift) ? maxRunSpeed : defaultWalkSpeed;
            }
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                targetSpeed = 0f;
            }
        }
        else
        {
            targetSpeed = 0f;
        }

        // 2. ПЛАВНОЕ ИЗМЕНЕНИЕ СКОРОСТИ
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

        // 3. ДВИЖЕНИЕ ПО СПЛАЙНУ
        if (!_isReturningToRoad)
        {
            currentDistance += currentSpeed * Time.deltaTime;
            if (currentDistance >= splineLength) currentDistance = 0f;
        }

        // 4. ПОЗИЦИЯ И ПОВОРОТ
        float t = currentDistance / splineLength;
        float3 position = trailSpline.Spline.EvaluatePosition(t);
        float3 tangent = trailSpline.Spline.EvaluateTangent(t);

        Vector3 targetSplinePos = (Vector3)position + Vector3.up * heightOffset;

        if (_isReturningToRoad)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetSplinePos, returnToRoadSpeed * Time.deltaTime);

            Vector3 flatTangent = new Vector3(tangent.x, 0f, tangent.z).normalized;
            if (flatTangent != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatTangent), Time.deltaTime * 5f);
            }

            if (Vector3.Distance(transform.position, targetSplinePos) <= 0.05f)
            {
                _isReturningToRoad = false;
                transform.position = targetSplinePos;
                targetSpeed = defaultWalkSpeed;
            }
        }
        else
        {
            transform.position = targetSplinePos;

            Vector3 flatTangent = new Vector3(tangent.x, 0f, tangent.z).normalized;
            if (flatTangent != Vector3.zero)
            {
                transform.forward = flatTangent;
            }
        }

        // 5. АНИМАЦИЯ
        if (animator != null)
        {
            float speedForAnim = Mathf.Abs(currentSpeed);
            if (_isReturningToRoad) speedForAnim = returnToRoadSpeed;

            animator.SetFloat("Speed", speedForAnim);
            animator.SetBool("IsWalking", speedForAnim > 0.05f);

            if (speedForAnim < 0.05f && !animator.GetBool("IsSitting"))
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
    // ПУБЛИЧНЫЕ МЕТОДЫ
    // ============================================

    // Метод для открытия/закрытия инвентаря извне
    public void SetInventoryOpen(bool isOpen)
    {
        isInventoryOpen = isOpen;

        if (isOpen)
        {
            // При открытии всё останавливаем
            currentSpeed = 0f;
            targetSpeed = 0f;
            _isReturningToRoad = false; // Отменяем возврат на дорогу, если он был

            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetBool("IsWalking", false);
            }
        }
        else
        {
            // При закрытии сбрасываем флаги и даём команду идти
            _isReturningToRoad = false;
            targetSpeed = defaultWalkSpeed;

            if (animator != null)
            {
                animator.SetFloat("Speed", currentSpeed);
                animator.SetBool("IsWalking", currentSpeed > 0.05f);
            }
        }
    }

    public void SetSpeed(float speed)
    {
        targetSpeed = Mathf.Clamp(speed, 0f, maxRunSpeed);
    }

    public void SetSitting(bool sitting)
    {
        _isSitting = sitting;
        if (animator != null)
        {
            animator.SetBool("IsSitting", sitting);
        }
    }

    public void StartReturningToRoad()
    {
        _isSitting = false;
        _isReturningToRoad = true;
        currentSpeed = 0f;
        targetSpeed = 0f;
    }

    public float GetCurrentDistance() { return currentDistance; }

    public void SetDistance(float distance)
    {
        currentDistance = Mathf.Clamp(distance, 0f, splineLength);
    }

    public float GetProgress()
    {
        if (splineLength <= 0f) return 0f;
        return Mathf.Clamp01(currentDistance / splineLength);
    }

    public float GetCurrentSpeed() { return currentSpeed; }
    public float GetSplineLength() { return splineLength; }

    [ContextMenu("Reset Position To Start")]
    public void ResetPositionToStart()
    {
        currentDistance = 0f;
        Debug.Log("[TrailWalker] 🔄 Сброс на старт");
    }
}