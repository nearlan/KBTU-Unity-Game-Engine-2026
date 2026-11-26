using UnityEngine;

namespace RUSH.Effects
{
    public class DamageFlash : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material damagedMaterial;
        [SerializeField, Min(0f)] private float duration = 0.15f;

        private float restoreTime;
        private bool isFlashing;

        private void Awake()
        {
            Apply(normalMaterial);
        }

        private void Update()
        {
            if (!isFlashing || Time.time < restoreTime)
                return;

            isFlashing = false;
            Apply(normalMaterial);
        }

        public void Flash()
        {
            Apply(damagedMaterial);
            restoreTime = Time.time + duration;
            isFlashing = true;
        }

        private void Apply(Material material)
        {
            if (material == null)
                return;

            foreach (Renderer target in renderers)
                target.sharedMaterial = material;
        }
    }
}
