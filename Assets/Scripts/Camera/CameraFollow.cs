using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Цель")]
    [SerializeField] private Transform target;

    [Header("Настройки позиции")]
    [SerializeField] private float distance = 4f;
    [SerializeField] private float height = 1f;
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Вращение камеры")]
    [SerializeField] private float rotationSpeed = 300f;
    [SerializeField] private float currentAngle = 0f;

    [Header("Автовозврат")]
    [SerializeField] private float returnDelay = 2f;
    [SerializeField] private float returnSpeed = 3f;
    [SerializeField] private float targetAngle = 0f;

    [Header("Чувствительность тача/мыши")]
    [SerializeField] private float touchSensitivity = 0.03f;
    [SerializeField] private float minSwipeDistance = 5f;

    [Header("Управление скоростью (тач)")]
    [SerializeField] private float speedSensitivity = 0.05f; // Увеличьте до 0.1f, если медленно
    [SerializeField] private float minSpeed = 0f;
    [SerializeField] private float maxSpeed = 6f;            // Должно совпадать с maxRunSpeed в TrailWalker
    [SerializeField] private float currentSpeed = 2f;
    [SerializeField] private float targetSpeed = 2f;

    private Vector3 currentVelocity = Vector3.zero;
    private float lastInputTime = 0f;
    private bool isReturning = false;

    private bool isInteracting = false;
    private Vector2 interactionStartPos;

    private TrailWalker trailWalker;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        if (target != null)
        {
            trailWalker = target.GetComponent<TrailWalker>();
            if (trailWalker == null)
            {
                trailWalker = target.GetComponentInChildren<TrailWalker>();
            }
            
            if (trailWalker != null)
            {
                // ИСПРАВЛЕНО: используем defaultWalkSpeed вместо несуществующего baseSpeed
                currentSpeed = trailWalker.defaultWalkSpeed;
                targetSpeed = currentSpeed;
                Debug.Log("[Camera] TrailWalker найден и подключен!");
            }
            else
            {
                Debug.LogWarning("[Camera] TrailWalker не найден на персонаже!");
            }
        }

        currentAngle = 0f;
        UpdateCameraPosition();
    }

    private void Update()
    {
        if (target == null) return;
        HandleKeyboardInput();
        HandleTouchAndMouseInput();
    }

    private void LateUpdate()
    {
        if (target == null) return;
        HandleAutoReturn();
        UpdateCameraPosition();
    }

    private void HandleKeyboardInput()
    {
        // ПОВОРОТ (A/D)
        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) rotationInput = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) rotationInput = 1f;

        if (rotationInput != 0)
        {
            currentAngle += rotationInput * rotationSpeed * Time.deltaTime;
            lastInputTime = Time.time;
            isReturning = false;
        }

        // СКОРОСТЬ (W/S) - только если есть TrailWalker
        if (trailWalker != null)
        {
            float speedInput = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) speedInput = 1f;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) speedInput = -1f;

            if (speedInput != 0)
            {
                targetSpeed += speedInput * 3f * Time.deltaTime;
                targetSpeed = Mathf.Clamp(targetSpeed, minSpeed, maxSpeed);
                trailWalker.SetSpeed(targetSpeed);
                currentSpeed = targetSpeed;
            }
        }
    }

    private void HandleTouchAndMouseInput()
    {
        bool isPressed = false;
        Vector2 currentPos = Vector2.zero;

        // 1. Проверка тача
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                interactionStartPos = touch.position;
                isInteracting = true;
                isPressed = true;
            }
            else if (touch.phase == TouchPhase.Moved && isInteracting)
            {
                currentPos = touch.position;
                isPressed = true;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isInteracting = false;
            }
        }
        // 2. Проверка мыши (для тестов в редакторе)
        else if (Input.GetMouseButton(0))
        {
            if (Input.GetMouseButtonDown(0))
            {
                interactionStartPos = Input.mousePosition;
                isInteracting = true;
                isPressed = true;
            }
            else
            {
                currentPos = Input.mousePosition;
                isPressed = true;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isInteracting = false;
        }

        // 3. Логика вращения (горизонтальный свайп)
        if (isPressed && isInteracting)
        {
            float deltaX = currentPos.x - interactionStartPos.x;

            if (Mathf.Abs(deltaX) > minSwipeDistance)
            {
                float rotationDelta = deltaX * touchSensitivity;
                currentAngle += rotationDelta;
                lastInputTime = Time.time;
                isReturning = false;
                interactionStartPos = currentPos; // Сброс для непрерывного вращения
            }
        }

        // 4. Логика скорости (вертикальный свайп)
        if (trailWalker != null && isInteracting && isPressed)
        {
            float deltaY = currentPos.y - interactionStartPos.y;

            if (Mathf.Abs(deltaY) > minSwipeDistance)
            {
                // deltaY > 0 (свайп вверх) = ускоряемся
                // deltaY < 0 (свайп вниз) = замедляемся
                float speedDelta = deltaY * speedSensitivity;
                targetSpeed += speedDelta;
                targetSpeed = Mathf.Clamp(targetSpeed, minSpeed, maxSpeed);
                
                trailWalker.SetSpeed(targetSpeed);
                currentSpeed = targetSpeed;
                
                // Сброс для непрерывного изменения скорости
                interactionStartPos = currentPos;
            }
        }
    }

    private void HandleAutoReturn()
    {
        if (Time.time - lastInputTime > returnDelay)
            isReturning = true;

        if (isReturning)
        {
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, returnSpeed * Time.deltaTime);
            if (Mathf.Abs(currentAngle - targetAngle) < 0.5f)
            {
                currentAngle = targetAngle;
                isReturning = false;
            }
        }
    }

    private Vector3 GetOffset()
    {
        Vector3 forward = target.forward;
        Vector3 right = target.right;
        float rad = currentAngle * Mathf.Deg2Rad;

        return forward * Mathf.Cos(rad) * -distance +
               right * Mathf.Sin(rad) * distance +
               Vector3.up * height;
    }

    private void UpdateCameraPosition()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + GetOffset();
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            1f / smoothSpeed
        );

        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    public void SetAngle(float angle) { currentAngle = angle; targetAngle = angle; }
    public float GetAngle() => currentAngle;
}