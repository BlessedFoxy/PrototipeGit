using UnityEngine;

public class TestInventory : MonoBehaviour
{
    public InventoryUI inventoryUI;
    public ItemData[] testItems;

    void Start()
    {
        if (inventoryUI == null)
        {
            inventoryUI = FindAnyObjectByType<InventoryUI>();
            if (inventoryUI == null)
            {
                Debug.LogError("[TestInventory] InventoryUI not found!");
                return;
            }
        }

        foreach (var item in testItems)
        {
            if (item != null)
            {
                inventoryUI.AddItem(item);
                Debug.Log($"✅ Добавлен предмет: {item.itemName}");
            }
        }
    }
}