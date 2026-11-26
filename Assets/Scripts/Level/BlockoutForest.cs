using UnityEngine;

namespace RUSH.Level
{
    public class BlockoutForest : MonoBehaviour
    {
        [SerializeField] private GameObject treePrefab;
        [SerializeField, Min(10f)] private float mapSize = 250f;
        [SerializeField, Min(1f)] private float playAreaSize = 170f;
        [SerializeField, Min(0f)] private float playAreaMargin = 2f;
        [SerializeField, Min(1f)] private float spacing = 5.5f;
        [SerializeField, Range(0f, 1f)] private float jitter = 0.8f;
        [SerializeField] private Vector2 scaleRange = new(0.8f, 1.35f);
        [SerializeField] private int seed = 1;

        public GameObject TreePrefab => treePrefab;
        public float MapSize => mapSize;
        public float PlayAreaSize => playAreaSize;
        public float PlayAreaMargin => playAreaMargin;
        public float Spacing => spacing;
        public float Jitter => jitter;
        public Vector2 ScaleRange => scaleRange;
        public int Seed => seed;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(playAreaSize, 1f, playAreaSize));
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(mapSize, 1f, mapSize));
        }
    }
}
