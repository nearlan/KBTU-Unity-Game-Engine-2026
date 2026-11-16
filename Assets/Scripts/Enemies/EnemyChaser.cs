using UnityEngine;
using UnityEngine.AI;

namespace KBTU.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyChaser : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float repathInterval = 0.2f;

        private NavMeshAgent agent;
        private Transform target;
        private float nextRepathTime;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (target == null || !agent.isOnNavMesh || Time.time < nextRepathTime)
                return;

            nextRepathTime = Time.time + repathInterval;
            agent.SetDestination(target.position);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void Stop()
        {
            if (agent.isOnNavMesh)
                agent.isStopped = true;

            agent.enabled = false;
            enabled = false;
        }
    }
}
