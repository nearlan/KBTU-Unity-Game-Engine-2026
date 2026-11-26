using System.Linq;
using RUSH.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RUSH.EditorTools
{
    [CustomEditor(typeof(ForestRing))]
    public class ForestRingEditor : Editor
    {
        private const StaticEditorFlags StaticFlags =
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            ForestRing forest = (ForestRing)target;
            if (GUILayout.Button("Generate"))
                Generate(forest);

            if (GUILayout.Button("Clear"))
                Clear(forest);

            EditorGUILayout.HelpBox($"Objects: {forest.transform.childCount}", MessageType.None);
        }

        public static int Generate(ForestRing forest)
        {
            Clear(forest);

            System.Random random = new(forest.Seed);
            Terrain terrain = Terrain.activeTerrain;
            int count = 0;

            foreach (ForestRing.Layer layer in forest.Layers)
            {
                GameObject[] prefabs = layer.prefabs.Where(p => p != null).ToArray();
                if (prefabs.Length == 0)
                    continue;

                Transform group = new GameObject(layer.name).transform;
                group.SetParent(forest.transform, false);
                Undo.RegisterCreatedObjectUndo(group.gameObject, "Generate Forest");
                count += Populate(forest, layer, prefabs, group, terrain, random);
            }

            EditorSceneManager.MarkSceneDirty(forest.gameObject.scene);
            return count;
        }

        public static void Clear(ForestRing forest)
        {
            for (int i = forest.transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(forest.transform.GetChild(i).gameObject);

            EditorSceneManager.MarkSceneDirty(forest.gameObject.scene);
        }

        private static int Populate(ForestRing forest, ForestRing.Layer layer, GameObject[] prefabs, Transform group, Terrain terrain, System.Random random)
        {
            Vector3 center = forest.transform.position;
            float half = forest.MapSize * 0.5f;
            float inner = forest.PlayAreaSize * 0.5f + forest.PlayAreaMargin + layer.inset;
            float outer = layer.bandWidth > 0f ? Mathf.Min(half, inner + layer.bandWidth) : half;
            float spacing = layer.spacing;
            float offset = spacing * 0.5f * layer.jitter;
            int count = 0;

            for (float x = -half + spacing * 0.5f; x < half; x += spacing)
            {
                for (float z = -half + spacing * 0.5f; z < half; z += spacing)
                {
                    float px = x + Range(random, -offset, offset);
                    float pz = z + Range(random, -offset, offset);
                    float ring = Mathf.Max(Mathf.Abs(px), Mathf.Abs(pz));

                    if (ring < inner || ring > outer)
                        continue;

                    Vector3 position = center + new Vector3(px, 0f, pz);
                    if (terrain != null)
                        position.y = terrain.transform.position.y + terrain.SampleHeight(position);
                    position.y -= layer.sink;

                    Quaternion rotation = Quaternion.Euler(
                        Range(random, -layer.maxTilt, layer.maxTilt),
                        Range(random, 0f, 360f),
                        Range(random, -layer.maxTilt, layer.maxTilt));

                    GameObject prefab = prefabs[random.Next(prefabs.Length)];
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                    instance.transform.SetPositionAndRotation(position, rotation);
                    instance.transform.localScale = Vector3.one * Range(random, layer.scaleRange.x, layer.scaleRange.y);
                    foreach (Transform part in instance.GetComponentsInChildren<Transform>())
                        GameObjectUtility.SetStaticEditorFlags(part.gameObject, StaticFlags);
                    count++;
                }
            }

            return count;
        }

        private static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
