namespace RUSH.Core
{
    public interface IDamageable
    {
        bool IsDead { get; }

        void TakeDamage(DamageInfo damage);
    }
}
