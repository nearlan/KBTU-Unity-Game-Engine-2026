using RUSH.Core;
using UnityEngine;

namespace RUSH.Items
{
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField, Min(1)] private int count = 1;
        [SerializeField] private AudioClip pickupClip;
        [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.8f;

        public ItemDefinition Definition => definition;
        public int Count => count;

        public static ItemPickup Spawn(ItemStack stack, Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            if (stack.IsEmpty || stack.Definition.WorldPrefab == null)
                return null;

            ItemPickup pickup = Instantiate(stack.Definition.WorldPrefab, position, rotation);
            pickup.definition = stack.Definition;
            pickup.count = stack.Count;

            if (pickup.TryGetComponent(out Rigidbody body))
                body.linearVelocity = velocity;

            return pickup;
        }

        public bool Interact(GameObject interactor)
        {
            if (definition == null || interactor == null || !interactor.TryGetComponent(out Inventory inventory))
                return false;

            int left = inventory.Add(definition, count);
            if (left == count)
                return false;

            if (pickupClip != null)
                AudioSource.PlayClipAtPoint(pickupClip, transform.position, pickupVolume);

            count = left;
            if (count == 0)
                Destroy(gameObject);

            return true;
        }
    }
}
