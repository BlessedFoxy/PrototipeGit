using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public Vector2Int size = Vector2Int.one;
    public int volume => size.x * size.y;
    public int quantity = 1;  // ← ДОБАВИТЬ!
}