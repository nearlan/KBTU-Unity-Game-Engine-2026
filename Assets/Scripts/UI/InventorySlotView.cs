using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RUSH.UI
{
    public enum InventorySlotKind
    {
        Grid,
        Weapon
    }

    public class InventorySlotView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private GameObject highlight;
        [SerializeField] private GameObject activeMarker;
        [SerializeField, Range(0f, 1f)] private float draggedIconAlpha = 0.3f;

        private InventoryWindow window;

        public InventorySlotKind Kind { get; private set; }
        public int Index { get; private set; }
        public bool HasContent { get; private set; }
        public Sprite Icon => icon != null ? icon.sprite : null;

        public void Bind(InventoryWindow owner, InventorySlotKind kind, int index)
        {
            window = owner;
            Kind = kind;
            Index = index;
            SetHighlighted(false);
            Clear();
        }

        public void Show(Sprite sprite, string count, bool active = false)
        {
            HasContent = sprite != null;
            icon.sprite = sprite;
            icon.enabled = HasContent;
            SetIconAlpha(1f);

            if (countLabel != null)
                countLabel.SetText(count ?? string.Empty);

            if (activeMarker != null)
                activeMarker.SetActive(active);
        }

        public void Clear()
        {
            Show(null, null);
        }

        public void SetDragged(bool dragged)
        {
            SetIconAlpha(dragged ? draggedIconAlpha : 1f);
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHighlighted(true);

        public void OnPointerExit(PointerEventData eventData) => SetHighlighted(false);

        public void OnPointerClick(PointerEventData eventData) => window.HandleClick(this, eventData);

        public void OnBeginDrag(PointerEventData eventData) => window.HandleBeginDrag(this, eventData);

        public void OnDrag(PointerEventData eventData) => window.HandleDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) => window.HandleEndDrag(eventData);

        public void OnDrop(PointerEventData eventData) => window.HandleDrop(this);

        private void SetHighlighted(bool highlighted)
        {
            if (highlight != null)
                highlight.SetActive(highlighted);
        }

        private void SetIconAlpha(float alpha)
        {
            Color color = icon.color;
            color.a = alpha;
            icon.color = color;
        }
    }
}
