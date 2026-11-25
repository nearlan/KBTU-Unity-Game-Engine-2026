using RUSH.Players;
using UnityEngine;

namespace RUSH.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class Crosshair : MonoBehaviour
    {
        private static readonly int SpreadParameter = Animator.StringToHash("Aim");

        [SerializeField] private PlayerWeaponHandler weaponHandler;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private Animator reticleAnimator;
        [SerializeField] private bool hideWhileAiming = true;
        [SerializeField, Min(0f)] private float fadeSpeed = 12f;

        [Header("Spread")]
        [SerializeField, Range(0f, 1f)] private float movingSpread = 0.4f;
        [SerializeField, Range(0f, 1f)] private float sprintingSpread = 0.8f;
        [SerializeField, Range(0f, 1f)] private float firingSpread = 1f;
        [SerializeField, Min(0f)] private float spreadSpeed = 6f;

        private CanvasGroup canvasGroup;
        private float spread;

        private bool ShouldBeVisible =>
            weaponHandler != null
            && weaponHandler.isActiveAndEnabled
            && weaponHandler.CurrentWeapon != null
            && !(hideWhileAiming && weaponHandler.IsAiming);

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float target = ShouldBeVisible ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * dt);

            if (reticleAnimator == null)
                return;

            spread = Mathf.MoveTowards(spread, EvaluateSpread(), spreadSpeed * dt);
            reticleAnimator.SetFloat(SpreadParameter, spread);
        }

        private float EvaluateSpread()
        {
            float result = 0f;

            if (movement != null && movement.isActiveAndEnabled)
                result = movement.IsSprinting ? sprintingSpread : movement.MoveAmount * movingSpread;

            if (weaponHandler != null && weaponHandler.isActiveAndEnabled && weaponHandler.IsFiring)
                result = Mathf.Max(result, firingSpread);

            return result;
        }
    }
}
