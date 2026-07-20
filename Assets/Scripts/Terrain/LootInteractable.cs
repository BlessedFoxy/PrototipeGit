using UnityEngine;

public class LootInteractable : MonoBehaviour
{
    [Header("Настройки")]
    public string itemName = "Сокровище";
    public float rotationSpeed = 90f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.3f;

    private Vector3 startPosition;
    private float startY;
    private bool isCollected = false;
    private float collectDistance = 2f;

    void Start()
    {
        startPosition = transform.position;
        startY = startPosition.y;
    }

    void Update()
    {
        if (isCollected) return;

        // Вращение
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Подпрыгивание
        float newY = startY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // Проверка сбора
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null && Vector3.Distance(transform.position, player.position) < collectDistance)
        {
            Collect();
        }
    }

    public void Initialize(string name)
    {
        itemName = name;
    }

    public void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        Debug.Log($"🎁 Собран предмет: {itemName}");

        // TODO: Добавить в инвентарь
        // InventoryManager.Instance.AddItem(itemName);

        // Эффект сбора
        // Instantiate(collectEffect, transform.position, Quaternion.identity);

        Destroy(gameObject, 0.3f);
    }
}