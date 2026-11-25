using RUSH.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RUSH.UI
{
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Entity target;
        [SerializeField] private Slider slider;
        [SerializeField, Min(0.1f)] private float drainSpeed = 1.5f;

        private float Normalized =>
            target != null && target.MaxHealth > 0f
                ? Mathf.Clamp01(target.CurrentHealth / target.MaxHealth)
                : 0f;

        private void Awake()
        {
            if (slider == null)
                return;

            slider.interactable = false;
            slider.wholeNumbers = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
        }

        private void Start()
        {
            if (slider != null)
                slider.value = Normalized;
        }

        private void Update()
        {
            if (slider == null)
                return;

            slider.value = Mathf.MoveTowards(slider.value, Normalized, drainSpeed * Time.deltaTime);
        }
    }
}
