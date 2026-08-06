using System.Collections.Generic;
using UnityEngine;

public class BackpackVisual : MonoBehaviour
{
    [Header("Ссылки")]
    public BackpackManager backpackManager; // Скрытая комната
    public Transform itemsContainer;        // Куда класть визуальные копии

    [Header("Настройки")]
    public float spacing = 0.15f;           // Расстояние между предметами
    public float scaleMultiplier = 0.5f;    // Размер предметов на спине

    private List<GameObject> visualItems = new List<GameObject>();

    void Start()
    {
        if (backpackManager == null)
            backpackManager = FindAnyObjectByType<BackpackManager>();

        if (itemsContainer == null)
        {
            GameObject container = new GameObject("ItemsVisualContainer");
            container.transform.SetParent(transform);
            container.transform.localPosition = Vector3.zero;
            itemsContainer = container.transform;
        }

        // Подписываемся на обновление рюкзака
        if (backpackManager != null)
            backpackManager.OnBackpackChanged += RefreshVisual;

        RefreshVisual();
    }

    void OnDestroy()
    {
        if (backpackManager != null)
            backpackManager.OnBackpackChanged -= RefreshVisual;
    }

    public void RefreshVisual()
    {
        // Очищаем старые визуальные копии
        foreach (var item in visualItems)
        {
            if (item != null)
                Destroy(item);
        }
        visualItems.Clear();

        if (backpackManager == null || backpackManager.items.Count == 0)
            return;

        // Создаём визуальные копии предметов
        float startX = -(backpackManager.items.Count - 1) * spacing / 2f;

        for (int i = 0; i < backpackManager.items.Count; i++)
        {
            GameObject original = backpackManager.items[i];
            if (original == null) continue;

            Item itemData = original.GetComponent<Item>();
            if (itemData == null) continue;

            // Создаём визуальную копию
            GameObject copy = new GameObject($"Visual_{itemData.itemName}_{i}");
            copy.transform.SetParent(itemsContainer);

            // Копируем спрайт
            SpriteRenderer sr = copy.AddComponent<SpriteRenderer>();
            sr.sprite = itemData.icon;
            sr.sortingOrder = 5;

            // Позиция
            float x = startX + i * spacing;
            copy.transform.localPosition = new Vector3(x, 0, 0);
            copy.transform.localScale = Vector3.one * scaleMultiplier;

            visualItems.Add(copy);
        }
    }
}