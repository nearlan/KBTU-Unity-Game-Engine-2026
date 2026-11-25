using System.Linq;
using RUSH.Players;
using RUSH.UI;
using RUSH.Weapons;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RUSH.EditorTools
{
    public static class SyntyHudBuilder
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string SyntyRoot = "Assets/Synty/InterfaceApocalypseHUD/";

        private const string ScreenHudPath = SyntyRoot + "Prefabs/_PreMadeHUDs/Screen_HUD_Apocalypse_Minimal_01.prefab";
        private const string DamageVignettePath = SyntyRoot + "Prefabs/FullscreenFX/FX_Apocalypse_Damage_Vignette_01.prefab";
        private const string FadeToBlackPath = SyntyRoot + "Prefabs/FullscreenOverlays/Overlay_Apocalypse_FadeToBlack_01.prefab";
        private const string HeaderLabelPath = SyntyRoot + "Prefabs/_CommonComponents/Label_Apocalypse_Header.prefab";
        private const string StaminaIconPath = SyntyRoot + "Sprites/Icons_Map/ICON_Apocalpyse_Map_Lightning_01.png";
        private const string DamageClipPath = "Assets/Sounds/Impacts/S_WEP_Impact_Bullet_01.wav";

        private const string StatDialController = "AC_HUD_Apocalypse_Stat_Dial";
        private const string DamageChipName = "Damage Chip";

        private static readonly (string Prefab, string Icon)[] WeaponIcons =
        {
            ("Assets/Prefabs/Weapons/AR_01.prefab", SyntyRoot + "Sprites/Icons_Weapons/ICON_SM_Wep_AssaultRifle_01_Underlay.png"),
            ("Assets/Prefabs/Weapons/Handgun_03.prefab", SyntyRoot + "Sprites/Icons_Weapons/ICON_SM_Wep_Pistol_01_Underlay.png"),
        };

        [MenuItem("Tools/RUSH/Build Synty HUD")]
        public static void Build()
        {
            if (Shader.Find("TextMeshPro/Distance Field") == null)
            {
                EditorUtility.DisplayDialog(
                    "Synty HUD",
                    "TextMeshPro Essential Resources are not imported.\n\n" +
                    "Run Window > TextMeshPro > Import TMP Essential Resources, then build the HUD again.",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Synty HUD",
                    $"Rebuild the HUD inside {PlayerPrefabPath}?\n\nAll current children of the HUD object will be replaced.",
                    "Build",
                    "Cancel"))
                return;

            AssignWeaponIcons();
            BuildPlayerHud();
            AssetDatabase.SaveAssets();

            Debug.Log("[SyntyHudBuilder] HUD rebuilt in " + PlayerPrefabPath);
        }

        private static void AssignWeaponIcons()
        {
            foreach ((string prefabPath, string iconPath) in WeaponIcons)
            {
                GameObject prefab = Load<GameObject>(prefabPath);
                Weapon weapon = prefab.GetComponentInChildren<Weapon>(true);
                if (weapon == null)
                {
                    Debug.LogWarning($"[SyntyHudBuilder] No Weapon component in {prefabPath}");
                    continue;
                }

                SetReference(weapon, "hudIcon", Load<Sprite>(iconPath));
                EditorUtility.SetDirty(weapon);
            }
        }

        private static void BuildPlayerHud()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            try
            {
                Transform hud = root.transform.Find("HUD");
                if (hud == null)
                    throw new System.InvalidOperationException("HUD object not found in " + PlayerPrefabPath);

                ClearHud(hud);

                Player player = root.GetComponent<Player>();
                PlayerWeaponHandler weaponHandler = root.GetComponent<PlayerWeaponHandler>();
                PlayerMovement movement = root.GetComponent<PlayerMovement>();
                PlayerStamina stamina = root.GetComponent<PlayerStamina>();
                AudioSource audioSource = root.GetComponent<AudioSource>();

                Transform screen = BuildScreenHud(hud);
                BuildHealthAndStamina(screen, player, stamina);
                BuildAmmo(screen, weaponHandler);
                BuildCrosshair(screen, weaponHandler, movement);
                BuildDamageFeedback(screen, player, audioSource);
                BuildGameOver(hud, screen, player);
                InventoryWindowBuilder.Build(root);
                RemoveSampleScripts(hud);

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ClearHud(Transform hud)
        {
            for (int i = hud.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(hud.GetChild(i).gameObject);

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(hud.gameObject);

            foreach (GameOverView view in hud.GetComponents<GameOverView>())
                Object.DestroyImmediate(view);
        }

        private static Transform BuildScreenHud(Transform hud)
        {
            GameObject screen = Instantiate(ScreenHudPath, hud);
            PrefabUtility.UnpackPrefabInstance(screen, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            screen.name = "Screen_HUD";
            Stretch(screen.GetComponent<RectTransform>());

            if (screen.TryGetComponent(out Image background))
                background.enabled = false;

            if (screen.TryGetComponent(out CanvasGroup group))
            {
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            DestroyChildren(screen.transform.Find("WorldSpace"));
            DestroyChildren(screen.transform.Find("ScreenSpace/Top Left"));
            DestroyChildren(screen.transform.Find("ScreenSpace/Top Right"));
            DestroyChildren(screen.transform.Find("ScreenSpace/Bottom"));

            return screen.transform;
        }

        private static void BuildHealthAndStamina(Transform screen, Player player, PlayerStamina stamina)
        {
            Transform dial = FindRequired(screen, "ScreenSpace/Bottom Left").GetChild(0);

            Slider healthSlider = dial.GetComponentsInChildren<Slider>(true)
                .FirstOrDefault(slider => IsActiveUnder(slider.transform, dial));
            if (healthSlider == null)
                throw new System.InvalidOperationException("Visible health slider not found in " + dial.name);

            HealthBarView health = dial.gameObject.AddComponent<HealthBarView>();
            SetReference(health, "target", player);
            SetReference(health, "slider", healthSlider);
            SetReference(health, "damageChip", BuildDamageChip(healthSlider));

            Animator[] statDials = dial.GetComponentsInChildren<Animator>(true)
                .Where(a => a.runtimeAnimatorController != null && a.runtimeAnimatorController.name == StatDialController)
                .ToArray();

            if (statDials.Length == 0)
                throw new System.InvalidOperationException("Stat dial not found in " + dial.name);

            Animator staminaDial = statDials[0];
            staminaDial.gameObject.name = "Stat_Dial_Stamina";
            staminaDial.gameObject.SetActive(true);
            for (int i = 1; i < statDials.Length; i++)
                statDials[i].gameObject.SetActive(false);

            Sprite staminaIcon = Load<Sprite>(StaminaIconPath);
            foreach (Image icon in staminaDial.GetComponentsInChildren<Image>(true).Where(image => image.name == "Icon"))
                icon.sprite = staminaIcon;

            StaminaDialView staminaView = dial.gameObject.AddComponent<StaminaDialView>();
            SetReference(staminaView, "stamina", stamina);
            SetReference(staminaView, "dialAnimator", staminaDial);
        }

        private static Image BuildDamageChip(Slider slider)
        {
            RectTransform fill = slider.fillRect;
            if (fill == null)
                throw new System.InvalidOperationException("Health slider has no fill rect");

            Transform existing = fill.parent.Find(DamageChipName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject chip = Object.Instantiate(fill.gameObject, fill.parent);
            chip.name = DamageChipName;
            chip.transform.SetSiblingIndex(fill.GetSiblingIndex());

            Image image = chip.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static void BuildAmmo(Transform screen, PlayerWeaponHandler weaponHandler)
        {
            Transform widget = FindRequired(screen, "ScreenSpace/Bottom Right").GetChild(0);
            TMP_Text[] labels = widget.GetComponentsInChildren<TMP_Text>(true);

            AmmoView view = widget.gameObject.AddComponent<AmmoView>();
            SetReference(view, "weaponHandler", weaponHandler);
            SetReference(view, "weaponIcon", FindByName<Image>(widget, "Icon_CurrentWeapon"));
            SetReference(view, "ammunitionLabel", labels.FirstOrDefault(l => l.name == "Label_AmmoCount"));
            SetReference(view, "reserveLabel", labels.FirstOrDefault(l => l.name == "Label_AmmoTotal"));
        }

        private static void BuildCrosshair(Transform screen, PlayerWeaponHandler weaponHandler, PlayerMovement movement)
        {
            Transform container = FindRequired(screen, "Crosshair");
            Crosshair crosshair = container.gameObject.AddComponent<Crosshair>();
            SetReference(crosshair, "weaponHandler", weaponHandler);
            SetReference(crosshair, "movement", movement);
            SetReference(crosshair, "reticleAnimator", container.GetComponentInChildren<Animator>(true));
        }

        private static void BuildDamageFeedback(Transform screen, Player player, AudioSource audioSource)
        {
            GameObject vignette = Instantiate(DamageVignettePath, FindRequired(screen, "FX"));
            vignette.name = "FX_Damage_Vignette";

            DamageFeedback feedback = vignette.AddComponent<DamageFeedback>();
            SetReference(feedback, "target", player);
            SetReference(feedback, "vignetteAnimator", vignette.GetComponent<Animator>());
            SetReference(feedback, "audioSource", audioSource);

            SerializedObject so = new(feedback);
            SerializedProperty clips = so.FindProperty("damageClips");
            clips.arraySize = 1;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = Load<AudioClip>(DamageClipPath);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildGameOver(Transform hud, Transform screen, Player player)
        {
            GameObject fade = Instantiate(FadeToBlackPath, hud);
            fade.name = "Overlay_FadeToBlack";
            Stretch(fade.GetComponent<RectTransform>());

            foreach (CanvasGroup group in fade.GetComponentsInChildren<CanvasGroup>(true))
                group.alpha = 0f;

            GameObject label = Instantiate(HeaderLabelPath, hud);
            label.name = "GameOverLabel";
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(1400f, 220f);

            TMP_Text text = label.GetComponent<TMP_Text>();
            text.text = "GAME OVER";
            text.fontSize = 140f;
            text.enableAutoSizing = false;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            CanvasGroup labelGroup = label.AddComponent<CanvasGroup>();
            labelGroup.alpha = 0f;

            GameOverView view = hud.gameObject.AddComponent<GameOverView>();
            SetReference(view, "target", player);
            SetReference(view, "hudAnimator", screen.GetComponent<Animator>());
            SetReference(view, "fadeAnimator", fade.GetComponent<Animator>());
            SetReference(view, "labelGroup", labelGroup);
        }

        private static void RemoveSampleScripts(Transform hud)
        {
            foreach (MonoBehaviour behaviour in hud.GetComponentsInChildren<MonoBehaviour>(true))
            {
                string ns = behaviour != null ? behaviour.GetType().Namespace : null;
                if (ns != null && ns.StartsWith("Synty.") && ns.EndsWith(".Samples"))
                    Object.DestroyImmediate(behaviour);
            }
        }

        private static GameObject Instantiate(string prefabPath, Transform parent)
        {
            return (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(prefabPath), parent);
        }

        private static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new System.IO.FileNotFoundException($"{typeof(T).Name} not found at {path}");
            return asset;
        }

        private static Transform FindRequired(Transform parent, string path)
        {
            Transform result = parent.Find(path);
            if (result == null)
                throw new System.InvalidOperationException($"'{path}' not found under {parent.name}");
            return result;
        }

        private static T FindByName<T>(Transform parent, string name) where T : Component
        {
            return parent.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name);
        }

        private static bool IsActiveUnder(Transform transform, Transform root)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                if (!current.gameObject.activeSelf)
                    return false;
                if (current == root)
                    return true;
            }

            return true;
        }

        private static void DestroyChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetReference(Object target, string property, Object value)
        {
            SerializedObject so = new(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null)
                throw new System.InvalidOperationException($"{target.GetType().Name}.{property} not found");

            if (value == null)
                Debug.LogWarning($"[SyntyHudBuilder] {target.GetType().Name}.{property} is left empty", target);

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
