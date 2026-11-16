using KBTU.Core;
using KBTU.Weapons;
using UnityEngine;

namespace KBTU.Players
{
    public class PlayerWeaponHandler : MonoBehaviour
    {
        private static readonly Vector3 ScreenCenter = new(0.5f, 0.5f, 0f);

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerArms arms;
        [SerializeField] private ArmsAnimationEvents animationEvents;
        [SerializeField] private Transform weaponSocket;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Weapon startingWeapon;

        [Header("Pickup")]
        [SerializeField, Min(0f)] private float pickupRange = 3f;
        [SerializeField, Min(0f)] private float pickupRadius = 0.35f;
        [SerializeField] private LayerMask pickupMask = ~0;

        [Header("Drop")]
        [SerializeField, Min(0f)] private float throwSpeed = 4f;
        [SerializeField] private Vector3 dropOffset = new(0f, -0.2f, 0.7f);

        [Header("Layers")]
        [SerializeField] private string equippedLayerName = "FirstPerson";
        [SerializeField] private string worldLayerName = "Default";

        private int equippedLayer;
        private int worldLayer;
        private bool isReloading;
        private bool isInspecting;

        public Weapon CurrentWeapon { get; private set; }
        public bool IsAiming { get; private set; }
        public bool IsBusy => isReloading || isInspecting;
        public bool IsFiring => CurrentWeapon != null && input.AttackHeld && CurrentWeapon.HasAmmunition;

        private Ray AimRay => viewCamera.ViewportPointToRay(ScreenCenter);

        private void Awake()
        {
            equippedLayer = Mathf.Max(0, LayerMask.NameToLayer(equippedLayerName));
            worldLayer = Mathf.Max(0, LayerMask.NameToLayer(worldLayerName));
        }

        private void OnEnable()
        {
            animationEvents.CasingEjected += HandleCasingEjected;
            animationEvents.AmmunitionFilled += HandleAmmunitionFilled;
            animationEvents.ReloadEnded += HandleReloadEnded;
            animationEvents.InspectEnded += HandleInspectEnded;
        }

        private void OnDisable()
        {
            animationEvents.CasingEjected -= HandleCasingEjected;
            animationEvents.AmmunitionFilled -= HandleAmmunitionFilled;
            animationEvents.ReloadEnded -= HandleReloadEnded;
            animationEvents.InspectEnded -= HandleInspectEnded;
            IsAiming = false;
        }

        private void Start()
        {
            arms.SetWeapon(null);

            if (startingWeapon != null)
                Equip(Instantiate(startingWeapon), false);
        }

        private void Update()
        {
            if (input.DropPressed)
                Drop();

            if (input.InteractPressed)
                TryPickUp();

            if (CurrentWeapon == null)
            {
                IsAiming = false;
                return;
            }

            if (input.ReloadPressed)
                TryReload();

            HandleFiring();
            UpdateAiming();
        }

        private void UpdateAiming()
        {
            if (!input.AimHeld || IsBusy)
                IsAiming = false;
            else if (input.AimPressed)
                IsAiming = true;
        }

        private void HandleFiring()
        {
            bool pressed = input.AttackPressed;
            bool held = input.AttackHeld && CurrentWeapon.IsAutomatic;

            if (!pressed && !held)
                return;

            if (isReloading || !CurrentWeapon.IsReadyToFire)
                return;

            if (CurrentWeapon.HasAmmunition)
            {
                InterruptInspect();
                CurrentWeapon.Fire(AimRay);
                arms.PlayFire();
            }
            else if (pressed)
            {
                InterruptInspect();
                CurrentWeapon.FireEmpty();
                arms.PlayFireEmpty();
            }
        }

        private void TryReload()
        {
            if (IsBusy || !CurrentWeapon.CanReload)
                return;

            isReloading = true;
            arms.PlayReload(!CurrentWeapon.HasAmmunition);
            CurrentWeapon.BeginReload();
        }

        private void InterruptInspect()
        {
            if (!isInspecting)
                return;

            isInspecting = false;
            arms.StopActions();
        }

        private void TryPickUp()
        {
            if (!PhysicsQuery.SphereCast(AimRay, pickupRadius, pickupRange, pickupMask, transform, out RaycastHit hit))
                return;

            Weapon weapon = hit.collider.GetComponentInParent<Weapon>();
            if (weapon == null || weapon.IsEquipped)
                return;

            Drop();
            Equip(weapon, true);
        }

        private void Equip(Weapon weapon, bool inspect)
        {
            CurrentWeapon = weapon;
            isReloading = false;
            isInspecting = false;

            weapon.Equip(gameObject, weaponSocket, equippedLayer);
            arms.SetWeapon(weapon);

            if (!inspect)
                return;

            arms.CompleteUnholster();
            arms.PlayInspect();
            isInspecting = true;
        }

        private void Drop()
        {
            if (CurrentWeapon == null)
                return;

            Weapon weapon = CurrentWeapon;
            CurrentWeapon = null;
            isReloading = false;
            isInspecting = false;
            IsAiming = false;
            arms.SetWeapon(null);

            Transform view = viewCamera.transform;
            weapon.Drop(view.TransformPoint(dropOffset), view.rotation, view.forward * throwSpeed, worldLayer);
        }

        private void HandleCasingEjected()
        {
            if (CurrentWeapon != null)
                CurrentWeapon.EjectCasing();
        }

        private void HandleAmmunitionFilled()
        {
            if (isReloading && CurrentWeapon != null)
                CurrentWeapon.FillAmmunition();
        }

        private void HandleReloadEnded()
        {
            isReloading = false;
        }

        private void HandleInspectEnded()
        {
            isInspecting = false;
        }
    }
}
