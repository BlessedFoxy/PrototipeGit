using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("Цель")]
    public Transform target;

    [Header("Настройки позиции")]
    public Vector3 offset = new Vector3(0, 5, -8);
    public float smoothSpeed = 5f;

    [Header("Осмотр")]
    public float lookSensitivity = 2f;
    public float maxLookAngle = 30f;

    private float currentLookX = 0f;

    void Start()
    {
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (target == null)
            {
                Debug.LogError("Камера: цель не найдена!");
                enabled = false;
                return;
            }
        }

        // Начальная позиция
        transform.position = target.position + offset;
        transform.LookAt(target);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Обработка осмотра
        HandleLookAround();

        // Расчет позиции
        Vector3 desiredPosition = target.position + offset;

        // Добавляем смещение для осмотра
        desiredPosition += Quaternion.Euler(0, currentLookX, 0) * new Vector3(1f, 0, 0);

        // Плавное движение
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Камера смотрит на цель
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    void HandleLookAround()
    {
        float mouseX = Input.GetAxis("Mouse X");

        if (Mathf.Abs(mouseX) > 0.1f)
        {
            currentLookX += mouseX * lookSensitivity;
            currentLookX = Mathf.Clamp(currentLookX, -maxLookAngle, maxLookAngle);
        }
        else
        {
            // Плавный возврат в центр
            currentLookX = Mathf.Lerp(currentLookX, 0f, Time.deltaTime * 2f);
        }
    }

    // Метод для сброса
    public void ResetLook()
    {
        currentLookX = 0f;
    }
}