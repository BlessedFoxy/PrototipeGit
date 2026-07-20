using UnityEngine;

public class TrailCamera : MonoBehaviour
{
    [Header("Цель")]
    public Transform target;

    [Header("Позиция камеры (левое плечо)")]
    public Vector3 shoulderOffset = new Vector3(-2.5f, 2.5f, -4f);  // ← ЛЕВОЕ ПЛЕЧО
    public float lookAheadDistance = 5f;                             // ← СМОТРИТ ВПЕРЁД
    public float smoothSpeed = 5f;

    [Header("Осмотр (ПКМ)")]
    public float lookSensitivity = 2f;
    public float maxLookAngle = 30f;
    public float autoReturnSpeed = 3f;

    private float currentLookX = 0f;
    private Vector3 velocityRef = Vector3.zero;
    private bool isLooking = false;

    void Start()
    {
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (target == null)
            {
                Debug.LogError("❌ Camera target not found!");
                enabled = false;
                return;
            }
        }

        transform.position = target.position + shoulderOffset;
        transform.LookAt(target.position + target.forward * lookAheadDistance + Vector3.up * 1.5f);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ============================================
        // ОСМОТР (ПРАВАЯ КНОПКА МЫШИ)
        // ============================================
        float lookInput = 0f;

        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            if (Mathf.Abs(mouseX) > 0.1f)
            {
                lookInput = mouseX * lookSensitivity;
                isLooking = true;
            }
        }
        else
        {
            isLooking = false;
        }

        // Тач (мобилки) — правая половина экрана
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved && touch.position.x > Screen.width / 2f)
            {
                float touchDelta = touch.deltaPosition.x * 0.01f * lookSensitivity;
                if (Mathf.Abs(touchDelta) > 0.1f)
                {
                    lookInput = touchDelta;
                    isLooking = true;
                }
            }
        }

        // Применяем осмотр
        if (isLooking)
        {
            currentLookX += lookInput;
            currentLookX = Mathf.Clamp(currentLookX, -maxLookAngle, maxLookAngle);
        }
        else
        {
            if (Mathf.Abs(currentLookX) > 0.1f)
            {
                currentLookX = Mathf.Lerp(currentLookX, 0f, autoReturnSpeed * Time.deltaTime);
            }
            else
            {
                currentLookX = 0f;
            }
        }

        // ============================================
        // ПОЗИЦИЯ КАМЕРЫ (ЛЕВОЕ ПЛЕЧО)
        // ============================================

        // Поворот персонажа
        Vector3 targetForward = target.forward;
        Vector3 targetRight = target.right;
        Vector3 targetUp = Vector3.up;

        // Базовое смещение (левое плечо)
        Vector3 baseOffset = shoulderOffset;

        // Добавляем осмотр (небольшое смещение влево-вправо)
        float lookOffsetX = currentLookX * 0.02f;
        Vector3 lookOffset = targetRight * lookOffsetX;

        // Мировые координаты
        Vector3 worldOffset = targetForward * baseOffset.z
                            + targetRight * baseOffset.x
                            + targetUp * baseOffset.y
                            + lookOffset;

        Vector3 desiredPosition = target.position + worldOffset;

        // Плавное движение
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocityRef,
            1f / smoothSpeed
        );

        // ============================================
        // КАМЕРА СМОТРИТ ВПЕРЁД ПО ТРОПЕ
        // ============================================

        // Точка взгляда — вперёд по направлению движения
        Vector3 lookTarget = target.position + targetForward * lookAheadDistance + Vector3.up * 1.5f;

        // При осмотре — смещаем точку взгляда
        if (Mathf.Abs(currentLookX) > 0.5f)
        {
            lookTarget += targetRight * currentLookX * 0.3f;
        }

        transform.LookAt(lookTarget);
    }

    public void ResetLook()
    {
        currentLookX = 0f;
        isLooking = false;
    }
}