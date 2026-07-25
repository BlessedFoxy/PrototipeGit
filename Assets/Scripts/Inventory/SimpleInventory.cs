using UnityEngine;
using System.Collections.Generic;

public class SimpleInventory : MonoBehaviour
{
    public List<ItemData> items = new List<ItemData>();

    public void AddItem(ItemData item)
    {
        items.Add(item);
        Debug.Log($"✅ Добавлен {item.itemName}");

        // Уведомляем UI
        InventoryUI ui = FindAnyObjectByType<InventoryUI>();
        if (ui != null) ui.RefreshUI();
    }

    public void RemoveItem(ItemData item)
    {
        items.Remove(item);
        InventoryUI ui = FindAnyObjectByType<InventoryUI>();
        if (ui != null) ui.RefreshUI();
    }
}