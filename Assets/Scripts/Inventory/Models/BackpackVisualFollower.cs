using UnityEngine;

public class BackpackVisualFollower : MonoBehaviour
{
    public Camera targetCamera;
    public float distanceFromCamera = 1.5f; // Как далеко от глаз
    public float heightOffset = -0.3f;      // На уровне пояса

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // 1. Ставим рюкзак ровно перед камерой (всегда!)
        Vector3 targetPosition = targetCamera.transform.position + targetCamera.transform.forward * distanceFromCamera;

        // 2. Корректируем высоту
        targetPosition.y = targetCamera.transform.position.y + heightOffset;

        // 3. Двигаем без задержек (жестко)
        transform.position = targetPosition;

        // 4. Поворачиваем строго к камере (чтобы видеть лицевую сторону)
        transform.rotation = Quaternion.LookRotation(-targetCamera.transform.forward);
    }
}