using System;
using System.Collections.Generic;
using UnityEngine;

namespace RUSH.Items
{
    public class Inventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int columns = 5;
        [SerializeField, Min(1)] private int rows = 4;
        [SerializeField] private bool allowRotation = true;
        [SerializeField] private bool singleRowItems;
        [SerializeField] private ItemStack[] startingItems = Array.Empty<ItemStack>();

        [NonSerialized] private List<InventoryItem> items;
        [NonSerialized] private InventoryItem[] cells;

        public event Action Changed;

        public int Columns => columns;
        public int Rows => rows;
        public bool AllowRotation => allowRotation;
        public IReadOnlyList<InventoryItem> Items => ItemList;

        private List<InventoryItem> ItemList => items ??= new List<InventoryItem>();

        private InventoryItem[] Cells
        {
            get
            {
                if (cells != null && cells.Length == columns * rows)
                    return cells;

                cells = new InventoryItem[columns * rows];
                foreach (InventoryItem item in ItemList)
                    Occupy(item, item);
                return cells;
            }
        }

        private void Awake()
        {
            foreach (ItemStack stack in startingItems)
            {
                if (!stack.IsEmpty)
                    AddSilently(stack.Definition, stack.Count);
            }
        }

        public InventoryItem GetAt(Vector2Int cell)
        {
            return IsInside(cell) ? Cells[cell.y * columns + cell.x] : null;
        }

        public bool CanPlace(Vector2Int size, Vector2Int position, InventoryItem ignore = null)
        {
            if (position.x < 0 || position.y < 0 || position.x + size.x > columns || position.y + size.y > rows)
                return false;

            if (singleRowItems && size.y > 1)
                return false;

            for (int y = position.y; y < position.y + size.y; y++)
            {
                for (int x = position.x; x < position.x + size.x; x++)
                {
                    InventoryItem occupant = Cells[y * columns + x];
                    if (occupant != null && occupant != ignore)
                        return false;
                }
            }

            return true;
        }

        public bool TryFindSpace(ItemDefinition definition, out Vector2Int position, out bool rotated)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                rotated = pass == 1;
                Vector2Int size = definition.GetSize(rotated);
                if (rotated && (!allowRotation || size == definition.Size))
                    break;

                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        position = new Vector2Int(x, y);
                        if (CanPlace(size, position))
                            return true;
                    }
                }
            }

            position = default;
            rotated = false;
            return false;
        }

        public int Add(ItemDefinition definition, int count)
        {
            int left = AddSilently(definition, count);
            if (left != count)
                Changed?.Invoke();
            return left;
        }

        public bool TryAddAt(ItemDefinition definition, Component instance, Vector2Int position, bool rotated, int count = 1)
        {
            if (definition == null || count <= 0 || rotated && !allowRotation || !CanPlace(definition.GetSize(rotated), position))
                return false;

            Insert(new InventoryItem(definition, count, instance, position, rotated));
            Changed?.Invoke();
            return true;
        }

        public bool TryAdd(ItemDefinition definition, Component instance)
        {
            return definition != null
                   && TryFindSpace(definition, out Vector2Int position, out bool rotated)
                   && TryAddAt(definition, instance, position, rotated);
        }

        public bool TryMove(InventoryItem item, Vector2Int position, bool rotated)
        {
            if (!Contains(item) || rotated && !allowRotation || !CanPlace(item.Definition.GetSize(rotated), position, item))
                return false;

            Occupy(item, null);
            item.Position = position;
            item.Rotated = rotated;
            Occupy(item, item);
            Changed?.Invoke();
            return true;
        }

        public bool TryMerge(InventoryItem source, InventoryItem target)
        {
            return TryMerge(this, source, this, target);
        }

        public static bool TryMerge(Inventory from, InventoryItem source, Inventory to, InventoryItem target)
        {
            if (from == null || to == null || !from.Contains(source) || !to.Contains(target) || !source.CanMergeInto(target))
                return false;

            int moved = Mathf.Min(source.Count, target.Definition.MaxStack - target.Count);
            target.Count += moved;
            source.Count -= moved;

            if (source.Count <= 0)
                from.Detach(source);

            from.Changed?.Invoke();
            if (to != from)
                to.Changed?.Invoke();
            return true;
        }

        public static bool TryTransfer(Inventory from, InventoryItem item, Inventory to, Vector2Int position, bool rotated)
        {
            if (from == null || to == null || !from.Contains(item))
                return false;

            if (from == to)
                return from.TryMove(item, position, rotated);

            if (rotated && !to.allowRotation || !to.CanPlace(item.Definition.GetSize(rotated), position))
                return false;

            from.Remove(item);
            return to.TryAddAt(item.Definition, item.Instance, position, rotated, item.Count);
        }

        public bool Remove(InventoryItem item)
        {
            if (!Contains(item))
                return false;

            Detach(item);
            Changed?.Invoke();
            return true;
        }

        public int Remove(ItemDefinition definition, int count)
        {
            if (definition == null || count <= 0)
                return 0;

            int removed = 0;
            for (int i = ItemList.Count - 1; i >= 0 && removed < count; i--)
            {
                InventoryItem item = ItemList[i];
                if (item.Definition != definition || item.Instance != null)
                    continue;

                int taken = Mathf.Min(item.Count, count - removed);
                item.Count -= taken;
                removed += taken;

                if (item.Count <= 0)
                    Detach(item);
            }

            if (removed > 0)
                Changed?.Invoke();
            return removed;
        }

        public int Count(ItemDefinition definition)
        {
            if (definition == null)
                return 0;

            int total = 0;
            foreach (InventoryItem item in ItemList)
            {
                if (item.Definition == definition)
                    total += item.Count;
            }

            return total;
        }

        public bool Use(InventoryItem item, GameObject user)
        {
            if (!Contains(item) || !item.Definition.IsUsable || !item.Definition.Use(user))
                return false;

            item.Count--;
            if (item.Count <= 0)
                Detach(item);

            Changed?.Invoke();
            return true;
        }

        public bool Contains(InventoryItem item)
        {
            return item != null && ItemList.Contains(item);
        }

        private int AddSilently(ItemDefinition definition, int count)
        {
            if (definition == null || count <= 0)
                return Mathf.Max(0, count);

            foreach (InventoryItem item in ItemList)
            {
                if (count <= 0)
                    break;

                if (item.Definition != definition || !item.IsStackable || item.Count >= definition.MaxStack)
                    continue;

                int added = Mathf.Min(definition.MaxStack - item.Count, count);
                item.Count += added;
                count -= added;
            }

            while (count > 0 && TryFindSpace(definition, out Vector2Int position, out bool rotated))
            {
                int added = Mathf.Min(definition.MaxStack, count);
                Insert(new InventoryItem(definition, added, null, position, rotated));
                count -= added;
            }

            return count;
        }

        private void Insert(InventoryItem item)
        {
            ItemList.Add(item);
            Occupy(item, item);
        }

        private void Detach(InventoryItem item)
        {
            Occupy(item, null);
            ItemList.Remove(item);
        }

        private void Occupy(InventoryItem item, InventoryItem value)
        {
            InventoryItem[] grid = cells ?? Cells;
            Vector2Int size = item.Size;
            for (int y = item.Position.y; y < item.Position.y + size.y; y++)
            {
                for (int x = item.Position.x; x < item.Position.x + size.x; x++)
                    grid[y * columns + x] = value;
            }
        }

        private bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < columns && cell.y < rows;
        }
    }
}
