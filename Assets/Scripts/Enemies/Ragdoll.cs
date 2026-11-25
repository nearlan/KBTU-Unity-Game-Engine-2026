using UnityEngine;

namespace RUSH.Enemies
{
    public class Ragdoll : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] bodies;
        [SerializeField, Min(0f)] private float upwardLift = 0.35f;
        [SerializeField, Min(0f)] private float torqueImpulse = 8f;
        [SerializeField, Range(0f, 1f)] private float secondaryBodyRatio = 0.25f;

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

            Vector3 liftedImpulse = impulse + Vector3.up * (impulse.magnitude * upwardLift);
            Rigidbody closest = FindClosestBody(point);
            if (closest != null)
            {
                closest.AddForceAtPosition(liftedImpulse, point, ForceMode.Impulse);
                closest.AddTorque(Random.insideUnitSphere * torqueImpulse, ForceMode.Impulse);
            }

            if (secondaryBodyRatio <= 0f)
                return;

            Vector3 secondaryImpulse = liftedImpulse * secondaryBodyRatio;
            foreach (Rigidbody body in bodies)
            {
                if (body != null && body != closest)
                    body.AddForce(secondaryImpulse, ForceMode.Impulse);
            }
        }

        private void SetSimulated(bool simulated)
        {
            foreach (Rigidbody body in bodies)
            {
                if (body == null)
                    continue;

                body.isKinematic = !simulated;
                body.interpolation = simulated ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
                body.collisionDetectionMode = simulated ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.Discrete;
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
