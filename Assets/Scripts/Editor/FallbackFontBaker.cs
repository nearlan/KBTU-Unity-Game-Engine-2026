using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RUSH.EditorTools
{
    public static class FallbackFontBaker
    {
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
        private const string ExtraCharacters = "ЁёІіЇїЄєҒғҚқҢңӨөҰұҮүҺһӘә—–«»№…°•×€£©®™←→↑↓";

        [MenuItem("Tools/RUSH/Bake Fallback Font")]
        public static void Bake()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
                throw new System.IO.FileNotFoundException("Fallback font not found at " + FontPath);

            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.ClearFontAssetData(true);
            bool complete = font.TryAddCharacters(BuildCharacterSet(), out string missing, true);
            font.atlasPopulationMode = AtlasPopulationMode.Static;

            SerializedObject so = new(font);
            so.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();

            string result = $"[FallbackFontBaker] {font.characterTable.Count} characters baked into {font.name}";
            if (!complete)
                result += ", missing: " + missing;
            Debug.Log(result);
        }

        private static string BuildCharacterSet()
        {
            StringBuilder characters = new();
            for (char c = (char)32; c < 127; c++)
                characters.Append(c);
            for (char c = 'А'; c <= 'я'; c++)
                characters.Append(c);
            characters.Append(ExtraCharacters);
            return characters.ToString();
        }
    }
}
