using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(SpriteRenderer))]
public class Item : MonoBehaviour
{
    [Header("Характеристики")]
    public string itemName = "Предмет";
    public float weight = 1f;
    public Sprite icon;

    [Header("Состояние")]
    public bool isInBackpack = false;
    private BackpackManager currentBackpack;

    private Rigidbody rb;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (icon == null && spriteRenderer != null)
            icon = spriteRenderer.sprite;
    }

    public void PickUp(BackpackManager backpack)
    {
        if (backpack == null) return;
        currentBackpack = backpack;
        isInBackpack = true;
        backpack.AddItem(this.gameObject);
    }

    public void Drop(Vector3 dropPosition)
    {
        if (currentBackpack != null)
        {
            currentBackpack.RemoveItem(this.gameObject);
        }

        isInBackpack = false;
        currentBackpack = null;

        transform.position = dropPosition;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one; // Возвращаем большой размер в мир

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.AddForce(new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 3f), 0), ForceMode.Impulse);
        }
    }
}