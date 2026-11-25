using System;
using UnityEngine;

namespace RUSH.Items
{
    [Serializable]
    public struct ItemStack
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField, Min(0)] private int count;

        public ItemStack(ItemDefinition definition, int count)
        {
            this.definition = count > 0 ? definition : null;
            this.count = definition != null ? Mathf.Max(0, count) : 0;
        }

        public static ItemStack Empty => default;

        public ItemDefinition Definition => definition;
        public int Count => count;
        public bool IsEmpty => definition == null || count <= 0;
        public int FreeSpace => IsEmpty ? 0 : Mathf.Max(0, definition.MaxStack - count);

        public bool CanMerge(ItemStack other)
        {
            return !IsEmpty && !other.IsEmpty && definition == other.definition;
        }

        public ItemStack WithCount(int newCount)
        {
            return new ItemStack(definition, newCount);
        }
    }
}
