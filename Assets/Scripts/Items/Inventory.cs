using System;
using UnityEngine;

namespace RUSH.Items
{
    public class Inventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int columns = 5;
        [SerializeField, Min(1)] private int rows = 4;
        [SerializeField] private ItemStack[] startingItems = Array.Empty<ItemStack>();

        [NonSerialized] private ItemStack[] slots;

        public event Action Changed;

        public int Columns => columns;
        public int Rows => rows;
        public int Capacity => Slots.Length;

        private ItemStack[] Slots => slots != null && slots.Length == columns * rows ? slots : slots = new ItemStack[columns * rows];

        public ItemStack this[int index] => Slots[index];

        private void Awake()
        {
            foreach (ItemStack stack in startingItems)
            {
                if (!stack.IsEmpty)
                    AddSilently(stack.Definition, stack.Count);
            }
        }

        public int Add(ItemDefinition definition, int count)
        {
            int left = AddSilently(definition, count);
            if (left != count)
                Changed?.Invoke();
            return left;
        }

        public int Remove(ItemDefinition definition, int count)
        {
            if (definition == null || count <= 0)
                return 0;

            int removed = 0;
            for (int i = Slots.Length - 1; i >= 0 && removed < count; i--)
            {
                if (Slots[i].Definition != definition)
                    continue;

                int taken = Mathf.Min(Slots[i].Count, count - removed);
                Slots[i] = Slots[i].WithCount(Slots[i].Count - taken);
                removed += taken;
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
            foreach (ItemStack stack in Slots)
            {
                if (stack.Definition == definition)
                    total += stack.Count;
            }

            return total;
        }

        public int IndexOf<T>() where T : ItemDefinition
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (!Slots[i].IsEmpty && Slots[i].Definition is T)
                    return i;
            }

            return -1;
        }

        public ItemStack Take(int index)
        {
            if (!IsValid(index) || Slots[index].IsEmpty)
                return ItemStack.Empty;

            ItemStack stack = Slots[index];
            Slots[index] = ItemStack.Empty;
            Changed?.Invoke();
            return stack;
        }

        public void Move(int from, int to)
        {
            if (!IsValid(from) || !IsValid(to) || from == to || Slots[from].IsEmpty)
                return;

            ItemStack source = Slots[from];
            ItemStack target = Slots[to];

            if (source.CanMerge(target) && target.FreeSpace > 0)
            {
                int moved = Mathf.Min(source.Count, target.FreeSpace);
                Slots[to] = target.WithCount(target.Count + moved);
                Slots[from] = source.WithCount(source.Count - moved);
            }
            else
            {
                Slots[to] = source;
                Slots[from] = target;
            }

            Changed?.Invoke();
        }

        public bool Use(int index, GameObject user)
        {
            if (!IsValid(index) || Slots[index].IsEmpty)
                return false;

            ItemStack stack = Slots[index];
            if (!stack.Definition.IsUsable || !stack.Definition.Use(user))
                return false;

            Slots[index] = stack.WithCount(stack.Count - 1);
            Changed?.Invoke();
            return true;
        }

        private int AddSilently(ItemDefinition definition, int count)
        {
            if (definition == null || count <= 0)
                return Mathf.Max(0, count);

            for (int i = 0; i < Slots.Length && count > 0; i++)
            {
                if (Slots[i].Definition != definition || Slots[i].FreeSpace <= 0)
                    continue;

                int added = Mathf.Min(Slots[i].FreeSpace, count);
                Slots[i] = Slots[i].WithCount(Slots[i].Count + added);
                count -= added;
            }

            for (int i = 0; i < Slots.Length && count > 0; i++)
            {
                if (!Slots[i].IsEmpty)
                    continue;

                int added = Mathf.Min(definition.MaxStack, count);
                Slots[i] = new ItemStack(definition, added);
                count -= added;
            }

            return count;
        }

        private bool IsValid(int index)
        {
            return index >= 0 && index < Slots.Length;
        }
    }
}
