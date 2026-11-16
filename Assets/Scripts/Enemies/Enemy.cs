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
        [SerializeField] private EnemyAudio enemyAudio;
        [SerializeField, Min(0f)] private float hitKnockback = 4.5f;
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

            if (enemyAudio == null)
                enemyAudio = GetComponent<EnemyAudio>();
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

            if (enemyAudio != null)
                enemyAudio.PlayDamage();

            if (hitKnockback > 0f)
                chaser.ApplyKnockback(damage.Direction * hitKnockback);
        }

        protected override void Die(DamageInfo killingBlow)
        {
            chaser.Stop();
            attack.enabled = false;

            if (enemyAudio != null)
                enemyAudio.PlayDeath();

            ragdoll.Activate(killingBlow.Direction * deathImpulse, killingBlow.Point);
        }
    }
}
