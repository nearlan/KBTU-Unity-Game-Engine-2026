using System.Collections.Generic;
using UnityEngine;

namespace RUSH.Effects
{
    public class BulletHole : MonoBehaviour
    {
        private static readonly List<BulletHole> ActiveHoles = new();

        [SerializeField] private Vector2 sizeRange = new(0.12f, 0.17f);
        [SerializeField, Min(0f)] private float surfaceOffset = 0.003f;
        [SerializeField, Min(1)] private int maxHoles = 64;

        private Collider surface;

        public float Radius => transform.localScale.x * 0.5f;

        public static bool TrySpawn(BulletHole prefab, RaycastHit hit)
        {
            if (prefab == null || hit.collider == null || hit.rigidbody != null)
                return false;

            float size = Random.Range(prefab.sizeRange.x, prefab.sizeRange.y);
            if (Overlaps(hit.collider, hit.point, size * 0.5f))
                return false;

            while (ActiveHoles.Count >= prefab.maxHoles)
            {
                BulletHole oldest = ActiveHoles[0];
                ActiveHoles.RemoveAt(0);
                Destroy(oldest.gameObject);
            }

            Quaternion rotation = Quaternion.LookRotation(-hit.normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            BulletHole hole = Instantiate(prefab, hit.point + hit.normal * prefab.surfaceOffset, rotation);
            hole.transform.localScale = new Vector3(size, size, 1f);
            hole.surface = hit.collider;
            return true;
        }

        private static bool Overlaps(Collider surface, Vector3 point, float radius)
        {
            foreach (BulletHole hole in ActiveHoles)
            {
                if (hole.surface != surface)
                    continue;

                float minimumDistance = hole.Radius + radius;
                if ((hole.transform.position - point).sqrMagnitude < minimumDistance * minimumDistance)
                    return true;
            }

            return false;
        }

        private void OnEnable()
        {
            ActiveHoles.Add(this);
        }

        private void OnDisable()
        {
            ActiveHoles.Remove(this);
        }
    }
}
