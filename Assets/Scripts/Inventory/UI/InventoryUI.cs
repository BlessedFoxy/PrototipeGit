using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("Ссылки")]
    public BackpackManager backpackManager;
    public Transform slotsParent;
    public GameObject slotPrefab;

    [Header("Настройки")]
    public KeyCode toggleKey = KeyCode.Tab;

    private Canvas canvas;
    private bool isOpen = false;
    private List<GameObject> slots = new List<GameObject>();

    void Start()
    {
        canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.enabled = false;

        if (backpackManager == null)
            backpackManager = FindAnyObjectByType<BackpackManager>();

        if (backpackManager != null)
            backpackManager.OnBackpackChanged += RefreshUI;

        RefreshUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isOpen = !isOpen;
            if (canvas != null)
            {
                canvas.enabled = isOpen;
                if (isOpen) RefreshUI();
            }
        }
    }

    public void RefreshUI()
    {
        // 🛑 МЫ ОТКЛЮЧАЕМ СОЗДАНИЕ UI-СЛОТОВ, ТАК КАК ТЕПЕРЬ МЫ ТАСКАЕМ ФИЗИЧЕСКИЕ ПРЕДМЕТЫ
        // Просто очищаем список, чтобы не накапливались старые слоты
        foreach (var slot in slots) Destroy(slot);
        slots.Clear();

        // Всё! Никакого CreateSlot() здесь больше не вызываем.
        // Зато мы можем оставить пустую заглушку, если пусто
        if (backpackManager == null || backpackManager.items.Count == 0)
        {
            Debug.Log("[InventoryUI] Рюкзак пуст, но UI-слоты не создаем.");
            return;
        }
    }

    void CreateSlot(Item item)
    {
        if (slotPrefab == null)
        {
            Debug.LogError("[InventoryUI] ❌ slotPrefab == null!");
            return;
        }

        GameObject slot = Instantiate(slotPrefab, slotsParent);
        slots.Add(slot);

        // Ищем иконку
        Image icon = slot.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null)
        {
            // 👀 ОТЛАДКА: Проверяем, есть ли картинка в самом предмете
            if (icon != null && item.icon != null)
            {
                icon.sprite = item.icon;
                icon.preserveAspect = true; // ⚡ ВОЛШЕБНАЯ СТРОКА! Включает авто-подгонку пропорций.
                icon.enabled = true;
            }
            else
            {
                // 🚨 ЭТО ОШИБКА, ЕСЛИ ТЫ ЭТО УВИДИШЬ В КОНСОЛИ
                Debug.LogError("[InventoryUI] ❌ У ПРЕДМЕТА НЕТ ИКОНКИ! item.icon == null!");
            }
        }
        else
        {
            // 🚨 ЭТО ОШИБКА, ЕСЛИ ТЫ ЭТО УВИДИШЬ
            Debug.LogError("[InventoryUI] ❌ Компонент Image не найден в префабе! Проверь название объекта!");
        }

        // Ищем название
        Text text = slot.transform.Find("ItemName")?.GetComponent<Text>();
        if (text != null)
        {
            text.text = item.itemName;
        }
    }

    void CreateEmptySlot()
    {
        if (slotPrefab == null) return;
        GameObject slot = Instantiate(slotPrefab, slotsParent);
        slots.Add(slot);

        Text text = slot.transform.Find("ItemName")?.GetComponent<Text>();
        if (text != null) text.text = "Пусто";
    }
}