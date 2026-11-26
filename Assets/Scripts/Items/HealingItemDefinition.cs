using RUSH.Core;
using UnityEngine;

namespace RUSH.Items
{
    [CreateAssetMenu(menuName = "RUSH/Items/Healing", fileName = "Healing")]
    public class HealingItemDefinition : ItemDefinition
    {
        [SerializeField, Min(1f)] private float healAmount = 35f;
        [SerializeField] private AudioClip useClip;
        [SerializeField, Range(0f, 1f)] private float useVolume = 0.8f;

        public float HealAmount => healAmount;

        public override bool IsUsable => true;

        public override bool Use(GameObject user)
        {
            if (user == null || !user.TryGetComponent(out Entity entity))
                return false;

            if (!entity.Heal(healAmount))
                return false;

            if (useClip != null)
                AudioSource.PlayClipAtPoint(useClip, user.transform.position, useVolume);

            return true;
        }
    }
}
