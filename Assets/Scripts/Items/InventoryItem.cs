using UnityEngine;

namespace RUSH.Items
{
    public sealed class InventoryItem
    {
        internal InventoryItem(ItemDefinition definition, int count, Component instance, Vector2Int position, bool rotated)
        {
            Definition = definition;
            Count = count;
            Instance = instance;
            Position = position;
            Rotated = rotated;
        }

        public ItemDefinition Definition { get; }
        public Component Instance { get; }
        public int Count { get; internal set; }
        public Vector2Int Position { get; internal set; }
        public bool Rotated { get; internal set; }

        public Vector2Int Size => Definition.GetSize(Rotated);
        public bool IsStackable => Instance == null && Definition.MaxStack > 1;

        public bool CanMergeInto(InventoryItem target)
        {
            return target != null && target != this && IsStackable && target.IsStackable
                   && target.Definition == Definition && target.Count < Definition.MaxStack;
        }
    }
}
