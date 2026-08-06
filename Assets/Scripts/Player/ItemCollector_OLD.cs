//using UnityEngine;

//public class ItemCollector : MonoBehaviour
//{
//    public Backpack backpack;
//    public Camera playerCamera;
//    public float interactDistance = 10f;
//    public LayerMask itemLayer;

//    public bool isInventoryOpen = false;

//    void Start()
//    {
//        if (backpack == null) backpack = FindAnyObjectByType<Backpack>();
//        if (playerCamera == null) playerCamera = Camera.main;
//    }

//    void Update()
//    {
//        if (isInventoryOpen) return;

//        if (Input.GetMouseButtonDown(0))
//        {
//            TryCollect(Input.mousePosition);
//        }
//    }

//    void TryCollect(Vector2 screenPosition)
//    {
//        Ray ray = playerCamera.ScreenPointToRay(screenPosition);
//        RaycastHit hit;

//        if (Physics.Raycast(ray, out hit, interactDistance, itemLayer))
//        {
//            Item item = hit.collider.GetComponent<Item>();
//            if (item != null && !item.isInBackpack)
//            {
//                if (backpack != null && backpack.CanAddItem(item))
//                {
//                    item.PickUp(backpack);
//                    Debug.Log($"[ItemCollector] ✅ Подобрал {item.itemName}!");
//                }
//            }
//        }
//    }
//}