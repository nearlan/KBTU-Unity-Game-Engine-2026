using RUSH.Items;
using UnityEngine;

namespace RUSH.Players
{
    public class PlayerItemActions : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private Camera viewCamera;

        [Header("Drop")]
        [SerializeField, Min(0f)] private float throwSpeed = 3f;
        [SerializeField] private Vector3 dropOffset = new(0f, -0.2f, 0.8f);

        public Inventory Inventory => inventory;

        public bool Use(Inventory source, InventoryItem item)
        {
            return source != null && source.Use(item, gameObject);
        }

        public bool Drop(Inventory source, InventoryItem item)
        {
            if (source == null || item == null || item.Instance != null || item.Definition.WorldPrefab == null || !source.Remove(item))
                return false;

            Transform view = viewCamera.transform;
            Quaternion rotation = Quaternion.Euler(0f, view.eulerAngles.y, 0f);
            ItemStack stack = new(item.Definition, item.Count);
            ItemPickup pickup = ItemPickup.Spawn(stack, view.TransformPoint(dropOffset), rotation, view.forward * throwSpeed);

            if (pickup != null && TryGetComponent(out Collider ownCollider))
            {
                foreach (Collider pickupCollider in pickup.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(pickupCollider, ownCollider);
            }

            return true;
        }
    }
}
