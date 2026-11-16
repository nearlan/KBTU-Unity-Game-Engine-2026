using KBTU.Core;
using UnityEngine;

namespace KBTU.Enemies
{
    public class EnemyAttack : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0f)] private float range = 1.8f;
        [SerializeField, Min(0.1f)] private float cooldown = 1f;

        private IDamageable target;
        private Transform targetTransform;
        private float nextAttackTime;

        private void Update()
        {
            if (target == null || target.IsDead || Time.time < nextAttackTime)
                return;

            Vector3 offset = targetTransform.position - transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude > range * range)
                return;

            nextAttackTime = Time.time + cooldown;
            target.TakeDamage(new DamageInfo(damage, targetTransform.position, offset.normalized, gameObject));
        }

        public void SetTarget(Entity newTarget)
        {
            target = newTarget;
            targetTransform = newTarget != null ? newTarget.transform : null;
        }
    }
}
