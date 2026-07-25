using UnityEngine;
using System.Collections.Generic;

public class BackpackGrid
{
    public int width;
    public int height;
    private ItemData[,] grid;

    public class PlacedItem
    {
        public ItemData item;
        public Vector2Int position;
        public bool isRotated;
    }

    private List<PlacedItem> placedItems = new List<PlacedItem>();

    public BackpackGrid(int width, int height)
    {
        this.width = width;
        this.height = height;
        grid = new ItemData[width, height];
    }

    public bool CanPlace(ItemData item, Vector2Int position, bool rotated)
    {
        Vector2Int itemSize = rotated ? new Vector2Int(item.size.y, item.size.x) : item.size;

        if (position.x < 0 || position.y < 0 ||
            position.x + itemSize.x > width ||
            position.y + itemSize.y > height)
            return false;

        for (int x = 0; x < itemSize.x; x++)
        {
            for (int y = 0; y < itemSize.y; y++)
            {
                if (grid[position.x + x, position.y + y] != null)
                    return false;
            }
        }

        return true;
    }

    public bool Place(ItemData item, Vector2Int position, bool rotated)
    {
        if (!CanPlace(item, position, rotated))
            return false;

        Vector2Int itemSize = rotated ? new Vector2Int(item.size.y, item.size.x) : item.size;

        for (int x = 0; x < itemSize.x; x++)
        {
            for (int y = 0; y < itemSize.y; y++)
            {
                grid[position.x + x, position.y + y] = item;
            }
        }

        placedItems.Add(new PlacedItem
        {
            item = item,
            position = position,
            isRotated = rotated
        });

        return true;
    }

    public void Remove(ItemData item)
    {
        PlacedItem placed = placedItems.Find(p => p.item == item);
        if (placed == null) return;

        Vector2Int itemSize = placed.isRotated ?
            new Vector2Int(placed.item.size.y, placed.item.size.x) :
            placed.item.size;

        for (int x = 0; x < itemSize.x; x++)
        {
            for (int y = 0; y < itemSize.y; y++)
            {
                grid[placed.position.x + x, placed.position.y + y] = null;
            }
        }

        placedItems.Remove(placed);
    }

    public int GetUsedVolume()
    {
        int volume = 0;
        foreach (var placed in placedItems)
        {
            volume += placed.item.volume;
        }
        return volume;
    }

    public int GetTotalVolume() => width * height;

    public float GetFillPercentage() =>
        (float)GetUsedVolume() / GetTotalVolume() * 100f;

    public List<PlacedItem> GetAllPlacedItems() => placedItems;
}