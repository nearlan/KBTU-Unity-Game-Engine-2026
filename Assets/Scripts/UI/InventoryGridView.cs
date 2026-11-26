using System.Collections.Generic;
using RUSH.Weapons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RUSH.UI
{
    public enum InventoryGridKind
    {
        Backpack,
        WeaponSlot
    }

    public class InventoryGridView : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private InventoryGridKind kind;
        [SerializeField] private WeaponSlot weaponSlot;
        [SerializeField, Min(1)] private int columns = 1;
        [SerializeField, Min(1)] private int rows = 1;
        [SerializeField, Min(1f)] private float cellSize = 104f;
        [SerializeField, Min(0f)] private float spacing = 10f;
        [SerializeField] private Image[] cells;
        [SerializeField] private RectTransform itemsRoot;
        [SerializeField] private InventoryItemView itemTemplate;
        [SerializeField] private GameObject activeMarker;

        [Header("Colors")]
        [SerializeField] private Color cellColor = new(0.16f, 0.17f, 0.15f, 0.95f);
        [SerializeField] private Color validColor = new(0.25f, 0.55f, 0.25f, 0.95f);
        [SerializeField] private Color invalidColor = new(0.6f, 0.18f, 0.15f, 0.95f);

        private readonly List<InventoryItemView> views = new();
        private InventoryWindow window;
        private int shownCount;

        public InventoryGridKind Kind => kind;
        public WeaponSlot WeaponSlot => weaponSlot;
        public int Columns => columns;
        public int Rows => rows;
        public float CellSize => cellSize;
        public float CellStep => Step;

        private RectTransform Rect => (RectTransform)transform;
        private float Step => cellSize + spacing;

        public void Bind(InventoryWindow owner)
        {
            window = owner;
            if (itemTemplate != null)
                itemTemplate.gameObject.SetActive(false);
            ClearPreview();
        }

        public bool Contains(Vector2 screenPoint)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(Rect, screenPoint, null);
        }

        public Vector2Int ScreenToCell(Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, screenPoint, null, out Vector2 local);
            Rect rect = Rect.rect;
            float x = local.x - rect.xMin;
            float y = rect.yMax - local.y;
            return new Vector2Int(Mathf.FloorToInt(x / Step), Mathf.FloorToInt(y / Step));
        }

        public Vector2 CellSizeFor(Vector2Int size)
        {
            return new Vector2(size.x * Step - spacing, size.y * Step - spacing);
        }

        public void BeginItems()
        {
            shownCount = 0;
        }

        public void AddItem(Sprite icon, Vector2Int position, Vector2Int size, bool rotated, string label, bool highlighted, float alpha)
        {
            InventoryItemView view;
            if (shownCount < views.Count)
            {
                view = views[shownCount];
            }
            else
            {
                view = Instantiate(itemTemplate, itemsRoot);
                views.Add(view);
            }

            shownCount++;
            view.gameObject.SetActive(true);

            RectTransform rect = view.Rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(position.x * Step, -position.y * Step);
            rect.sizeDelta = CellSizeFor(size);
            view.Show(icon, label, rotated, highlighted, alpha);
        }

        public void EndItems()
        {
            for (int i = shownCount; i < views.Count; i++)
                views[i].gameObject.SetActive(false);
        }

        public void SetActiveMarker(bool active)
        {
            if (activeMarker != null)
                activeMarker.SetActive(active);
        }

        public void ShowPreview(Vector2Int position, Vector2Int size, bool valid)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                int x = i % columns;
                int y = i / columns;
                bool inside = x >= position.x && x < position.x + size.x && y >= position.y && y < position.y + size.y;
                cells[i].color = inside ? (valid ? validColor : invalidColor) : cellColor;
            }
        }

        public void ClearPreview()
        {
            foreach (Image cell in cells)
                cell.color = cellColor;
        }

        public void OnPointerClick(PointerEventData eventData) => window.HandleClick(this, eventData);

        public void OnBeginDrag(PointerEventData eventData) => window.HandleBeginDrag(this, eventData);

        public void OnDrag(PointerEventData eventData) => window.HandleDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) => window.HandleEndDrag(eventData);
    }
}
