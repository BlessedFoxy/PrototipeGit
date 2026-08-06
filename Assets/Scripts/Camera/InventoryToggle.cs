using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    public Canvas inventoryCanvas; // Сюда InventoryCanvas
    public TrailWalker walker;     // Сюда игрока (TrailWalker)

    private bool isInventoryOpen = false;

    void Start()
    {
        if (inventoryCanvas != null)
            inventoryCanvas.enabled = false;
    }

    void Update()
    {
        // Если нажали Tab - открываем/закрываем интерфейс
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isInventoryOpen = !isInventoryOpen;
            inventoryCanvas.enabled = isInventoryOpen;

            // Блокируем/разблокируем движение игрока
            if (walker != null)
            {
                walker.isInventoryOpen = isInventoryOpen;
                walker.SetSpeed(isInventoryOpen ? 0f : 5f);
            }
        }
    }
}