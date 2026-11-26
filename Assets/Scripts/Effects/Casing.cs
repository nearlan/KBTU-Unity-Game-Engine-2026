using RUSH.Core;
using UnityEngine;

namespace RUSH.Effects
{
    [RequireComponent(typeof(Rigidbody), typeof(AudioSource))]
    public class Casing : MonoBehaviour
    {
        [SerializeField] private AudioClip[] impactClips;
        [SerializeField, Min(0f)] private float lifetime = 5f;
        [SerializeField, Min(0f)] private float spin = 25f;

        private Rigidbody body;
        private AudioSource audioSource;
        private bool hasLanded;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            audioSource = GetComponent<AudioSource>();
            Destroy(gameObject, lifetime);
        }

        public void Launch(Vector3 velocity, Collider ignoredCollider)
        {
            if (ignoredCollider != null && TryGetComponent(out Collider ownCollider))
                Physics.IgnoreCollision(ownCollider, ignoredCollider);

            body.linearVelocity = velocity;
            body.angularVelocity = Random.insideUnitSphere * spin;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasLanded)
                return;

            hasLanded = true;
            AudioClip clip = impactClips.GetRandom();
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }
    }
}
