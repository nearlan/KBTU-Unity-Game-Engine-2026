using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RUSH.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject highlight;
        [SerializeField, Min(0f)] private float iconPadding = 10f;

        private CanvasGroup canvasGroup;

        public RectTransform Rect => (RectTransform)transform;

        private CanvasGroup Group => canvasGroup != null ? canvasGroup : canvasGroup = GetComponent<CanvasGroup>();

        public void Show(Sprite sprite, string text, bool rotated, bool highlighted, float alpha = 1f)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;

            Vector2 area = Rect.sizeDelta - Vector2.one * (2f * iconPadding);
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = rotated ? new Vector2(area.y, area.x) : area;
            iconRect.localEulerAngles = new Vector3(0f, 0f, rotated ? 90f : 0f);

            if (label != null)
                label.SetText(text ?? string.Empty);

            if (highlight != null)
                highlight.SetActive(highlighted);

            Group.alpha = alpha;
        }
    }
}
