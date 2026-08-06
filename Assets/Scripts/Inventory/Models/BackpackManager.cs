using System.Collections.Generic;
using UnityEngine;

public class BackpackManager : MonoBehaviour
{
    [Header("Настройки рюкзака")]
    public float maxWeight = 30f;
    public int maxItems = 20;
    public float currentWeight = 0f;

    [Header("Состояние")]
    public List<GameObject> items = new List<GameObject>();

    public System.Action OnBackpackChanged;

    public static BackpackManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        Debug.Log($"[BackpackManager] ✅ Инициализирован. Предметов: {items.Count}");
    }

    public bool CanAddItem(Item item)
    {
        if (item == null) return false;
        if (currentWeight + item.weight > maxWeight) return false;
        if (items.Count >= maxItems) return false;
        return true;
    }

    public void AddItem(GameObject itemObject)
    {
        Item item = itemObject.GetComponent<Item>();
        if (item == null) return;

        items.Add(itemObject);
        currentWeight += item.weight;

        // 1. Находим ItemContainer внутри BackpackContainer
        Transform itemContainer = transform.Find("ItemContainer");
        if (itemContainer == null)
        {
            Debug.LogError("❌ Не найден ItemContainer!");
            return;
        }

        // 2. Кидаем предмет внутрь
        itemObject.transform.SetParent(itemContainer);

        // 3. Устанавливаем правильный маленький размер
        itemObject.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

        // 4. Кидаем его в случайное место на дне (немного выше пола)
        itemObject.transform.localPosition = new Vector3(
            Random.Range(-0.15f, 0.15f),
            -0.03f, // Чуть выше дна, чтобы гравитация схватила и уронила
            0f
        );
        itemObject.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        // 5. Включаем физику
        Rigidbody rb = itemObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | // З-заморожена, чтобы не улетело вглубь
                             RigidbodyConstraints.FreezeRotationX |
                             RigidbodyConstraints.FreezeRotationY;
            rb.linearVelocity = Vector3.zero;
        }

        OnBackpackChanged?.Invoke();
    }
    public void RemoveItem(GameObject itemObject)
    {
        if (items.Contains(itemObject))
        {
            items.Remove(itemObject);
            itemObject.transform.SetParent(null);

            Rigidbody rb = itemObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            Debug.Log($"[BackpackManager] ❌ Удален предмет. Осталось: {items.Count}");
            OnBackpackChanged?.Invoke();
        }
    }

    public void CloseBackpackInventory()
    {
        OnBackpackChanged?.Invoke();
        Debug.Log("[BackpackManager] 🔄 Инвентарь обновлен");
    }

    public int GetItemCount() { return items.Count; }
    public float GetCurrentWeight() { return currentWeight; }
}