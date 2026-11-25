using RUSH.Core;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RUSH.UI
{
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Entity target;
        [SerializeField] private Slider slider;
        [FormerlySerializedAs("drainSpeed")]
        [SerializeField, Min(0.1f)] private float healSpeed = 1.5f;

        [Header("Damage Chip")]
        [Tooltip("Segment that shows the health just lost: flashes, holds, then shrinks and fades.")]
        [SerializeField] private Image damageChip;
        [SerializeField] private Color chipFlashColor = new(1f, 0.55f, 0.45f, 1f);
        [SerializeField] private Color chipColor = new(0.85f, 0.08f, 0.08f, 1f);
        [SerializeField, Min(0f)] private float chipFlashDuration = 0.12f;
        [SerializeField, Min(0f)] private float chipHoldDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float chipFadeDuration = 0.45f;

        private RectTransform chipRect;
        private float displayed;
        private float chipStart;
        private float chipEnd;
        private float chipTimer;
        private bool chipActive;

        private float Normalized =>
            target != null && target.MaxHealth > 0f
                ? Mathf.Clamp01(target.CurrentHealth / target.MaxHealth)
                : 0f;

        private void Awake()
        {
            if (slider != null)
            {
                slider.interactable = false;
                slider.wholeNumbers = false;
                slider.minValue = 0f;
                slider.maxValue = 1f;
            }

            if (damageChip != null)
            {
                chipRect = damageChip.rectTransform;
                damageChip.raycastTarget = false;
                damageChip.enabled = false;
            }
        }

        private void Start()
        {
            displayed = Normalized;
            if (slider != null)
                slider.value = displayed;
        }

        private void Update()
        {
            if (slider == null)
                return;

            float actual = Normalized;

            if (actual < displayed)
                StartChip(displayed, actual);
            else
                displayed = Mathf.MoveTowards(displayed, actual, healSpeed * Time.deltaTime);

            slider.value = displayed;
            UpdateChip();
        }

        private void StartChip(float from, float to)
        {
            // Stacking hits extend the current chip instead of restarting it from a shorter segment.
            chipEnd = chipActive ? Mathf.Max(chipEnd, from) : from;
            chipStart = chipEnd;
            chipTimer = 0f;
            chipActive = damageChip != null;
            displayed = to;
        }

        private void UpdateChip()
        {
            if (!chipActive)
                return;

            chipTimer += Time.deltaTime;

            float fadeStart = chipFlashDuration + chipHoldDuration;
            float fade = Mathf.Clamp01((chipTimer - fadeStart) / chipFadeDuration);
            float eased = fade * fade * (3f - 2f * fade);

            // Shrink towards the current value while fading, so the segment visibly "drains away".
            chipEnd = Mathf.Lerp(chipStart, displayed, eased);

            Color color = chipFlashDuration > 0f
                ? Color.Lerp(chipFlashColor, chipColor, Mathf.Clamp01(chipTimer / chipFlashDuration))
                : chipColor;
            color.a *= 1f - eased;

            if (fade >= 1f || chipEnd <= displayed)
            {
                chipActive = false;
                damageChip.enabled = false;
                return;
            }

            damageChip.enabled = true;
            damageChip.color = color;
            SetChipRange(displayed, chipEnd);
        }

        private void SetChipRange(float min, float max)
        {
            bool reversed = slider.direction == Slider.Direction.RightToLeft;
            float left = reversed ? 1f - max : min;
            float right = reversed ? 1f - min : max;

            chipRect.anchorMin = new Vector2(left, 0f);
            chipRect.anchorMax = new Vector2(right, 1f);
            chipRect.offsetMin = Vector2.zero;
            chipRect.offsetMax = Vector2.zero;
        }
    }
}
