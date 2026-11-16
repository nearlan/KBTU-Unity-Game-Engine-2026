using UnityEngine;

namespace KBTU.Core
{
    public static class PhysicsQuery
    {
        private static readonly RaycastHit[] Buffer = new RaycastHit[32];

        public static bool Raycast(Ray ray, float distance, LayerMask mask, Transform ignored, out RaycastHit hit)
        {
            int count = Physics.RaycastNonAlloc(ray, Buffer, distance, mask, QueryTriggerInteraction.Ignore);
            return FindClosest(count, ignored, out hit);
        }

        public static bool SphereCast(Ray ray, float radius, float distance, LayerMask mask, Transform ignored, out RaycastHit hit)
        {
            int count = Physics.SphereCastNonAlloc(ray, radius, Buffer, distance, mask, QueryTriggerInteraction.Ignore);
            return FindClosest(count, ignored, out hit);
        }

        private static bool FindClosest(int count, Transform ignored, out RaycastHit closest)
        {
            closest = default;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = Buffer[i];
                if (ignored != null && candidate.collider.transform.IsChildOf(ignored))
                    continue;

                if (found && candidate.distance >= closest.distance)
                    continue;

                closest = candidate;
                found = true;
            }

            return found;
        }
    }
}
