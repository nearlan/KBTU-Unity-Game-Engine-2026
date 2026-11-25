using UnityEngine;

namespace RUSH.Players
{
    public class PlayerStamina : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxStamina = 100f;

        [Header("Costs")]
        [SerializeField, Min(0f)] private float sprintCostPerSecond = 20f;
        [SerializeField, Min(0f)] private float jumpCost = 15f;

        [Header("Regeneration")]
        [SerializeField, Min(0f)] private float regenPerSecond = 25f;
        [SerializeField, Min(0f)] private float regenDelay = 1f;
        [SerializeField, Range(0f, 1f)] private float exhaustionRecoveryThreshold = 0.3f;

        private float regenCooldown;

        public float Max => maxStamina;
        public float Current { get; private set; }
        public float Normalized => Current / maxStamina;
        public bool IsExhausted { get; private set; }
        public bool CanSprint => !IsExhausted && Current > 0f;

        private void Awake()
        {
            Current = maxStamina;
        }

        private void Update()
        {
            if (regenCooldown > 0f)
            {
                regenCooldown -= Time.deltaTime;
                return;
            }

            Current = Mathf.Min(maxStamina, Current + regenPerSecond * Time.deltaTime);

            if (IsExhausted && Normalized >= exhaustionRecoveryThreshold)
                IsExhausted = false;
        }

        public void ConsumeSprint(float deltaTime)
        {
            Spend(sprintCostPerSecond * deltaTime);
        }

        public bool TryConsumeJump()
        {
            if (IsExhausted || Current < jumpCost)
                return false;

            Spend(jumpCost);
            return true;
        }

        private void Spend(float amount)
        {
            if (amount <= 0f)
                return;

            Current = Mathf.Max(0f, Current - amount);
            regenCooldown = regenDelay;

            if (Current <= 0f)
                IsExhausted = true;
        }
    }
}
