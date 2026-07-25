using UnityEngine;

public class ItemCollector : MonoBehaviour
{
    [Header("Ссылки")]
    public SimpleInventory inventory;
    public Camera playerCamera;
    public float interactDistance = 10f;
    public LayerMask itemLayer;

    [Header("UI")]
    public GameObject interactPrompt;

    void Start()
    {
        if (inventory == null)
            inventory = GetComponent<SimpleInventory>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void Update()
    {
        // ============================================
        // 1. КЛИК МЫШКОЙ (ПК)
        // ============================================
        if (Input.GetMouseButtonDown(0))
        {
            TryCollect(Input.mousePosition);
        }

        // ============================================
        // 2. ТАЧ (МОБИЛКА)
        // ============================================
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                TryCollect(touch.position);
            }
        }
    }

    void TryCollect(Vector2 screenPosition)
    {
        if (playerCamera == null)
        {
            Debug.LogError("[ItemCollector] Камера не назначена!");
            return;
        }

        // ============================================
        // 🔥 ПУСКАЕМ ЛУЧ ИЗ КАМЕРЫ В ТОЧКУ КЛИКА
        // ============================================
        Ray ray = playerCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, itemLayer))
        {
            Debug.Log($"[ItemCollector] Клик по: {hit.collider.gameObject.name}, Tag: {hit.collider.tag}");

            if (!hit.collider.CompareTag("Collectible")) return;

            WorldLoot loot = hit.collider.GetComponent<WorldLoot>();
            if (loot != null && !loot.IsCollected())
            {
                loot.Collect();
                if (interactPrompt != null)
                    interactPrompt.SetActive(false);
            }
        }
        else
        {
            Debug.Log("[ItemCollector] Луч никуда не попал");
        }
    }
}