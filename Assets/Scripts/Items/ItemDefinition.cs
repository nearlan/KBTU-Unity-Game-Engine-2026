using UnityEngine;

namespace RUSH.Items
{
    [CreateAssetMenu(menuName = "RUSH/Items/Item", fileName = "Item")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(1)] private int maxStack = 1;
        [SerializeField] private ItemPickup worldPrefab;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public int MaxStack => maxStack;
        public ItemPickup WorldPrefab => worldPrefab;

        public virtual bool IsUsable => false;

        public virtual bool Use(GameObject user)
        {
            return false;
        }
    }
}
