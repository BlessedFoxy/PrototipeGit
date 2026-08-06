using UnityEngine;

public class OverflowTrigger : MonoBehaviour
{
    public Transform playerTransform;

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Item"))
        {
            other.gameObject.layer = LayerMask.NameToLayer("Default");
            Vector3 dropPos = playerTransform.position - playerTransform.forward * 1.5f + Vector3.up * 0.5f;
            other.transform.position = dropPos;

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce((-playerTransform.forward + Vector3.up) * 3f, ForceMode.Impulse);
            }

            // ✅ ИСПРАВЛЕНО!
            BackpackManager backpackManager = FindAnyObjectByType<BackpackManager>();
            if (backpackManager != null)
                backpackManager.CloseBackpackInventory();
        }
    }
}