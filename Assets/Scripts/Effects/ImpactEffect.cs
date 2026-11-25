using RUSH.Core;
using UnityEngine;

namespace RUSH.Effects
{
    [RequireComponent(typeof(AudioSource))]
    public class ImpactEffect : MonoBehaviour
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Min(0.1f)] private float lifetime = 3f;

        private void Start()
        {
            AudioClip clip = clips.GetRandom();
            if (clip != null)
                GetComponent<AudioSource>().PlayOneShot(clip);

            Destroy(gameObject, lifetime);
        }
    }
}
