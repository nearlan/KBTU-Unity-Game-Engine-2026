using RUSH.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RUSH.EditorTools
{
    [CustomEditor(typeof(BlockoutForest))]
    public class BlockoutForestEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            BlockoutForest forest = (BlockoutForest)target;
            using (new EditorGUI.DisabledScope(forest.TreePrefab == null))
            {
                if (GUILayout.Button("Generate"))
                    Generate(forest);
            }

            if (GUILayout.Button("Clear"))
                Clear(forest);

            EditorGUILayout.HelpBox($"Trees: {forest.transform.childCount}", MessageType.None);
        }

        public static int Generate(BlockoutForest forest)
        {
            if (forest.TreePrefab == null)
                throw new System.InvalidOperationException("Tree prefab is not assigned");

            Clear(forest);

            System.Random random = new(forest.Seed);
            Vector3 center = forest.transform.position;
            float half = forest.MapSize * 0.5f;
            float inner = forest.PlayAreaSize * 0.5f + forest.PlayAreaMargin;
            float spacing = forest.Spacing;
            float offset = spacing * 0.5f * forest.Jitter;
            Terrain terrain = Terrain.activeTerrain;
            int count = 0;

            for (float x = -half + spacing * 0.5f; x < half; x += spacing)
            {
                for (float z = -half + spacing * 0.5f; z < half; z += spacing)
                {
                    float px = x + Range(random, -offset, offset);
                    float pz = z + Range(random, -offset, offset);

                    if (Mathf.Abs(px) < inner && Mathf.Abs(pz) < inner)
                        continue;

                    if (Mathf.Abs(px) > half - 1f || Mathf.Abs(pz) > half - 1f)
                        continue;

                    Vector3 position = center + new Vector3(px, 0f, pz);
                    if (terrain != null)
                        position.y = terrain.transform.position.y + terrain.SampleHeight(position);

                    GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(forest.TreePrefab, forest.transform);
                    tree.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Range(random, 0f, 360f), 0f));
                    tree.transform.localScale = Vector3.one * Range(random, forest.ScaleRange.x, forest.ScaleRange.y);
                    tree.name = $"Tree_{count:0000}";
                    Undo.RegisterCreatedObjectUndo(tree, "Generate Blockout Forest");
                    count++;
                }
            }

            EditorSceneManager.MarkSceneDirty(forest.gameObject.scene);
            return count;
        }

        public static void Clear(BlockoutForest forest)
        {
            for (int i = forest.transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(forest.transform.GetChild(i).gameObject);

            EditorSceneManager.MarkSceneDirty(forest.gameObject.scene);
        }

        private static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
