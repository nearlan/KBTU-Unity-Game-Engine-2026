using RUSH.Items;
using UnityEngine;

namespace RUSH.Weapons
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class Weapon : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField] private WeaponItemDefinition itemDefinition;

        [Header("Holding")]
        [SerializeField] private RuntimeAnimatorController armsController;
        [SerializeField] private Vector3 gripPosition;
        [SerializeField] private Quaternion gripRotation = Quaternion.identity;

        [Header("World")]
        [SerializeField] private Collider worldCollider;
        [SerializeField, Min(0f)] private float dropSpin = 4f;

        [Header("Audio")]
        [SerializeField] private WeaponSounds sounds = new();

        [Header("Recoil")]
        [SerializeField] private WeaponRecoil recoil = new();

        [Header("HUD")]
        [SerializeField] private Sprite hudIcon;

        private Rigidbody body;

        protected Animator Animator { get; private set; }

        public GameObject Owner { get; private set; }
        public bool IsEquipped => Owner != null;
        public RuntimeAnimatorController ArmsController => armsController;
        public WeaponSounds Sounds => sounds;
        public WeaponRecoil Recoil => recoil;
        public Sprite HudIcon => hudIcon;
        public WeaponItemDefinition ItemDefinition => itemDefinition;

        public virtual AmmoDefinition AmmoType => null;
        public virtual int MissingRounds => 0;

        public abstract bool IsAutomatic { get; }
        public abstract bool IsReadyToFire { get; }
        public abstract bool HasAmmunition { get; }
        public abstract bool CanReload { get; }

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody>();
            Animator = GetComponent<Animator>();

            if (!IsEquipped)
                EnterWorldState();
        }

        public void Equip(GameObject owner, Transform socket, int layer)
        {
            Owner = owner;

            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            worldCollider.enabled = false;

            transform.SetParent(socket, false);
            transform.SetLocalPositionAndRotation(gripPosition, gripRotation);
            transform.localScale = Vector3.one;
            SetLayer(layer);

            if (Animator != null)
            {
                Animator.enabled = true;
                Animator.Rebind();
            }

            OnEquipped();
        }

        public void Drop(Vector3 position, Quaternion rotation, Vector3 velocity, int layer)
        {
            Collider ownerCollider = Owner != null ? Owner.GetComponent<Collider>() : null;
            Owner = null;

            transform.SetParent(null, true);
            transform.SetPositionAndRotation(position, rotation);
            SetLayer(layer);
            EnterWorldState();

            if (ownerCollider != null)
                Physics.IgnoreCollision(worldCollider, ownerCollider);

            body.linearVelocity = velocity;
            body.angularVelocity = Random.insideUnitSphere * dropSpin;

            OnDropped();
        }

        public abstract void Fire(Ray aimRay);

        public abstract void FireEmpty();

        public abstract void BeginReload();

        public abstract void FillAmmunition(int rounds);

        public virtual void EjectCasing()
        {
        }

        protected virtual void OnEquipped()
        {
        }

        protected virtual void OnDropped()
        {
        }

        protected void PlayAnimation(int stateHash)
        {
            if (Animator != null && Animator.isActiveAndEnabled)
                Animator.Play(stateHash, 0, 0f);
        }

        private void EnterWorldState()
        {
            if (Animator != null)
            {
                Animator.Rebind();
                Animator.Update(0f);
                Animator.enabled = false;
            }

            worldCollider.enabled = true;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void SetLayer(int layer)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }
    }
}
