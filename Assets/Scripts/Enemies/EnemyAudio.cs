using KBTU.Core;
using UnityEngine;

namespace KBTU.Enemies
{
    [RequireComponent(typeof(AudioSource))]
    public class EnemyAudio : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField] private AudioClip[] idleClips;
        [SerializeField] private AudioClip[] damageClips;
        [SerializeField] private AudioClip[] deathClips;

        [Header("Idle")]
        [SerializeField] private Vector2 idleInterval = new(3f, 8f);
        [SerializeField] private Vector2 pitchRange = new(0.92f, 1.08f);
        [SerializeField, Range(0f, 1f)] private float idleVolume = 0.6f;

        [Header("Combat")]
        [SerializeField, Range(0f, 1f)] private float damageVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float deathVolume = 1f;

        [Header("3D Audio")]
        [SerializeField, Min(0.1f)] private float minDistance = 2f;
        [SerializeField, Min(1f)] private float maxDistance = 25f;

        private AudioSource audioSource;
        private float nextIdleTime;
        private bool isDead;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;

            float initialMax = Mathf.Max(0.5f, idleInterval.y);
            nextIdleTime = Time.time + Random.Range(0.5f, initialMax);
        }

        private void Update()
        {
            if (isDead || idleClips == null || idleClips.Length == 0 || Time.time < nextIdleTime)
                return;

            ScheduleNextIdle();
            PlayRandomClip(idleClips, idleVolume);
        }

        public void PlayDamage()
        {
            if (isDead)
                return;

            PlayRandomClip(damageClips, damageVolume);
        }

        public void PlayDeath()
        {
            if (isDead)
                return;

            isDead = true;
            audioSource.Stop();
            PlayRandomClip(deathClips, deathVolume);
        }

        private void ScheduleNextIdle()
        {
            float min = Mathf.Min(idleInterval.x, idleInterval.y);
            float max = Mathf.Max(idleInterval.x, idleInterval.y);
            nextIdleTime = Time.time + Random.Range(Mathf.Max(0.1f, min), Mathf.Max(0.1f, max));
        }

        private void PlayRandomClip(AudioClip[] clips, float volume)
        {
            AudioClip clip = clips.GetRandom();
            if (clip == null)
                return;

            float minPitch = Mathf.Min(pitchRange.x, pitchRange.y);
            float maxPitch = Mathf.Max(pitchRange.x, pitchRange.y);
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
