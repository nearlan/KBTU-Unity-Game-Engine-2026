using RUSH.Core;
using RUSH.Effects;
using UnityEngine;

namespace RUSH.Weapons
{
    public class Firearm : Weapon
    {
        private static readonly int FireState = Animator.StringToHash("Fire");
        private static readonly int ReloadState = Animator.StringToHash("Reload");
        private static readonly int ReloadEmptyState = Animator.StringToHash("Reload Empty");

        [Header("Firing")]
        [SerializeField] private bool automatic;
        [SerializeField, Min(1f)] private float roundsPerMinute = 600f;
        [SerializeField, Min(0f)] private float damage = 20f;
        [SerializeField, Min(1f)] private float range = 200f;
        [SerializeField, Min(0f)] private float impactForce = 15f;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Ammunition")]
        [SerializeField, Min(1)] private int magazineSize = 30;

        [Header("Effects")]
        [SerializeField] private MuzzleFlash muzzleFlash;
        [SerializeField] private Transform ejectionSocket;
        [SerializeField] private Casing casingPrefab;
        [SerializeField] private ImpactEffect impactPrefab;
        [SerializeField] private BulletHole bulletHolePrefab;
        [SerializeField] private Vector2 casingSideSpeed = new(1.5f, 2.5f);
        [SerializeField] private Vector2 casingUpSpeed = new(1f, 2f);

        private float lastShotTime = float.NegativeInfinity;

        public int Ammunition { get; private set; }
        public int MagazineSize => magazineSize;

        public override bool IsAutomatic => automatic;
        public override bool IsReadyToFire => Time.time - lastShotTime >= 60f / roundsPerMinute;
        public override bool HasAmmunition => Ammunition > 0;
        public override bool CanReload => Ammunition < magazineSize;

        protected override void Awake()
        {
            base.Awake();
            Ammunition = magazineSize;
        }

        public override void Fire(Ray aimRay)
        {
            if (!HasAmmunition)
                return;

            lastShotTime = Time.time;
            Ammunition--;
            PlayAnimation(FireState);

            if (muzzleFlash != null)
                muzzleFlash.Play();

            Transform ignored = Owner != null ? Owner.transform : transform;
            if (PhysicsQuery.Raycast(aimRay, range, hitMask, ignored, out RaycastHit hit))
                HandleHit(aimRay, hit);
        }

        public override void FireEmpty()
        {
            lastShotTime = Time.time;
        }

        public override void BeginReload()
        {
            PlayAnimation(HasAmmunition ? ReloadState : ReloadEmptyState);
        }

        public override void FillAmmunition()
        {
            Ammunition = magazineSize;
        }

        public override void EjectCasing()
        {
            if (casingPrefab == null || ejectionSocket == null)
                return;

            Transform reference = Owner != null ? Owner.transform : transform;
            Vector3 velocity = reference.right * Random.Range(casingSideSpeed.x, casingSideSpeed.y)
                               + Vector3.up * Random.Range(casingUpSpeed.x, casingUpSpeed.y);

            Casing casing = Instantiate(casingPrefab, ejectionSocket.position, ejectionSocket.rotation);
            casing.Launch(velocity, Owner != null ? Owner.GetComponent<Collider>() : null);
        }

        private void HandleHit(Ray aimRay, RaycastHit hit)
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(new DamageInfo(damage, hit.point, aimRay.direction, Owner));

            if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                hit.rigidbody.AddForceAtPosition(aimRay.direction * impactForce, hit.point, ForceMode.Impulse);

            if (impactPrefab != null)
                Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));

            BulletHole.TrySpawn(bulletHolePrefab, hit);
        }
    }
}
