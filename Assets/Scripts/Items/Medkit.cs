using KBTU.Core;
using UnityEngine;

namespace KBTU.Items
{
    public class Medkit : MonoBehaviour, IInteractable
    {
        [SerializeField, Min(1f)] private float healAmount = 35f;
        [SerializeField] private bool requireMissingHealth = true;
        [SerializeField] private AudioClip useClip;
        [SerializeField, Range(0f, 1f)] private float useVolume = 0.8f;

        public float HealAmount => healAmount;

        public bool Interact(GameObject interactor)
        {
            if (interactor == null || !interactor.TryGetComponent(out Entity entity))
                return false;

            if (requireMissingHealth && entity.CurrentHealth >= entity.MaxHealth)
            {
                Debug.Log($"[Medkit] HP уже полное ({entity.CurrentHealth:0}/{entity.MaxHealth:0})", this);
                return false;
            }

            if (!entity.Heal(healAmount))
                return false;

            Debug.Log($"[Medkit] Использована аптечка +{healAmount:0} HP ({entity.CurrentHealth:0}/{entity.MaxHealth:0})", this);

            if (useClip != null)
                AudioSource.PlayClipAtPoint(useClip, transform.position, useVolume);

            Destroy(gameObject);
            return true;
        }
    }
}
