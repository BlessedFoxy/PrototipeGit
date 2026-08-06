using UnityEngine;

public class BackpackTriggerListener : MonoBehaviour
{
    public System.Action<GameObject> OnItemEnter;
    public System.Action<GameObject> OnItemExit;

    void OnTriggerEnter2D(Collider2D other)
    {
        // Проверяем, что это предмет (слой Items)
        if (other.gameObject.layer == LayerMask.NameToLayer("Items"))
        {
            OnItemEnter?.Invoke(other.gameObject);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Items"))
        {
            OnItemExit?.Invoke(other.gameObject);
        }
    }
}