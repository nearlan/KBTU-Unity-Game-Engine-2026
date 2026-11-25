using UnityEngine;

namespace RUSH.Players
{
    [RequireComponent(typeof(AudioSource))]
    public class PlayerFootsteps : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private AudioClip walkClip;
        [SerializeField] private AudioClip sprintClip;
        [SerializeField, Min(0f)] private float minimumSpeed = 0.5f;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
        }

        private void Update()
        {
            bool isWalking = movement.enabled && movement.IsGrounded && movement.PlanarSpeed > minimumSpeed;
            if (!isWalking)
            {
                if (audioSource.isPlaying)
                    audioSource.Stop();

                return;
            }

            AudioClip clip = movement.IsSprinting ? sprintClip : walkClip;
            if (audioSource.clip != clip)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
            else if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }
}
