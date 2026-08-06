using UnityEngine;

public class BackpackFollower : MonoBehaviour
{
    public Camera targetCamera;
    public float distanceFromCamera = 1.5f;
    public float heightOffset = -0.4f;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // 1. Создаём луч из центра экрана (чтобы не зависеть от дрожания forward)
        Ray ray = targetCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

        // 2. Позиционируем якорь на пересечении луча с плоскостью на нужной дистанции
        Vector3 targetPosition = ray.GetPoint(distanceFromCamera);

        // 3. Поднимаем/опускаем по высоте (пояс)
        targetPosition.y += heightOffset;

        // 4. Двигаем якорь жёстко (без Lerp)
        transform.position = targetPosition;

        // 5. Поворачиваем рюкзак строго лицом к камере (без дрожания)
        Vector3 dirToCamera = targetCamera.transform.position - transform.position;
        dirToCamera.y = 0f; // 🛑 Убираем наклон по вертикали!
        transform.rotation = Quaternion.LookRotation(-dirToCamera);
    }
}