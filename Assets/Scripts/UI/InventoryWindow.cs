using RUSH.Core;
using RUSH.Items;
using RUSH.Players;
using RUSH.Weapons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RUSH.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryWindow : MonoBehaviour
    {
        private enum DropAction
        {
            None,
            Move,
            Merge,
            ToBackpack,
            ToSlot,
            World
        }

        private struct DropTarget
        {
            public DropAction Action;
            public InventoryGridView Grid;
            public Vector2Int Position;
            public Vector2Int Size;
            public bool Rotated;
            public bool Valid;
            public InventoryItem MergeTarget;
        }

        [SerializeField] private Entity owner;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Inventory inventory;
        [SerializeField] private PlayerItemActions itemActions;
        [SerializeField] private PlayerWeaponHandler weaponHandler;
        [SerializeField] private PlayerLook look;

        [Header("View")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private InventoryGridView backpackGrid;
        [SerializeField] private InventoryGridView[] weaponGrids;
        [SerializeField] private InventoryItemView dragGhost;
        [SerializeField, Range(0f, 1f)] private float draggedAlpha = 0.35f;
        [SerializeField, Range(0f, 1f)] private float ghostAlpha = 0.85f;
        [SerializeField, Min(0f)] private float fadeSpeed = 12f;

        private CanvasGroup canvasGroup;
        private Vector2 pointer;
        private bool dirty = true;
        private int weaponSignature;
        private object hoveredObject;

        private bool dragging;
        private InventoryGridView dragSource;
        private InventoryItem dragItem;
        private Weapon dragWeapon;
        private ItemDefinition dragDefinition;
        private bool dragRotated;
        private Vector2Int dragGrab;

        public bool IsOpen { get; private set; }

        private RectTransform Rect => (RectTransform)transform;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            backpackGrid.Bind(this);
            foreach (InventoryGridView grid in weaponGrids)
                grid.Bind(this);

            if (dragGhost != null)
            {
                dragGhost.GetComponent<CanvasGroup>().blocksRaycasts = false;
                dragGhost.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            inventory.Changed += MarkDirty;
            weaponHandler.WeaponsChanged += MarkDirty;
            owner.Died += HandleOwnerDied;
            MarkDirty();
        }

        private void OnDisable()
        {
            inventory.Changed -= MarkDirty;
            weaponHandler.WeaponsChanged -= MarkDirty;
            owner.Died -= HandleOwnerDied;
            SetOpen(false);
        }

        private void Update()
        {
            if (input.InventoryPressed && !owner.IsDead)
                SetOpen(!IsOpen);

            float target = IsOpen ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * Time.unscaledDeltaTime);

            if (!IsOpen)
                return;

            if (Pointer.current != null)
                pointer = Pointer.current.position.ReadValue();

            UpdateHover();

            if (dragging)
            {
                if (input.RotateItemPressed)
                    RotateDrag();

                UpdateDragPreview();
                MoveGhost();
            }
            else if (input.InteractPressed && TryGetHovered(out InventoryGridView grid, out Vector2Int cell))
            {
                UseAt(grid, cell);
            }

            int signature = ComputeWeaponSignature();
            if (signature != weaponSignature)
            {
                weaponSignature = signature;
                dirty = true;
            }

            if (dirty)
                Refresh();
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
                CancelDrag();

            hoveredObject = null;
            Refresh();
        }

        public void HandleClick(InventoryGridView grid, PointerEventData eventData)
        {
            if (IsOpen && !dragging && eventData.button == PointerEventData.InputButton.Right)
                UseAt(grid, grid.ScreenToCell(eventData.position));
        }

        public void HandleBeginDrag(InventoryGridView grid, PointerEventData eventData)
        {
            if (!IsOpen || dragging || eventData.button != PointerEventData.InputButton.Left)
                return;

            Vector2Int cell = grid.ScreenToCell(eventData.position);

            if (grid.Kind == InventoryGridKind.Backpack)
            {
                InventoryItem item = inventory.GetAt(cell);
                if (item == null)
                    return;

                dragItem = item;
                dragWeapon = item.Instance as Weapon;
                dragDefinition = item.Definition;
                dragRotated = item.Rotated;
                dragGrab = cell - item.Position;
            }
            else
            {
                Weapon weapon = SlotWeaponAt(grid, cell);
                if (weapon == null)
                    return;

                dragItem = null;
                dragWeapon = weapon;
                dragDefinition = weapon.ItemDefinition;
                dragRotated = false;
                dragGrab = new Vector2Int(Mathf.Clamp(cell.x, 0, WeaponSize(weapon).x - 1), 0);
            }

            dragging = true;
            dragSource = grid;
            pointer = eventData.position;
            ShowGhost();
            dirty = true;
        }

        public void HandleDrag(PointerEventData eventData)
        {
            if (dragging)
                pointer = eventData.position;
        }

        public void HandleEndDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;

            pointer = eventData.position;
            DropTarget target = EvaluateDrop();
            InventoryGridView source = dragSource;
            InventoryItem item = dragItem;
            CancelDrag();

            if (target.Action != DropAction.World && !target.Valid)
                return;

            switch (target.Action)
            {
                case DropAction.Move:
                    inventory.TryMove(item, target.Position, target.Rotated);
                    break;
                case DropAction.Merge:
                    inventory.TryMerge(item, target.MergeTarget);
                    break;
                case DropAction.ToBackpack:
                    weaponHandler.MoveToBackpack(source.WeaponSlot, target.Position, target.Rotated);
                    break;
                case DropAction.ToSlot:
                    weaponHandler.EquipFromBackpack(item);
                    break;
                case DropAction.World:
                    DropToWorld(source, item);
                    break;
            }
        }

        private void DropToWorld(InventoryGridView source, InventoryItem item)
        {
            if (source.Kind == InventoryGridKind.WeaponSlot)
            {
                weaponHandler.Drop(source.WeaponSlot);
                return;
            }

            if (item.Instance is Weapon)
                weaponHandler.DropFromBackpack(item);
            else
                itemActions.Drop(item);
        }

        private DropTarget EvaluateDrop()
        {
            InventoryGridView grid = GridAt(pointer);
            if (grid == null)
            {
                bool insidePanel = RectTransformUtility.RectangleContainsScreenPoint(panel, pointer, null);
                return new DropTarget { Action = insidePanel ? DropAction.None : DropAction.World };
            }

            if (grid.Kind == InventoryGridKind.Backpack)
            {
                if (dragDefinition == null)
                    return new DropTarget { Action = DropAction.None };

                Vector2Int cell = grid.ScreenToCell(pointer);
                InventoryItem occupant = inventory.GetAt(cell);
                if (dragItem != null && dragItem.CanMergeInto(occupant))
                {
                    return new DropTarget
                    {
                        Action = DropAction.Merge, Grid = grid, Position = occupant.Position,
                        Size = occupant.Size, Valid = true, MergeTarget = occupant
                    };
                }

                Vector2Int size = dragDefinition.GetSize(dragRotated);
                Vector2Int position = cell - dragGrab;
                return new DropTarget
                {
                    Action = dragItem != null ? DropAction.Move : DropAction.ToBackpack,
                    Grid = grid,
                    Position = position,
                    Size = size,
                    Rotated = dragRotated,
                    Valid = inventory.CanPlace(size, position, dragItem)
                };
            }

            if (dragItem == null)
                return new DropTarget { Action = DropAction.None };

            bool fits = dragWeapon != null && dragWeapon.Slot == grid.WeaponSlot;
            Vector2Int slotSize = dragDefinition != null ? dragDefinition.Size : Vector2Int.one;
            return new DropTarget
            {
                Action = DropAction.ToSlot,
                Grid = grid,
                Position = Vector2Int.zero,
                Size = Vector2Int.Min(slotSize, new Vector2Int(grid.Columns, grid.Rows)),
                Valid = fits
            };
        }

        private void UseAt(InventoryGridView grid, Vector2Int cell)
        {
            if (grid.Kind == InventoryGridKind.WeaponSlot)
            {
                if (SlotWeaponAt(grid, cell) != null)
                    weaponHandler.Select(grid.WeaponSlot);
                return;
            }

            InventoryItem item = inventory.GetAt(cell);
            if (item == null)
                return;

            if (item.Instance is Weapon)
                weaponHandler.EquipFromBackpack(item);
            else
                itemActions.Use(item);
        }

        private void RotateDrag()
        {
            if (dragDefinition == null || dragDefinition.Size.x == dragDefinition.Size.y)
                return;

            dragRotated = !dragRotated;
            Vector2Int rotatedSize = dragDefinition.GetSize(dragRotated);
            dragGrab = new Vector2Int(
                Mathf.Clamp(dragGrab.y, 0, rotatedSize.x - 1),
                Mathf.Clamp(dragGrab.x, 0, rotatedSize.y - 1));
            ShowGhost();
        }

        private void UpdateDragPreview()
        {
            backpackGrid.ClearPreview();
            foreach (InventoryGridView grid in weaponGrids)
                grid.ClearPreview();

            DropTarget target = EvaluateDrop();
            if (target.Grid != null && target.Action != DropAction.None)
                target.Grid.ShowPreview(target.Position, target.Size, target.Valid);
        }

        private void ShowGhost()
        {
            if (dragGhost == null)
                return;

            Vector2Int size = dragDefinition != null ? dragDefinition.GetSize(dragRotated) : Vector2Int.one;
            RectTransform rect = dragGhost.Rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = backpackGrid.CellSizeFor(size);
            dragGhost.gameObject.SetActive(true);
            dragGhost.transform.SetAsLastSibling();
            dragGhost.Show(DragIcon(), string.Empty, dragRotated, false, ghostAlpha);
            MoveGhost();
        }

        private void MoveGhost()
        {
            if (dragGhost == null || !dragGhost.gameObject.activeSelf)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, pointer, null, out Vector2 local);
            float step = backpackGrid.CellStep;
            float half = backpackGrid.CellSize * 0.5f;
            dragGhost.Rect.anchoredPosition = local + new Vector2(-(dragGrab.x * step + half), dragGrab.y * step + half);
        }

        private Sprite DragIcon()
        {
            if (dragDefinition != null && dragDefinition.Icon != null)
                return dragDefinition.Icon;
            return dragWeapon != null ? dragWeapon.HudIcon : null;
        }

        private void CancelDrag()
        {
            dragging = false;
            dragSource = null;
            dragItem = null;
            dragWeapon = null;
            dragDefinition = null;
            dragRotated = false;

            if (dragGhost != null)
                dragGhost.gameObject.SetActive(false);

            backpackGrid.ClearPreview();
            foreach (InventoryGridView grid in weaponGrids)
                grid.ClearPreview();

            dirty = true;
        }

        private void UpdateHover()
        {
            object current = null;
            if (!dragging && TryGetHovered(out InventoryGridView grid, out Vector2Int cell))
            {
                current = grid.Kind == InventoryGridKind.Backpack
                    ? inventory.GetAt(cell)
                    : SlotWeaponAt(grid, cell);
            }

            if (ReferenceEquals(current, hoveredObject))
                return;

            hoveredObject = current;
            dirty = true;
        }

        private bool TryGetHovered(out InventoryGridView grid, out Vector2Int cell)
        {
            grid = GridAt(pointer);
            cell = grid != null ? grid.ScreenToCell(pointer) : default;
            return grid != null;
        }

        private InventoryGridView GridAt(Vector2 screenPoint)
        {
            if (backpackGrid.Contains(screenPoint))
                return backpackGrid;

            foreach (InventoryGridView grid in weaponGrids)
            {
                if (grid.Contains(screenPoint))
                    return grid;
            }

            return null;
        }

        private Weapon SlotWeaponAt(InventoryGridView grid, Vector2Int cell)
        {
            Weapon weapon = weaponHandler.GetWeapon(grid.WeaponSlot);
            if (weapon == null)
                return null;

            Vector2Int size = WeaponSize(weapon);
            return cell.x >= 0 && cell.y >= 0 && cell.x < size.x && cell.y < size.y ? weapon : null;
        }

        private static Vector2Int WeaponSize(Weapon weapon)
        {
            return weapon.ItemDefinition != null ? weapon.ItemDefinition.Size : Vector2Int.one;
        }

        private void Refresh()
        {
            dirty = false;

            backpackGrid.BeginItems();
            foreach (InventoryItem item in inventory.Items)
            {
                float alpha = item == dragItem ? draggedAlpha : 1f;
                bool highlighted = hoveredObject == item;
                backpackGrid.AddItem(item.Definition.Icon, item.Position, item.Size, item.Rotated, LabelFor(item), highlighted, alpha);
            }
            backpackGrid.EndItems();

            foreach (InventoryGridView grid in weaponGrids)
            {
                Weapon weapon = weaponHandler.GetWeapon(grid.WeaponSlot);
                grid.BeginItems();
                if (weapon != null)
                {
                    float alpha = dragging && dragItem == null && dragWeapon == weapon ? draggedAlpha : 1f;
                    Sprite icon = weapon.ItemDefinition != null && weapon.ItemDefinition.Icon != null ? weapon.ItemDefinition.Icon : weapon.HudIcon;
                    grid.AddItem(icon, Vector2Int.zero, WeaponSize(weapon), false, AmmoLabel(weapon), ReferenceEquals(hoveredObject, weapon), alpha);
                }
                grid.EndItems();
                grid.SetActiveMarker(weapon != null && weaponHandler.ActiveSlot == grid.WeaponSlot);
            }
        }

        private static string LabelFor(InventoryItem item)
        {
            if (item.Instance is Weapon weapon)
                return AmmoLabel(weapon);

            return item.Definition.MaxStack > 1 ? item.Count.ToString() : string.Empty;
        }

        private static string AmmoLabel(Weapon weapon)
        {
            return weapon is Firearm firearm ? $"{firearm.Ammunition}/{firearm.MagazineSize}" : string.Empty;
        }

        private int ComputeWeaponSignature()
        {
            int signature = (int)weaponHandler.ActiveSlot;
            for (int i = 0; i < weaponGrids.Length; i++)
            {
                if (weaponHandler.GetWeapon(weaponGrids[i].WeaponSlot) is Firearm firearm)
                    signature = signature * 31 + firearm.Ammunition + 1;
            }

            return signature;
        }

        private void MarkDirty()
        {
            dirty = true;
        }

        private void HandleOwnerDied(Entity entity)
        {
            SetOpen(false);
        }
    }
}
