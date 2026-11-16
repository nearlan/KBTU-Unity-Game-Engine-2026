using UnityEngine;

namespace KBTU.Weapons
{
    public class MuzzleFlash : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particlesPrefab;
        [SerializeField, Min(1)] private int particleCount = 5;
        [SerializeField] private Light lightPrefab;
        [SerializeField, Min(0f)] private float lightDuration = 0.05f;

        private ParticleSystem particles;
        private Light flashLight;
        private float lightOffTime;

        private void Awake()
        {
            if (particlesPrefab != null)
                particles = SpawnAtSocket(particlesPrefab);

            if (lightPrefab != null)
            {
                flashLight = SpawnAtSocket(lightPrefab);
                flashLight.enabled = false;
            }
        }

        private T SpawnAtSocket<T>(T prefab) where T : Component
        {
            T instance = Instantiate(prefab, transform, false);
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            return instance;
        }

        private void Update()
        {
            if (flashLight != null && flashLight.enabled && Time.time >= lightOffTime)
                flashLight.enabled = false;
        }

        public void Play()
        {
            if (particles != null)
                particles.Emit(particleCount);

            if (flashLight == null)
                return;

            flashLight.enabled = true;
            lightOffTime = Time.time + lightDuration;
        }
    }
}
