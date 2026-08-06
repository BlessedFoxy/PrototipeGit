using UnityEngine;

public class ItemCollector : MonoBehaviour
{
    public BackpackManager backpackManager;
    public Camera playerCamera;
    public float interactDistance = 10f;
    public LayerMask itemLayer;

    void Start()
    {
        if (backpackManager == null)
            backpackManager = FindAnyObjectByType<BackpackManager>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryCollect(Input.mousePosition);
        }
    }

    void TryCollect(Vector2 screenPosition)
    {
        Ray ray = playerCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, itemLayer))
        {
            Item item = hit.collider.GetComponent<Item>();
            if (item != null && !item.isInBackpack)
            {
                if (backpackManager != null && backpackManager.CanAddItem(item))
                {
                    // Подбираем предмет
                    item.PickUp(backpackManager);
                    Debug.Log($"[ItemCollector] ✅ Подобрал {item.itemName}!");

                    // ✅ Обновляем визуал на спине
                    BackpackVisual visual = FindAnyObjectByType<BackpackVisual>();
                    if (visual != null) visual.RefreshVisual();

                    // ✅ ОБНОВЛЯЕМ UI ИНВЕНТАРЬ
                    InventoryUI ui = FindAnyObjectByType<InventoryUI>();
                    if (ui != null)
                    {
                        ui.RefreshUI();
                        Debug.Log("[ItemCollector] UI инвентарь обновлён");
                    }
                }
                else
                {
                    Debug.Log("[ItemCollector] ❌ Рюкзак полон!");
                }
            }
        }
    }
}