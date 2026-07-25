using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class InventoryUI : MonoBehaviour
{
    [Header("Settings")]
    public SimpleInventory inventory;
    public float animationDuration = 0.4f;
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("Camera Blocker")]
    public GameObject cameraObject;

    [Header("Адаптивные настройки")]
    public int columns = 4;
    public int rows = 4;
    public float panelWidthPercent = 0.85f;      // ← 85% от ширины экрана
    public float panelHeightPercent = 0.45f;     // ← 45% от высоты экрана
    public float cellSpacing = 6f;               // ← отступ между ячейками

    private MonoBehaviour cameraController;
    private VisualElement root;
    private VisualElement backpackPanel;
    private VisualElement handle;
    private VisualElement gridContainer;
    private Label itemCounter;
    private bool isOpen = false;
    private bool isAnimating = false;

    void Start()
    {
        // Авто-поиск CameraFollow
        if (cameraObject != null)
        {
            cameraController = cameraObject.GetComponent<CameraFollow>();
            if (cameraController == null)
                cameraController = cameraObject.GetComponentInChildren<CameraFollow>();
        }
        else
        {
            cameraController = FindAnyObjectByType<CameraFollow>();
        }

        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) { Debug.LogError("[InventoryUI] UIDocument not found!"); return; }

        root = uiDocument.rootVisualElement;
        backpackPanel = root.Q<VisualElement>("backpack-panel");
        handle = root.Q<VisualElement>("handle");
        gridContainer = root.Q<VisualElement>("grid-container");
        itemCounter = root.Q<Label>("item-counter");

        if (backpackPanel == null) { Debug.LogError("[InventoryUI] backpack-panel not found!"); return; }

        // ============================================
        // 🔥 АДАПТИВНЫЙ РАЗМЕР ПАНЕЛИ
        // ============================================
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        backpackPanel.style.width = Length.Percent(panelWidthPercent * 100f);
        backpackPanel.style.left = Length.Percent((1f - panelWidthPercent) / 2f * 100f);
        backpackPanel.style.right = Length.Percent((1f - panelWidthPercent) / 2f * 100f);
        backpackPanel.style.height = Length.Percent(panelHeightPercent * 100f);

        // Панель скрыта снизу
        float panelHeightPx = screenHeight * panelHeightPercent;
        backpackPanel.style.bottom = -panelHeightPx - 100;

        // ============================================
        // 🔥 АДАПТИВНЫЙ РАЗМЕР ЯЧЕЕК
        // ============================================
        float panelWidthPx = screenWidth * panelWidthPercent;
        float totalSpacing = cellSpacing * (columns + 1);
        float cellSize = (panelWidthPx - totalSpacing) / columns;
        cellSize = Mathf.Min(cellSize, 80f); // ← максимум 80px
        cellSize = Mathf.Max(cellSize, 40f); // ← минимум 40px

        // ============================================
        // НАСТРОЙКА РУЧКИ
        // ============================================
        if (handle != null)
        {
            handle.RegisterCallback<ClickEvent>(evt => ToggleInventory());
            handle.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
        }

        var closeBtn = root.Q<Label>("close-btn");
        if (closeBtn != null)
            closeBtn.RegisterCallback<ClickEvent>(evt => ToggleInventory());

        // ============================================
        // СОЗДАЁМ СЕТКУ
        // ============================================
        CreateGrid(cellSize);
        RefreshUI();
        Debug.Log($"[InventoryUI] ✅ Адаптивный инвентарь: {cellSize:F0}px ячейки, {panelWidthPx:F0}x{panelHeightPx * screenHeight:F0}");
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) ToggleInventory();

        if (isOpen && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended && touch.deltaPosition.y < -50f)
                ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (isAnimating) return;

        isOpen = !isOpen;
        isAnimating = true;

        if (cameraController != null)
            cameraController.enabled = !isOpen;

        float panelHeightPx = Screen.height * panelHeightPercent;
        float targetBottom = isOpen ? 0 : -panelHeightPx - 100;

        StartCoroutine(AnimatePanel(targetBottom));
    }

    System.Collections.IEnumerator AnimatePanel(float targetBottom)
    {
        float elapsed = 0f;
        float panelHeightPx = Screen.height * panelHeightPercent;
        float startBottom = isOpen ? -panelHeightPx - 100 : 0;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            backpackPanel.style.bottom = Mathf.Lerp(startBottom, targetBottom, smoothT);
            yield return null;
        }

        backpackPanel.style.bottom = targetBottom;
        isAnimating = false;
        if (isOpen) RefreshUI();
    }

    void CreateGrid(float cellSize)
    {
        if (gridContainer == null) return;
        gridContainer.Clear();

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                var cell = new VisualElement();
                cell.style.width = cellSize;
                cell.style.height = cellSize;
                cell.style.marginLeft = cellSpacing / 2;
                cell.style.marginRight = cellSpacing / 2;
                cell.style.marginTop = cellSpacing / 2;
                cell.style.marginBottom = cellSpacing / 2;
                cell.style.backgroundColor = new Color(0.25f, 0.25f, 0.35f, 0.4f);
                cell.style.borderTopLeftRadius = 8;
                cell.style.borderTopRightRadius = 8;
                cell.style.borderBottomLeftRadius = 8;
                cell.style.borderBottomRightRadius = 8;
                cell.style.borderLeftWidth = 1;
                cell.style.borderRightWidth = 1;
                cell.style.borderTopWidth = 1;
                cell.style.borderBottomWidth = 1;
                cell.style.borderLeftColor = new Color(0.5f, 0.5f, 0.7f, 0.1f);
                cell.style.borderRightColor = new Color(0.5f, 0.5f, 0.7f, 0.1f);
                cell.style.borderTopColor = new Color(0.5f, 0.5f, 0.7f, 0.1f);
                cell.style.borderBottomColor = new Color(0.5f, 0.5f, 0.7f, 0.1f);

                gridContainer.Add(cell);
            }
        }
    }

    public void RefreshUI()
    {
        if (gridContainer == null) return;

        var cells = gridContainer.Children().ToList();
        foreach (var c in cells) c.Clear();

        if (inventory == null || inventory.items.Count == 0)
        {
            var empty = new Label("🎒 Пусто");
            empty.style.color = Color.gray;
            empty.style.fontSize = 16;
            empty.style.unityTextAlign = TextAnchor.MiddleCenter;
            empty.style.width = Length.Percent(100);
            empty.style.height = Length.Percent(100);
            gridContainer.Add(empty);
            if (itemCounter != null) itemCounter.text = "0 предметов";
            return;
        }

        if (itemCounter != null)
            itemCounter.text = $"{inventory.items.Sum(i => i.quantity)} предметов";

        int index = 0;
        var allCells = gridContainer.Children().ToList();

        foreach (var item in inventory.items)
        {
            if (index >= allCells.Count) break;
            var cell = allCells[index];

            var slot = new VisualElement();
            slot.style.position = Position.Absolute;
            slot.style.left = 0;
            slot.style.top = 0;
            slot.style.width = Length.Percent(100);
            slot.style.height = Length.Percent(100);
            slot.style.alignItems = Align.Center;
            slot.style.justifyContent = Justify.Center;

            if (item.icon != null)
            {
                var icon = new Image { sprite = item.icon };
                icon.style.width = Length.Percent(60);
                icon.style.height = Length.Percent(60);
                slot.Add(icon);
            }

            if (item.quantity > 1)
            {
                var qty = new Label($"x{item.quantity}");
                qty.style.position = Position.Absolute;
                qty.style.right = 2;
                qty.style.bottom = 1;
                qty.style.fontSize = 10;
                qty.style.color = Color.white;
                slot.Add(qty);
            }

            cell.Add(slot);
            index++;
        }
    }

    public void AddItem(ItemData item)
    {
        if (item == null || inventory == null) return;
        inventory.AddItem(item);
    }
}