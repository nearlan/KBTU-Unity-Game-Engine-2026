using UnityEngine;

namespace KBTU.Enemies
{
    public class Ragdoll : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] bodies;

        public bool IsActive { get; private set; }

        private void Awake()
        {
            SetSimulated(false);
        }

        public void Activate(Vector3 impulse, Vector3 point)
        {
            if (IsActive)
                return;

            IsActive = true;
            SetSimulated(true);

            Rigidbody closest = FindClosestBody(point);
            if (closest != null)
                closest.AddForceAtPosition(impulse, point, ForceMode.Impulse);
        }

        private void SetSimulated(bool simulated)
        {
            foreach (Rigidbody body in bodies)
            {
                body.isKinematic = !simulated;
                body.interpolation = simulated ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            }
        }

        private Rigidbody FindClosestBody(Vector3 point)
        {
            Rigidbody closest = null;
            float closestDistance = float.MaxValue;

            foreach (Rigidbody body in bodies)
            {
                float distance = (body.worldCenterOfMass - point).sqrMagnitude;
                if (distance >= closestDistance)
                    continue;

                closest = body;
                closestDistance = distance;
            }

            return closest;
        }
    }
}
