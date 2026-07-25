using UnityEngine;

public class WorldLoot : MonoBehaviour
{
    [Header("Данные")]
    public ItemData itemData;
    public int quantity = 1;

    [Header("Визуал")]
    public float bobSpeed = 1f;
    public float bobHeight = 0.2f;
    public float rotationSpeed = 1f;

    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private float timeOffset;
    private bool isCollected = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (itemData != null && itemData.icon != null)
            spriteRenderer.sprite = itemData.icon;

        var collider = GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
        }

        startPosition = transform.position;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
        gameObject.tag = "Collectible";
    }

    void Update()
    {
        float yOffset = Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobHeight;
        transform.position = startPosition + Vector3.up * yOffset;
        transform.Rotate(Vector3.up, Time.deltaTime * rotationSpeed * 30f);
    }

    public void Collect()
    {
        if (isCollected) return;

        InventoryUI ui = FindAnyObjectByType<InventoryUI>();
        if (ui == null)
        {
            Debug.LogWarning("[WorldLoot] InventoryUI не найден!");
            return;
        }

        ItemData copy = ScriptableObject.CreateInstance<ItemData>();
        copy.itemName = itemData.itemName;
        copy.icon = itemData.icon;
        copy.quantity = quantity;

        ui.AddItem(copy);
        isCollected = true;
        Debug.Log($"🎒 Собран {itemData.itemName} x{quantity}");

        Destroy(gameObject, 0.2f);
    }

    public bool IsCollected() => isCollected;
}