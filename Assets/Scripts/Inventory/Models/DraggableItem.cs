using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class DraggableItem : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 offset;
    private Rigidbody rb;
    private Item item;
    private Camera mainCam;
    private float fixedZ;

    void Start()
    {
        mainCam = Camera.main;
        rb = GetComponent<Rigidbody>();
        item = GetComponent<Item>();
        fixedZ = transform.position.z;
    }

    void Update()
    {
        bool isPressed = false;
        Vector3 pressPos = Vector3.zero;

        if (Input.GetMouseButtonDown(0))
        {
            isPressed = true;
            pressPos = Input.mousePosition;
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            isPressed = true;
            pressPos = Input.GetTouch(0).position;
        }

        if (isPressed)
        {
            Ray ray = mainCam.ScreenPointToRay(pressPos);
            RaycastHit hit;

            // 🟢 ОТЛАДКА 1: Рисуем красный луч в окне Scene
            Debug.DrawRay(ray.origin, ray.direction * 10f, Color.red, 3f);

            if (Physics.Raycast(ray, out hit, 100f))
            {
                // 🟢 ОТЛАДКА 2: Пишем в консоль имя объекта
                Debug.Log("👀 Луч попал в: " + hit.collider.gameObject.name);

                // Проверяем, есть ли скрипт Item
                Item hitItem = hit.collider.GetComponent<Item>();
                if (hitItem != null && hitItem == item && item.isInBackpack)
                {
                    StartDragging();
                    return;
                }
            }
            else
            {
                // 🟢 ОТЛАДКА 3: Если луч промахнулся
                Debug.Log("❌ Луч НИКУДА не попал (пустота)");
            }
        }

        // ... (остальной код перетаскивания) ...



        if (isDragging)
        {
            bool isReleased = false;
            if (Input.GetMouseButtonUp(0)) isReleased = true;
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) isReleased = true;

            if (isReleased)
            {
                StopDragging();
                return;
            }

            Vector3 currentInputPos = Vector3.zero;
            if (Input.GetMouseButton(0)) currentInputPos = Input.mousePosition;
            else if (Input.touchCount > 0) currentInputPos = Input.GetTouch(0).position;

            Vector3 mousePos = currentInputPos;

            // 🛑 ФИКС: Берем Z НЕ из яблока, а из постоянной дистанции (например, 1.5 метра от камеры)!
            mousePos.z = 1.5f;

            Vector3 worldPos = mainCam.ScreenToWorldPoint(mousePos);

            // Оставляем Z яблока неизменным (каким оно было до перетаскивания)
            transform.position = new Vector3(worldPos.x + offset.x, worldPos.y + offset.y, transform.position.z);
        }
    }

    void StartDragging()
    {
        isDragging = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Vector3 mousePos = Vector3.zero;
        if (Input.GetMouseButton(0)) mousePos = Input.mousePosition;
        else if (Input.touchCount > 0) mousePos = Input.GetTouch(0).position;

        // 🛑 ФИКС: Фиксированная дистанция вместо расчёта от яблока
        mousePos.z = 1.5f;

        Vector3 worldPos = mainCam.ScreenToWorldPoint(mousePos);
        offset = transform.position - worldPos;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void StopDragging()
    {
        isDragging = false;

        if (rb != null && item != null && item.isInBackpack)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
        }
    }
}