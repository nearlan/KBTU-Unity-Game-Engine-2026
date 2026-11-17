using UnityEngine;
using UnityEngine.AI;

namespace KBTU.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyChaser : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float repathInterval = 0.2f;
        [SerializeField, Min(0f)] private float knockbackDamping = 10f;
        [SerializeField, Min(0f)] private float maxKnockbackSpeed = 12f;

        private NavMeshAgent agent;
        private Transform target;
        private Vector3 knockbackVelocity;
        private float nextRepathTime;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (knockbackVelocity.sqrMagnitude > 0.001f)
            {
                if (agent.isOnNavMesh)
                    agent.Move(knockbackVelocity * Time.deltaTime);

                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 1f - Mathf.Exp(-knockbackDamping * Time.deltaTime));
            }

            if (target == null || !agent.isOnNavMesh || Time.time < nextRepathTime)
                return;

            nextRepathTime = Time.time + repathInterval;
            agent.SetDestination(target.position);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void ApplyKnockback(Vector3 impulse)
        {
            Vector3 planarImpulse = new(impulse.x, 0f, impulse.z);
            knockbackVelocity = Vector3.ClampMagnitude(knockbackVelocity + planarImpulse, maxKnockbackSpeed);
        }

        public void Stop()
        {
            knockbackVelocity = Vector3.zero;

            if (agent.isOnNavMesh)
                agent.isStopped = true;

            agent.enabled = false;
            enabled = false;
        }
    }
}
