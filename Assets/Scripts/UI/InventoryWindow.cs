using RUSH.Core;
using RUSH.Items;
using RUSH.Players;
using RUSH.Weapons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RUSH.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryWindow : MonoBehaviour
    {
        [SerializeField] private Entity owner;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Inventory inventory;
        [SerializeField] private PlayerItemActions itemActions;
        [SerializeField] private PlayerWeaponHandler weaponHandler;
        [SerializeField] private PlayerLook look;

        [Header("View")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private InventorySlotView[] gridSlots;
        [SerializeField] private InventorySlotView[] weaponSlots;
        [SerializeField] private Image dragIcon;
        [SerializeField, Min(0f)] private float fadeSpeed = 12f;

        private CanvasGroup canvasGroup;
        private InventorySlotView dragSource;
        private InventorySlotView hovered;
        private bool dropHandled;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            for (int i = 0; i < gridSlots.Length; i++)
                gridSlots[i].Bind(this, InventorySlotKind.Grid, i);

            for (int i = 0; i < weaponSlots.Length; i++)
                weaponSlots[i].Bind(this, InventorySlotKind.Weapon, i);

            if (dragIcon != null)
            {
                dragIcon.raycastTarget = false;
                dragIcon.enabled = false;
            }
        }

        private void OnEnable()
        {
            inventory.Changed += Refresh;
            weaponHandler.WeaponsChanged += Refresh;
            owner.Died += HandleOwnerDied;
            Refresh();
        }

        private void OnDisable()
        {
            inventory.Changed -= Refresh;
            weaponHandler.WeaponsChanged -= Refresh;
            owner.Died -= HandleOwnerDied;
            SetOpen(false);
        }

        private void Update()
        {
            if (input.InventoryPressed && !owner.IsDead)
                SetOpen(!IsOpen);

            if (IsOpen && hovered != null && input.InteractPressed)
                UseSlot(hovered);

            if (IsOpen)
                RefreshWeapons();

            float target = IsOpen ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * Time.unscaledDeltaTime);
        }

        public void SetOpen(bool open)
        {
            if (open == IsOpen)
                return;

            IsOpen = open;
            canvasGroup.interactable = open;
            canvasGroup.blocksRaycasts = open;
            look.InputBlocked = open;
            weaponHandler.InputBlocked = open;

            if (!open)
            {
                CancelDrag();
                hovered = null;
            }

            Refresh();
        }

        public void HandleClick(InventorySlotView slot, PointerEventData eventData)
        {
            if (IsOpen && eventData.button == PointerEventData.InputButton.Right)
                UseSlot(slot);
        }

        public void HandleHover(InventorySlotView slot, bool entered)
        {
            if (entered)
                hovered = slot;
            else if (hovered == slot)
                hovered = null;
        }

        private void UseSlot(InventorySlotView slot)
        {
            if (slot.Kind == InventorySlotKind.Grid)
                itemActions.Use(slot.Index);
            else
                weaponHandler.Select((WeaponSlot)slot.Index);
        }

        public void HandleBeginDrag(InventorySlotView slot, PointerEventData eventData)
        {
            if (!IsOpen || !slot.HasContent || eventData.button != PointerEventData.InputButton.Left)
                return;

            dragSource = slot;
            dropHandled = false;
            slot.SetDragged(true);

            if (dragIcon != null)
            {
                dragIcon.sprite = slot.Icon;
                dragIcon.enabled = true;
                dragIcon.transform.SetAsLastSibling();
            }

            HandleDrag(eventData);
        }

        public void HandleDrag(PointerEventData eventData)
        {
            if (dragSource != null && dragIcon != null)
                dragIcon.transform.position = eventData.position;
        }

        public void HandleDrop(InventorySlotView target)
        {
            if (dragSource == null)
                return;

            dropHandled = true;

            if (dragSource.Kind == InventorySlotKind.Grid && target.Kind == InventorySlotKind.Grid)
                inventory.Move(dragSource.Index, target.Index);
        }

        public void HandleEndDrag(PointerEventData eventData)
        {
            if (dragSource == null)
                return;

            InventorySlotView source = dragSource;
            bool outside = !RectTransformUtility.RectangleContainsScreenPoint(panel, eventData.position, eventData.pressEventCamera);
            CancelDrag();

            if (dropHandled || !outside)
                return;

            if (source.Kind == InventorySlotKind.Grid)
                itemActions.Drop(source.Index);
            else
                weaponHandler.Drop((WeaponSlot)source.Index);
        }

        private void CancelDrag()
        {
            if (dragSource != null)
                dragSource.SetDragged(false);

            dragSource = null;

            if (dragIcon != null)
                dragIcon.enabled = false;
        }

        private void Refresh()
        {
            RefreshGrid();
            RefreshWeapons();
        }

        private void RefreshGrid()
        {
            int count = Mathf.Min(gridSlots.Length, inventory.Capacity);
            for (int i = 0; i < count; i++)
            {
                ItemStack stack = inventory[i];
                if (stack.IsEmpty)
                {
                    gridSlots[i].Clear();
                    continue;
                }

                string label = stack.Definition.MaxStack > 1 ? stack.Count.ToString() : string.Empty;
                gridSlots[i].Show(stack.Definition.Icon, label);
            }
        }

        private void RefreshWeapons()
        {
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                Weapon weapon = weaponHandler.GetWeapon((WeaponSlot)i);
                if (weapon == null)
                {
                    weaponSlots[i].Clear();
                    continue;
                }

                string ammunition = weapon is Firearm firearm ? $"{firearm.Ammunition}/{firearm.MagazineSize}" : string.Empty;
                weaponSlots[i].Show(weapon.HudIcon, ammunition, i == (int)weaponHandler.ActiveSlot);
            }
        }

        private void HandleOwnerDied(Entity entity)
        {
            SetOpen(false);
        }
    }
}
