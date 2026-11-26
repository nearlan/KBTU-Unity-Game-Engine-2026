using System;
using UnityEngine;

namespace RUSH.Level
{
    public class ForestRing : MonoBehaviour
    {
        [Serializable]
        public class Layer
        {
            public string name = "Layer";
            public GameObject[] prefabs = Array.Empty<GameObject>();
            [Min(0.5f)] public float spacing = 4.5f;
            [Range(0f, 1f)] public float jitter = 0.9f;
            public Vector2 scaleRange = new(0.85f, 1.25f);
            [Min(0f)] public float inset;
            [Min(0f)] public float bandWidth;
            [Range(0f, 15f)] public float maxTilt = 3f;
            [Min(0f)] public float sink = 0.2f;
        }

        [SerializeField, Min(10f)] private float mapSize = 250f;
        [SerializeField, Min(1f)] private float playAreaSize = 170f;
        [SerializeField] private float playAreaMargin = 3f;
        [SerializeField] private Layer[] layers = Array.Empty<Layer>();
        [SerializeField] private int seed = 1;

        public float MapSize => mapSize;
        public float PlayAreaSize => playAreaSize;
        public float PlayAreaMargin => playAreaMargin;
        public Layer[] Layers => layers;
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
