using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    public float speed = 5f;
    public float acceleration = 3f;
    public float deceleration = 5f;

    [Header("Ссылки")]
    public FollowSpline followSpline;

    [Header("Управление")]
    public bool useKeyboard = true;
    public bool useTouch = true;

    private float currentSpeed = 0f;
    private bool isMovingForward = false;
    private bool isMovingBackward = false;

    void Start()
    {
        if (followSpline == null)
        {
            followSpline = GetComponent<FollowSpline>();
            if (followSpline == null)
            {
                followSpline = FindAnyObjectByType<FollowSpline>();
                if (followSpline == null)
                {
                    Debug.LogError("FollowSpline не найден! Добавьте компонент на персонажа.");
                    enabled = false;
                    return;
                }
            }
        }

        // Автоматически начинаем движение
        followSpline.StartFollowing();
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        bool moveForward = false;
        bool moveBackward = false;

        // Клавиатура
        if (useKeyboard)
        {
            moveForward = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            moveBackward = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        }

        // Тач (мобильное управление)
        if (useTouch && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                // Свайп вверх = вперед, вниз = назад
                if (touch.deltaPosition.y > 0)
                {
                    moveForward = true;
                }
                else if (touch.deltaPosition.y < 0)
                {
                    moveBackward = true;
                }
            }
        }

        // Обновляем состояние
        isMovingForward = moveForward;
        isMovingBackward = moveBackward;

        // Устанавливаем скорость
        float targetSpeed = 0f;
        if (moveForward)
        {
            targetSpeed = speed;
        }
        else if (moveBackward)
        {
            targetSpeed = -speed * 0.5f;
        }

        // Обновляем скорость в FollowSpline
        if (followSpline != null)
        {
            followSpline.speed = Mathf.Abs(targetSpeed);

            if (Mathf.Abs(targetSpeed) > 0.1f)
            {
                followSpline.StartFollowing();
            }
            else
            {
                followSpline.StopFollowing();
            }
        }
    }

    // Геттеры для отладки
    public bool IsMovingForward() => isMovingForward;
    public bool IsMovingBackward() => isMovingBackward;
    public float GetCurrentSpeed() => followSpline != null ? followSpline.currentSpeed : 0f;
}