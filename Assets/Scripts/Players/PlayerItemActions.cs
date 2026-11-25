using RUSH.Items;
using UnityEngine;

namespace RUSH.Players
{
    public class PlayerItemActions : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Inventory inventory;
        [SerializeField] private Camera viewCamera;

        [Header("Drop")]
        [SerializeField, Min(0f)] private float throwSpeed = 3f;
        [SerializeField] private Vector3 dropOffset = new(0f, -0.2f, 0.8f);

        public Inventory Inventory => inventory;

        private void Update()
        {
            if (input.HealPressed)
                UseFirst<HealingItemDefinition>();
        }

        public bool UseFirst<T>() where T : ItemDefinition
        {
            return inventory.Use(inventory.IndexOf<T>(), gameObject);
        }

        public bool Use(int index)
        {
            return inventory.Use(index, gameObject);
        }

        public bool Drop(int index)
        {
            ItemStack stack = inventory[index];
            if (stack.IsEmpty || stack.Definition.WorldPrefab == null)
                return false;

            stack = inventory.Take(index);
            Transform view = viewCamera.transform;
            Quaternion rotation = Quaternion.Euler(0f, view.eulerAngles.y, 0f);
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
