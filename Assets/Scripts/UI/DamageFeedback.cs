using RUSH.Core;
using UnityEngine;

namespace RUSH.UI
{
    public class DamageFeedback : MonoBehaviour
    {
        private static readonly int HitTrigger = Animator.StringToHash("Hit");
        private static readonly int LowHealthBool = Animator.StringToHash("Active");

        [SerializeField] private Entity target;
        [SerializeField] private Animator vignetteAnimator;
        [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.3f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] damageClips;
        [SerializeField, Range(0f, 1f)] private float damageVolume = 0.8f;

        private void OnEnable()
        {
            if (target == null)
                return;

            target.Damaged += HandleDamaged;
            target.Healed += HandleHealed;
            target.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (target == null)
                return;

            target.Damaged -= HandleDamaged;
            target.Healed -= HandleHealed;
            target.Died -= HandleDied;
        }

        private void HandleDamaged(Entity entity, DamageInfo damage)
        {
            if (vignetteAnimator != null)
                vignetteAnimator.SetTrigger(HitTrigger);

            RefreshLowHealth();
            PlayDamageSound();
        }

        private void HandleHealed(Entity entity, float amount)
        {
            RefreshLowHealth();
        }

        private void HandleDied(Entity entity)
        {
            if (vignetteAnimator != null)
                vignetteAnimator.SetBool(LowHealthBool, false);
        }

        private void RefreshLowHealth()
        {
            if (vignetteAnimator == null || target.IsDead || target.MaxHealth <= 0f)
                return;

            bool isLow = target.CurrentHealth / target.MaxHealth <= lowHealthThreshold;
            vignetteAnimator.SetBool(LowHealthBool, isLow);
        }

        private void PlayDamageSound()
        {
            if (audioSource == null || damageClips == null || damageClips.Length == 0)
                return;

            AudioClip clip = damageClips[Random.Range(0, damageClips.Length)];
            if (clip != null)
                audioSource.PlayOneShot(clip, damageVolume);
        }
    }
}
