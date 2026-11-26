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
            Place,
            Merge,
            World
        }

        private struct DropTarget
        {
            public DropAction Action;
            public InventoryGridView Grid;
            public Inventory Inventory;
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
        [SerializeField] private InventoryGridView[] handGrids;
        [SerializeField] private InventoryItemView dragGhost;
        [SerializeField, Range(0f, 1f)] private float draggedAlpha = 0.35f;
        [SerializeField, Range(0f, 1f)] private float ghostAlpha = 0.85f;
        [SerializeField, Min(0f)] private float fadeSpeed = 12f;

        private CanvasGroup canvasGroup;
        private Vector2 pointer;
        private bool dirty = true;
        private int ammoSignature;
        private InventoryItem hoveredItem;

        private bool dragging;
        private Inventory dragInventory;
        private InventoryItem dragItem;
        private bool dragRotated;
        private Vector2Int dragGrab;

        public bool IsOpen { get; private set; }

        private Inventory Hands => weaponHandler.Hands;
        private RectTransform Rect => (RectTransform)transform;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            backpackGrid.Bind(this);
            foreach (InventoryGridView grid in handGrids)
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
            Hands.Changed += MarkDirty;
            weaponHandler.SelectionChanged += MarkDirty;
            owner.Died += HandleOwnerDied;
            MarkDirty();
        }

        private void OnDisable()
        {
            inventory.Changed -= MarkDirty;
            Hands.Changed -= MarkDirty;
            weaponHandler.SelectionChanged -= MarkDirty;
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

            int signature = ComputeAmmoSignature();
            if (signature != ammoSignature)
            {
                ammoSignature = signature;
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

            hoveredItem = null;
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

            Inventory source = InventoryOf(grid);
            Vector2Int cell = ToInventoryCell(grid, grid.ScreenToCell(eventData.position));
            InventoryItem item = source.GetAt(cell);
            if (item == null)
                return;

            dragging = true;
            dragInventory = source;
            dragItem = item;
            dragRotated = item.Rotated;
            dragGrab = cell - item.Position;
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
            Inventory source = dragInventory;
            InventoryItem item = dragItem;
            CancelDrag();

            switch (target.Action)
            {
                case DropAction.Place when target.Valid:
                    Inventory.TryTransfer(source, item, target.Inventory, target.Position, target.Rotated);
                    break;
                case DropAction.Merge:
                    Inventory.TryMerge(source, item, target.Inventory, target.MergeTarget);
                    break;
                case DropAction.World:
                    weaponHandler.DropItem(source, item);
                    break;
            }
        }

        private DropTarget EvaluateDrop()
        {
            InventoryGridView grid = GridAt(pointer);
            if (grid == null)
            {
                bool insidePanel = RectTransformUtility.RectangleContainsScreenPoint(panel, pointer, null);
                return new DropTarget { Action = insidePanel ? DropAction.None : DropAction.World };
            }

            Inventory target = InventoryOf(grid);
            Vector2Int cell = ToInventoryCell(grid, grid.ScreenToCell(pointer));
            InventoryItem occupant = target.GetAt(cell);

            if (dragItem.CanMergeInto(occupant))
            {
                return new DropTarget
                {
                    Action = DropAction.Merge, Grid = grid, Inventory = target, Position = occupant.Position,
                    Size = occupant.Size, Valid = true, MergeTarget = occupant
                };
            }

            bool rotated = target.AllowRotation && dragRotated;
            Vector2Int size = dragItem.Definition.GetSize(rotated);
            Vector2Int grab = rotated == dragRotated ? dragGrab : new Vector2Int(dragGrab.y, dragGrab.x);
            grab = Vector2Int.Min(Vector2Int.Max(grab, Vector2Int.zero), size - Vector2Int.one);
            Vector2Int position = cell - grab;

            InventoryItem ignore = target == dragInventory ? dragItem : null;
            return new DropTarget
            {
                Action = DropAction.Place,
                Grid = grid,
                Inventory = target,
                Position = position,
                Size = size,
                Rotated = rotated,
                Valid = target.CanPlace(size, position, ignore)
            };
        }

        private void UseAt(InventoryGridView grid, Vector2Int cell)
        {
            Inventory source = InventoryOf(grid);
            InventoryItem item = source.GetAt(ToInventoryCell(grid, cell));
            if (item == null)
                return;

            if (item.Definition.IsUsable)
                itemActions.Use(source, item);
            else if (source == Hands)
                weaponHandler.Select(item);
            else
                weaponHandler.MoveToHands(source, item);
        }

        private void RotateDrag()
        {
            Vector2Int size = dragItem.Definition.Size;
            if (size.x == size.y)
                return;

            dragRotated = !dragRotated;
            Vector2Int rotatedSize = dragItem.Definition.GetSize(dragRotated);
            dragGrab = new Vector2Int(
                Mathf.Clamp(dragGrab.y, 0, rotatedSize.x - 1),
                Mathf.Clamp(dragGrab.x, 0, rotatedSize.y - 1));
            ShowGhost();
        }

        private void UpdateDragPreview()
        {
            ClearPreviews();

            DropTarget target = EvaluateDrop();
            if (target.Grid == null || target.Action == DropAction.None)
                return;

            Vector2Int offset = new(0, target.Grid.RowOffset);
            target.Grid.ShowPreview(target.Position - offset, target.Size, target.Valid);
        }

        private void ClearPreviews()
        {
            backpackGrid.ClearPreview();
            foreach (InventoryGridView grid in handGrids)
                grid.ClearPreview();
        }

        private void ShowGhost()
        {
            if (dragGhost == null)
                return;

            RectTransform rect = dragGhost.Rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = backpackGrid.CellSizeFor(dragItem.Definition.GetSize(dragRotated));
            dragGhost.gameObject.SetActive(true);
            dragGhost.transform.SetAsLastSibling();
            dragGhost.Show(IconOf(dragItem), string.Empty, dragRotated, false, false, ghostAlpha);
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

        private void CancelDrag()
        {
            dragging = false;
            dragInventory = null;
            dragItem = null;
            dragRotated = false;

            if (dragGhost != null)
                dragGhost.gameObject.SetActive(false);

            ClearPreviews();
            dirty = true;
        }

        private void UpdateHover()
        {
            InventoryItem current = null;
            if (!dragging && TryGetHovered(out InventoryGridView grid, out Vector2Int cell))
                current = InventoryOf(grid).GetAt(ToInventoryCell(grid, cell));

            if (current == hoveredItem)
                return;

            hoveredItem = current;
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

            foreach (InventoryGridView grid in handGrids)
            {
                if (grid.Contains(screenPoint))
                    return grid;
            }

            return null;
        }

        private Inventory InventoryOf(InventoryGridView grid)
        {
            return grid.Kind == InventoryGridKind.Hands ? Hands : inventory;
        }

        private static Vector2Int ToInventoryCell(InventoryGridView grid, Vector2Int cell)
        {
            return cell + new Vector2Int(0, grid.RowOffset);
        }

        private void Refresh()
        {
            dirty = false;

            backpackGrid.BeginItems();
            foreach (InventoryItem item in inventory.Items)
                AddItemView(backpackGrid, item, item.Position);
            backpackGrid.EndItems();

            foreach (InventoryGridView grid in handGrids)
            {
                grid.BeginItems();
                foreach (InventoryItem item in Hands.Items)
                {
                    if (item.Position.y == grid.RowOffset)
                        AddItemView(grid, item, item.Position - new Vector2Int(0, grid.RowOffset));
                }
                grid.EndItems();
            }
        }

        private void AddItemView(InventoryGridView grid, InventoryItem item, Vector2Int position)
        {
            float alpha = item == dragItem ? draggedAlpha : 1f;
            bool selected = item == weaponHandler.SelectedItem;
            grid.AddItem(IconOf(item), position, item.Size, item.Rotated, LabelFor(item), item == hoveredItem, selected, alpha);
        }

        private static Sprite IconOf(InventoryItem item)
        {
            if (item.Definition.Icon != null)
                return item.Definition.Icon;
            return item.Instance is Weapon weapon ? weapon.HudIcon : null;
        }

        private static string LabelFor(InventoryItem item)
        {
            if (item.Instance is Firearm firearm)
                return $"{firearm.Ammunition}/{firearm.MagazineSize}";

            return item.Definition.MaxStack > 1 ? item.Count.ToString() : string.Empty;
        }

        private int ComputeAmmoSignature()
        {
            int signature = 17;
            foreach (InventoryItem item in Hands.Items)
            {
                if (item.Instance is Firearm firearm)
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
