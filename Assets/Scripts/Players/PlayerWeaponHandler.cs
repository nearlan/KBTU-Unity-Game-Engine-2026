using System;
using RUSH.Core;
using RUSH.Items;
using RUSH.Weapons;
using UnityEngine;

namespace RUSH.Players
{
    public class PlayerWeaponHandler : MonoBehaviour
    {
        private static readonly Vector3 ScreenCenter = new(0.5f, 0.5f, 0f);
        private static readonly int SlotCount = Enum.GetValues(typeof(WeaponSlot)).Length;

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Inventory inventory;
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

        [Header("Switching")]
        [SerializeField, Min(0f)] private float scrollThreshold = 0.01f;

        [Header("Layers")]
        [SerializeField] private string equippedLayerName = "FirstPerson";
        [SerializeField] private string worldLayerName = "Default";

        private readonly Weapon[] slots = new Weapon[SlotCount];
        private int activeSlot;
        private int equippedLayer;
        private int worldLayer;
        private bool isReloading;
        private bool isInspecting;

        public event Action<Weapon> Fired;
        public event Action WeaponsChanged;

        public Weapon CurrentWeapon => slots[activeSlot];
        public WeaponSlot ActiveSlot => (WeaponSlot)activeSlot;
        public bool InputBlocked { get; set; }
        public bool IsAiming { get; private set; }
        public bool IsBusy => isReloading || isInspecting;
        public bool IsFiring => CurrentWeapon != null && !InputBlocked && input.AttackHeld && CurrentWeapon.HasAmmunition;
        public bool HasUnlimitedAmmunition => CurrentWeapon != null && CurrentWeapon.AmmoType == null;
        public int ReserveAmmunition => CurrentWeapon != null ? AvailableRounds(CurrentWeapon) : 0;

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
                Store(Instantiate(startingWeapon), false);
        }

        private void Update()
        {
            if (InputBlocked)
            {
                IsAiming = false;
                return;
            }

            if (input.DropPressed)
                Drop(ActiveSlot);

            if (input.InteractPressed)
                TryPickUp();

            HandleSwitching();

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

        public Weapon GetWeapon(WeaponSlot slot)
        {
            return slots[(int)slot];
        }

        public void Select(WeaponSlot slot)
        {
            int index = (int)slot;
            if (index == activeSlot || slots[index] == null)
                return;

            Holster(CurrentWeapon);
            activeSlot = index;
            Draw(slots[index], false);
            WeaponsChanged?.Invoke();
        }

        public void Drop(WeaponSlot slot)
        {
            Weapon weapon = TakeFromSlot((int)slot, true);
            if (weapon == null)
                return;

            Throw(weapon);
            WeaponsChanged?.Invoke();
        }

        public bool MoveToBackpack(WeaponSlot slot, Vector2Int position, bool rotated)
        {
            int index = (int)slot;
            Weapon weapon = slots[index];
            if (weapon == null || weapon.ItemDefinition == null || inventory == null)
                return false;

            if (!inventory.CanPlace(weapon.ItemDefinition.GetSize(rotated), position))
                return false;

            TakeFromSlot(index, true);
            inventory.TryAddAt(weapon.ItemDefinition, weapon, position, rotated);
            WeaponsChanged?.Invoke();
            return true;
        }

        public bool EquipFromBackpack(InventoryItem item)
        {
            if (inventory == null || item == null || item.Instance is not Weapon weapon || !inventory.Contains(item))
                return false;

            int index = (int)weapon.Slot;
            Weapon occupant = slots[index];
            Vector2Int position = item.Position;
            bool rotated = item.Rotated;

            inventory.Remove(item);

            if (occupant != null)
            {
                if (!FindBackpackSpace(occupant.ItemDefinition, position, rotated, out Vector2Int spot, out bool spotRotated))
                {
                    inventory.TryAddAt(item.Definition, weapon, position, rotated);
                    return false;
                }

                TakeFromSlot(index, false);
                inventory.TryAddAt(occupant.ItemDefinition, occupant, spot, spotRotated);
            }

            PutInSlot(weapon, index, false);
            return true;
        }

        public bool DropFromBackpack(InventoryItem item)
        {
            if (inventory == null || item == null || item.Instance is not Weapon weapon || !inventory.Remove(item))
                return false;

            Throw(weapon);
            return true;
        }

        private void HandleSwitching()
        {
            if (input.PrimaryWeaponPressed)
            {
                Select(WeaponSlot.Primary);
                return;
            }

            if (input.SecondaryWeaponPressed)
            {
                Select(WeaponSlot.Secondary);
                return;
            }

            float scroll = input.CycleWeapon;
            if (Mathf.Abs(scroll) > scrollThreshold)
                Cycle(scroll > 0f ? -1 : 1);
        }

        private void Cycle(int direction)
        {
            for (int step = 1; step < SlotCount; step++)
            {
                int index = (activeSlot + direction * step + SlotCount) % SlotCount;
                if (slots[index] == null)
                    continue;

                Select((WeaponSlot)index);
                return;
            }
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
                Fired?.Invoke(CurrentWeapon);
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
            if (IsBusy || !CurrentWeapon.CanReload || AvailableRounds(CurrentWeapon) <= 0)
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

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(gameObject);
                return;
            }

            Weapon weapon = hit.collider.GetComponentInParent<Weapon>();
            if (weapon == null || weapon.IsEquipped)
                return;

            Store(weapon, true);
        }

        private bool Store(Weapon weapon, bool inspect)
        {
            int index = (int)weapon.Slot;
            if (slots[index] == null)
            {
                weapon.Equip(gameObject, weaponSocket, equippedLayer);
                PutInSlot(weapon, index, inspect);
                return true;
            }

            if (inventory == null || weapon.ItemDefinition == null
                || !inventory.TryFindSpace(weapon.ItemDefinition, out Vector2Int position, out bool rotated))
                return false;

            weapon.Equip(gameObject, weaponSocket, equippedLayer);
            weapon.gameObject.SetActive(false);
            inventory.TryAddAt(weapon.ItemDefinition, weapon, position, rotated);
            return true;
        }

        private void PutInSlot(Weapon weapon, int index, bool inspect)
        {
            Holster(CurrentWeapon);
            slots[index] = weapon;
            activeSlot = index;
            Draw(weapon, inspect);
            WeaponsChanged?.Invoke();
        }

        private Weapon TakeFromSlot(int index, bool switchToOther)
        {
            Weapon weapon = slots[index];
            if (weapon == null)
                return null;

            slots[index] = null;
            bool wasActive = index == activeSlot;
            if (wasActive)
            {
                ResetActionState();
                arms.SetWeapon(null);
            }

            weapon.gameObject.SetActive(false);

            if (wasActive && switchToOther)
            {
                int other = FindOccupiedSlot();
                if (other >= 0)
                {
                    activeSlot = other;
                    Draw(slots[other], false);
                }
            }

            return weapon;
        }

        private void Throw(Weapon weapon)
        {
            weapon.gameObject.SetActive(true);
            Transform view = viewCamera.transform;
            weapon.Drop(view.TransformPoint(dropOffset), view.rotation, view.forward * throwSpeed, worldLayer);
        }

        private bool FindBackpackSpace(ItemDefinition definition, Vector2Int preferred, bool preferredRotated, out Vector2Int position, out bool rotated)
        {
            position = preferred;
            rotated = preferredRotated;
            if (definition == null)
                return false;

            if (inventory.CanPlace(definition.GetSize(rotated), position))
                return true;

            rotated = !preferredRotated;
            if (inventory.CanPlace(definition.GetSize(rotated), position))
                return true;

            return inventory.TryFindSpace(definition, out position, out rotated);
        }

        private void Holster(Weapon weapon)
        {
            if (weapon == null)
                return;

            ResetActionState();
            weapon.gameObject.SetActive(false);
        }

        private void Draw(Weapon weapon, bool inspect)
        {
            ResetActionState();
            weapon.gameObject.SetActive(true);
            arms.SetWeapon(weapon);

            if (!inspect)
                return;

            arms.CompleteUnholster();
            arms.PlayInspect();
            isInspecting = true;
        }

        private void ResetActionState()
        {
            isReloading = false;
            isInspecting = false;
            IsAiming = false;
        }

        private int FindOccupiedSlot()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    return i;
            }

            return -1;
        }

        private int AvailableRounds(Weapon weapon)
        {
            if (weapon.AmmoType == null)
                return int.MaxValue;

            return inventory != null ? inventory.Count(weapon.AmmoType) : 0;
        }

        private int TakeRounds(Weapon weapon, int rounds)
        {
            if (weapon.AmmoType == null)
                return rounds;

            return inventory != null ? inventory.Remove(weapon.AmmoType, rounds) : 0;
        }

        private void HandleCasingEjected()
        {
            if (CurrentWeapon != null)
                CurrentWeapon.EjectCasing();
        }

        private void HandleAmmunitionFilled()
        {
            if (!isReloading || CurrentWeapon == null)
                return;

            CurrentWeapon.FillAmmunition(TakeRounds(CurrentWeapon, CurrentWeapon.MissingRounds));
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
