using RUSH.Weapons;
using UnityEngine;

namespace RUSH.Players
{
    [RequireComponent(typeof(Animator))]
    public class PlayerArms : MonoBehaviour
    {
        private static readonly int MovementParameter = Animator.StringToHash("Movement");
        private static readonly int AimingParameter = Animator.StringToHash("Aiming");
        private static readonly int AimParameter = Animator.StringToHash("Aim");
        private static readonly int RunningParameter = Animator.StringToHash("Running");

        private static readonly int FireState = Animator.StringToHash("Fire");
        private static readonly int FireEmptyState = Animator.StringToHash("Fire Empty");
        private static readonly int ReloadState = Animator.StringToHash("Reload");
        private static readonly int ReloadEmptyState = Animator.StringToHash("Reload Empty");
        private static readonly int InspectState = Animator.StringToHash("Inspect");
        private static readonly int DefaultState = Animator.StringToHash("Default");
        private static readonly int UnholsterState = Animator.StringToHash("Unholster");

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Renderer[] renderers;
        [SerializeField, Min(0f)] private float locomotionDamping = 0.15f;
        [SerializeField, Min(0f)] private float aimingDamping = 0.075f;
        [SerializeField, Min(0f)] private float fireTransition = 0.05f;

        private Animator animator;
        private int overlayLayer;
        private int actionsLayer;
        private int holsterLayer;

        public Weapon Weapon { get; private set; }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            CacheLayers();
        }

        public void SetWeapon(Weapon weapon)
        {
            Weapon = weapon;
            SetVisible(weapon != null);

            if (weapon == null)
                return;

            animator.runtimeAnimatorController = weapon.ArmsController;
            animator.Rebind();
            CacheLayers();
        }

        public void UpdateLocomotion(float movement, bool running, bool aiming)
        {
            float deltaTime = Time.deltaTime;
            animator.SetFloat(MovementParameter, movement, locomotionDamping, deltaTime);
            animator.SetFloat(AimingParameter, aiming ? 1f : 0f, aimingDamping, deltaTime);
            animator.SetBool(AimParameter, aiming);
            animator.SetBool(RunningParameter, running);
        }

        public void PlayFire() => animator.CrossFade(FireState, fireTransition, overlayLayer, 0f);

        public void PlayFireEmpty() => animator.CrossFade(FireEmptyState, fireTransition, overlayLayer, 0f);

        public void PlayReload(bool empty) => animator.Play(empty ? ReloadEmptyState : ReloadState, actionsLayer, 0f);

        public void PlayInspect() => animator.Play(InspectState, actionsLayer, 0f);

        public void StopActions() => animator.Play(DefaultState, actionsLayer, 0f);

        public void CompleteUnholster() => animator.Play(UnholsterState, holsterLayer, 1f);

        public void PlayWeaponSound(WeaponSoundType type)
        {
            if (Weapon != null)
                PlayClip(Weapon.Sounds.Get(type));
        }

        public void PlayClip(AudioClip clip, float volume = 1f)
        {
            if (clip != null)
                audioSource.PlayOneShot(clip, volume);
        }

        private void SetVisible(bool visible)
        {
            foreach (Renderer target in renderers)
                target.enabled = visible;
        }

        private void CacheLayers()
        {
            overlayLayer = animator.GetLayerIndex("Layer Overlay");
            actionsLayer = animator.GetLayerIndex("Layer Actions");
            holsterLayer = animator.GetLayerIndex("Layer Holster");
        }
    }
}
