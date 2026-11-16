using KBTU.Core;
using KBTU.Effects;
using UnityEngine;

namespace KBTU.Enemies
{
    [RequireComponent(typeof(EnemyChaser), typeof(EnemyAttack), typeof(Ragdoll))]
    public class Enemy : Entity
    {
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private DamageFlash damageFlash;
        [SerializeField, Min(0f)] private float deathImpulse = 60f;

        private EnemyChaser chaser;
        private EnemyAttack attack;
        private Ragdoll ragdoll;

        protected override void Awake()
        {
            base.Awake();
            chaser = GetComponent<EnemyChaser>();
            attack = GetComponent<EnemyAttack>();
            ragdoll = GetComponent<Ragdoll>();
        }

        private void Start()
        {
            GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);
            if (targetObject == null || !targetObject.TryGetComponent(out Entity target))
                return;

            chaser.SetTarget(target.transform);
            attack.SetTarget(target);
        }

        protected override void OnDamaged(DamageInfo damage)
        {
            if (damageFlash != null)
                damageFlash.Flash();
        }

        protected override void Die(DamageInfo killingBlow)
        {
            chaser.Stop();
            attack.enabled = false;
            ragdoll.Activate(killingBlow.Direction * deathImpulse, killingBlow.Point);
        }
    }
}
