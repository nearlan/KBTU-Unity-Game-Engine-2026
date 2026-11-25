using System;
using UnityEngine;

namespace RUSH.Core
{
    public abstract class Entity : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        public event Action<Entity, DamageInfo> Damaged;
        public event Action<Entity, float> Healed;
        public event Action<Entity> Died;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        protected virtual void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (IsDead || damage.Amount <= 0f)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage.Amount);
            OnDamaged(damage);
            Damaged?.Invoke(this, damage);

            if (CurrentHealth > 0f)
                return;

            IsDead = true;
            Die(damage);
            Died?.Invoke(this);
        }

        public bool Heal(float amount)
        {
            if (IsDead || amount <= 0f || CurrentHealth >= maxHealth)
                return false;

            float previous = CurrentHealth;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            float gained = CurrentHealth - previous;
            if (gained <= 0f)
                return false;

            Healed?.Invoke(this, gained);
            return true;
        }

        protected virtual void OnDamaged(DamageInfo damage)
        {
        }

        protected abstract void Die(DamageInfo killingBlow);
    }
}
