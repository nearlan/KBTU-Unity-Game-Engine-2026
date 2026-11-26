using System;
using System.Collections.Generic;
using RUSH.Core;
using RUSH.Items;
using RUSH.Weapons;
using UnityEngine;

namespace RUSH.Players
{
    public class PlayerWeaponHandler : MonoBehaviour
    {
        private static readonly Vector3 ScreenCenter = new(0.5f, 0.5f, 0f);

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Inventory inventory;
        [SerializeField] private Inventory hands;
        [SerializeField] private PlayerItemActions itemActions;
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

        private readonly List<InventoryItem> orderedHands = new();
        private InventoryItem selected;
        private int equippedLayer;
        private int worldLayer;
        private bool isReloading;
        private bool isInspecting;
        private bool suppressAutoSelect;

        public event Action<Weapon> Fired;
        public event Action SelectionChanged;

        public Inventory Hands => hands;
        public InventoryItem SelectedItem => selected;
        public Weapon CurrentWeapon => selected?.Instance as Weapon;
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
            hands.Changed += HandleHandsChanged;
        }

        private void OnDisable()
        {
            animationEvents.CasingEjected -= HandleCasingEjected;
            animationEvents.AmmunitionFilled -= HandleAmmunitionFilled;
            animationEvents.ReloadEnded -= HandleReloadEnded;
            animationEvents.InspectEnded -= HandleInspectEnded;
            hands.Changed -= HandleHandsChanged;
            IsAiming = false;
        }

        private void Start()
        {
            arms.SetWeapon(null);

            if (startingWeapon != null)
                StoreWeapon(Instantiate(startingWeapon), false);
        }

        private void Update()
        {
            if (InputBlocked)
            {
                IsAiming = false;
                return;
            }

            if (input.DropPressed && selected != null)
                DropItem(hands, selected);

            if (input.InteractPressed)
                TryPickUp();

            HandleSwitching();

            if (CurrentWeapon == null)
            {
                IsAiming = false;
                if (selected != null && input.AttackPressed)
                    itemActions.Use(hands, selected);
                return;
            }

            if (input.ReloadPressed)
                TryReload();

            HandleFiring();
            UpdateAiming();
        }

        public void Select(InventoryItem item, bool inspect = false)
        {
            if (item == null || item == selected || !hands.Contains(item))
                return;

            Holster(CurrentWeapon);
            selected = item;

            if (CurrentWeapon != null)
            {
                Draw(CurrentWeapon, inspect);
            }
            else
            {
                ResetActionState();
                arms.SetWeapon(null);
            }

            SelectionChanged?.Invoke();
        }

        public bool MoveToHands(Inventory source, InventoryItem item)
        {
            if (source == null || source == hands || item == null || !hands.TryFindSpace(item.Definition, out Vector2Int position, out _))
                return false;

            if (!Inventory.TryTransfer(source, item, hands, position, false))
                return false;

            Select(hands.GetAt(position));
            return true;
        }

        public bool DropItem(Inventory source, InventoryItem item)
        {
            if (source == null || item == null)
                return false;

            if (item.Instance is not Weapon weapon)
                return itemActions.Drop(source, item);

            if (!source.Remove(item))
                return false;

            Throw(weapon);
            return true;
        }

        private void HandleSwitching()
        {
            for (int i = 0; i < input.HandSlotCount; i++)
            {
                if (!input.HandSlotPressed(i))
                    continue;

                Select(hands.GetAt(new Vector2Int(i % hands.Columns, i / hands.Columns)));
                return;
            }

            float scroll = input.CycleWeapon;
            if (Mathf.Abs(scroll) > scrollThreshold)
                Cycle(scroll > 0f ? -1 : 1);
        }

        private void Cycle(int direction)
        {
            List<InventoryItem> items = OrderedHands();
            if (items.Count == 0)
                return;

            int index = items.IndexOf(selected);
            int next = index < 0 ? 0 : (index + direction + items.Count) % items.Count;
            Select(items[next]);
        }

        private List<InventoryItem> OrderedHands()
        {
            orderedHands.Clear();
            orderedHands.AddRange(hands.Items);
            orderedHands.Sort((a, b) => a.Position.y != b.Position.y ? a.Position.y.CompareTo(b.Position.y) : a.Position.x.CompareTo(b.Position.x));
            return orderedHands;
        }

        private void HandleHandsChanged()
        {
            if (selected != null && !hands.Contains(selected))
            {
                Weapon weapon = selected.Instance as Weapon;
                selected = null;
                ResetActionState();
                arms.SetWeapon(null);

                if (weapon != null)
                    weapon.gameObject.SetActive(false);
            }

            if (selected == null && !suppressAutoSelect)
            {
                List<InventoryItem> items = OrderedHands();
                if (items.Count > 0)
                    Select(items[0]);
            }

            SelectionChanged?.Invoke();
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

            StoreWeapon(weapon, true);
        }

        private bool StoreWeapon(Weapon weapon, bool inspect)
        {
            ItemDefinition definition = weapon.ItemDefinition;
            if (definition == null)
                return false;

            if (hands.TryFindSpace(definition, out Vector2Int handPosition, out _))
            {
                Attach(weapon);
                suppressAutoSelect = true;
                hands.TryAddAt(definition, weapon, handPosition, false);
                suppressAutoSelect = false;
                Select(hands.GetAt(handPosition), inspect);
                return true;
            }

            if (inventory == null || !inventory.TryFindSpace(definition, out Vector2Int position, out bool rotated))
                return false;

            Attach(weapon);
            inventory.TryAddAt(definition, weapon, position, rotated);
            return true;
        }

        private void Attach(Weapon weapon)
        {
            weapon.Equip(gameObject, weaponSocket, equippedLayer);
            weapon.gameObject.SetActive(false);
        }

        private void Throw(Weapon weapon)
        {
            weapon.gameObject.SetActive(true);
            Transform view = viewCamera.transform;
            weapon.Drop(view.TransformPoint(dropOffset), view.rotation, view.forward * throwSpeed, worldLayer);
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

        private int AvailableRounds(Weapon weapon)
        {
            if (weapon.AmmoType == null)
                return int.MaxValue;

            int rounds = hands.Count(weapon.AmmoType);
            if (inventory != null)
                rounds += inventory.Count(weapon.AmmoType);
            return rounds;
        }

        private int TakeRounds(Weapon weapon, int rounds)
        {
            if (weapon.AmmoType == null)
                return rounds;

            int taken = inventory != null ? inventory.Remove(weapon.AmmoType, rounds) : 0;
            return taken + hands.Remove(weapon.AmmoType, rounds - taken);
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
