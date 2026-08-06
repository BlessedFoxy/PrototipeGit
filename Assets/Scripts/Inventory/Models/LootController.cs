using System.Collections;
using UnityEngine;

public class LootController : MonoBehaviour
{
    public Camera mainCamera;
    public Transform physicsBackpackContainer; // PhysicsBackpackRoom/BackpackContainer
    public BackpackManager backpackManager;

    [Header("Анимация подбора")]
    public float flySpeed = 5f;
    public float destroyDistance = 0.2f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Item"))
                {
                    StartCoroutine(FlyToBackpack(hit.collider.gameObject));
                }
            }
        }
    }

    IEnumerator FlyToBackpack(GameObject item)
    {
        Rigidbody rb = item.GetComponent<Rigidbody>();
        Collider col = item.GetComponent<Collider>();

        // Отключаем физику мира
        if (rb != null) rb.isKinematic = true;
        if (col != null) col.enabled = false;

        // Анимация полёта к камере
        while (Vector3.Distance(item.transform.position, mainCamera.transform.position) > destroyDistance)
        {
            item.transform.position = Vector3.MoveTowards(item.transform.position, mainCamera.transform.position, flySpeed * Time.deltaTime);
            item.transform.localScale = Vector3.Lerp(item.transform.localScale, Vector3.zero, flySpeed * Time.deltaTime);
            yield return null;
        }

        // Телепорт в скрытую комнату
        item.transform.localScale = Vector3.one;
        Vector3 spawnPoint = physicsBackpackContainer.position + Vector3.up * 2f;
        item.transform.position = spawnPoint;
        item.layer = LayerMask.NameToLayer("Inventory");

        // Включаем физику в комнате
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        if (col != null) col.enabled = true;

        yield return new WaitForFixedUpdate();

        // Обновляем визуал на спине
        if (backpackManager != null)
            backpackManager.CloseBackpackInventory();
    }
}