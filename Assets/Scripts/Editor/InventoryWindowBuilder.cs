using RUSH.Core;
using RUSH.Items;
using RUSH.Players;
using RUSH.UI;
using RUSH.Weapons;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RUSH.EditorTools
{
    public static class InventoryWindowBuilder
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string WindowName = "InventoryWindow";
        private const string SyntyRoot = "Assets/Synty/InterfaceApocalypseHUD/";
        private const string HudSprites = SyntyRoot + "Sprites/HUD/SPR_HUD_Apocalypse_";
        private const string Fonts = SyntyRoot + "Fonts/SairaCondensed/Font_Apocalypse_";

        private const float CellSize = 104f;
        private const float CellSpacing = 10f;
        private const float WeaponSlotWidth = 380f;
        private const float WeaponSlotHeight = 150f;

        private static readonly Color PanelColor = new(0.07f, 0.08f, 0.07f, 0.94f);
        private static readonly Color DimColor = new(0f, 0f, 0f, 0.55f);
        private static readonly Color SlotColor = new(0.16f, 0.17f, 0.15f, 0.95f);
        private static readonly Color HighlightColor = new(1f, 1f, 1f, 0.12f);
        private static readonly Color AccentColor = new(0.96f, 0.78f, 0.25f, 1f);
        private static readonly Color TextColor = new(0.92f, 0.9f, 0.85f, 1f);
        private static readonly Color MutedTextColor = new(0.92f, 0.9f, 0.85f, 0.55f);

        private static readonly string[] WeaponSlotTitles = { "1   ОСНОВНОЕ", "2   ПИСТОЛЕТ" };

        [MenuItem("Tools/RUSH/Build Inventory Window")]
        public static void BuildInPlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Build(root);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log("[InventoryWindowBuilder] Inventory window rebuilt in " + PlayerPrefabPath);
        }

        public static InventoryWindow Build(GameObject playerRoot)
        {
            Transform hud = playerRoot.transform.Find("HUD");
            if (hud == null)
                throw new System.InvalidOperationException("HUD object not found in " + playerRoot.name);

            Transform existing = hud.Find(WindowName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            if (!hud.TryGetComponent(out GraphicRaycaster _))
                hud.gameObject.AddComponent<GraphicRaycaster>();

            Inventory inventory = Require<Inventory>(playerRoot);

            RectTransform window = CreateRect(WindowName, hud);
            Stretch(window);
            Transform screenHud = hud.Find("Screen_HUD");
            window.SetSiblingIndex(screenHud != null ? screenHud.GetSiblingIndex() + 1 : hud.childCount - 1);
            window.gameObject.AddComponent<CanvasGroup>();

            Image dim = CreateImage("Dim", window, null, DimColor);
            Stretch(dim.rectTransform);

            float gridWidth = inventory.Columns * CellSize + (inventory.Columns - 1) * CellSpacing;
            float gridHeight = inventory.Rows * CellSize + (inventory.Rows - 1) * CellSpacing;
            float panelWidth = 60f + WeaponSlotWidth + 50f + gridWidth + 60f;
            float contentHeight = Mathf.Max(gridHeight, 2f * WeaponSlotHeight + 20f);
            float panelHeight = 170f + contentHeight + 100f;

            Image panel = CreateImage("Panel", window, LoadSprite("Box_Large_02"), PanelColor, 2f);
            panel.rectTransform.sizeDelta = new Vector2(panelWidth, panelHeight);

            TMP_Text title = CreateText("Title", panel.transform, "ИНВЕНТАРЬ", "Header", 54f, TextColor, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(60f, -40f), new Vector2(600f, 64f));

            Image line = CreateImage("Divider", panel.transform, LoadSprite("Line_01"), AccentColor);
            Place(line.rectTransform, new Vector2(60f, -108f), new Vector2(panelWidth - 120f, 6f));

            TMP_Text weaponsLabel = CreateText("WeaponsLabel", panel.transform, "ОРУЖИЕ", "Bold", 26f, AccentColor, TextAlignmentOptions.Left);
            Place(weaponsLabel.rectTransform, new Vector2(60f, -125f), new Vector2(WeaponSlotWidth, 36f));

            InventorySlotView[] weaponSlots = new InventorySlotView[WeaponSlotTitles.Length];
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                weaponSlots[i] = CreateWeaponSlot(panel.transform, i);
                Place((RectTransform)weaponSlots[i].transform, new Vector2(60f, -170f - i * (WeaponSlotHeight + 20f)), new Vector2(WeaponSlotWidth, WeaponSlotHeight));
            }

            float gridLeft = 60f + WeaponSlotWidth + 50f;
            TMP_Text backpackLabel = CreateText("BackpackLabel", panel.transform, "РЮКЗАК", "Bold", 26f, AccentColor, TextAlignmentOptions.Left);
            Place(backpackLabel.rectTransform, new Vector2(gridLeft, -125f), new Vector2(gridWidth, 36f));

            RectTransform grid = CreateRect("Grid", panel.transform);
            Place(grid, new Vector2(gridLeft, -170f), new Vector2(gridWidth, gridHeight));
            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(CellSize, CellSize);
            layout.spacing = new Vector2(CellSpacing, CellSpacing);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = inventory.Columns;

            InventorySlotView[] gridSlots = new InventorySlotView[inventory.Capacity];
            for (int i = 0; i < gridSlots.Length; i++)
                gridSlots[i] = CreateGridSlot(grid, i);

            TMP_Text hint = CreateText("Hint", panel.transform,
                "ЛКМ — перетащить    ПКМ — использовать / выбрать    За окно — выбросить    H — аптечка    Tab — закрыть",
                "Body", 22f, MutedTextColor, TextAlignmentOptions.Left);
            Place(hint.rectTransform, new Vector2(60f, -(170f + contentHeight + 35f)), new Vector2(panelWidth - 120f, 34f));

            Image dragIcon = CreateImage("DragIcon", window, null, Color.white);
            dragIcon.rectTransform.sizeDelta = new Vector2(CellSize * 0.85f, CellSize * 0.85f);
            dragIcon.preserveAspect = true;
            dragIcon.raycastTarget = false;
            dragIcon.enabled = false;

            InventoryWindow view = window.gameObject.AddComponent<InventoryWindow>();
            SerializedObject so = new(view);
            so.FindProperty("owner").objectReferenceValue = Require<Entity>(playerRoot);
            so.FindProperty("input").objectReferenceValue = Require<PlayerInputReader>(playerRoot);
            so.FindProperty("inventory").objectReferenceValue = inventory;
            so.FindProperty("itemActions").objectReferenceValue = Require<PlayerItemActions>(playerRoot);
            so.FindProperty("weaponHandler").objectReferenceValue = Require<PlayerWeaponHandler>(playerRoot);
            so.FindProperty("look").objectReferenceValue = Require<PlayerLook>(playerRoot);
            so.FindProperty("panel").objectReferenceValue = panel.rectTransform;
            so.FindProperty("dragIcon").objectReferenceValue = dragIcon;
            SetArray(so.FindProperty("gridSlots"), gridSlots);
            SetArray(so.FindProperty("weaponSlots"), weaponSlots);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static InventorySlotView CreateGridSlot(Transform parent, int index)
        {
            Image background = CreateImage($"Slot_{index:00}", parent, LoadSprite("Box_Medium_02"), SlotColor, 2.5f);
            RectTransform slot = background.rectTransform;

            Image icon = CreateImage("Icon", slot, null, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Stretch(icon.rectTransform, 12f);

            TMP_Text count = CreateText("Count", slot, "", "Bold", 26f, TextColor, TextAlignmentOptions.BottomRight);
            Stretch(count.rectTransform, 8f);

            GameObject highlight = CreateFrame(slot, "Highlight", HighlightColor);

            return AddSlotView(slot, icon, count, highlight, null);
        }

        private static InventorySlotView CreateWeaponSlot(Transform parent, int index)
        {
            Image background = CreateImage($"WeaponSlot_{(WeaponSlot)index}", parent, LoadSprite("Box_Medium_02"), SlotColor, 2f);
            RectTransform slot = background.rectTransform;
            slot.anchorMin = slot.anchorMax = new Vector2(0f, 1f);
            slot.pivot = new Vector2(0f, 1f);

            TMP_Text title = CreateText("Title", slot, WeaponSlotTitles[index], "Bold", 22f, MutedTextColor, TextAlignmentOptions.TopLeft);
            Stretch(title.rectTransform, 12f);

            Image icon = CreateImage("Icon", slot, null, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Stretch(icon.rectTransform, 22f);
            icon.rectTransform.offsetMax = new Vector2(-40f, -34f);
            icon.rectTransform.offsetMin = new Vector2(40f, 14f);

            TMP_Text ammo = CreateText("Ammo", slot, "", "Bold", 24f, TextColor, TextAlignmentOptions.BottomRight);
            Stretch(ammo.rectTransform, 12f);

            GameObject highlight = CreateFrame(slot, "Highlight", HighlightColor);

            Image marker = CreateImage("Active", slot, LoadSprite("Frame_Large_06"), AccentColor, 2.5f);
            Stretch(marker.rectTransform, -4f);
            marker.raycastTarget = false;
            marker.gameObject.SetActive(false);

            return AddSlotView(slot, icon, ammo, highlight, marker.gameObject);
        }

        private static GameObject CreateFrame(Transform parent, string name, Color color)
        {
            Image frame = CreateImage(name, parent, LoadSprite("Box_Medium_02"), color, 2.5f);
            Stretch(frame.rectTransform);
            frame.raycastTarget = false;
            frame.gameObject.SetActive(false);
            return frame.gameObject;
        }

        private static InventorySlotView AddSlotView(RectTransform slot, Image icon, TMP_Text count, GameObject highlight, GameObject marker)
        {
            InventorySlotView view = slot.gameObject.AddComponent<InventorySlotView>();
            SerializedObject so = new(view);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("countLabel").objectReferenceValue = count;
            so.FindProperty("highlight").objectReferenceValue = highlight;
            so.FindProperty("activeMarker").objectReferenceValue = marker;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, float pixelsPerUnitMultiplier = 1f)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, string font, float size, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Fonts + font + ".asset");
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        private static void Place(RectTransform rect, Vector2 topLeft, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static Sprite LoadSprite(string name)
        {
            string path = HudSprites + name + ".png";
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                    return sprite;
            }

            throw new System.IO.FileNotFoundException("Sprite not found at " + path);
        }

        private static T Require<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component == null)
                throw new System.InvalidOperationException($"{typeof(T).Name} not found on {root.name}");
            return component;
        }

        private static void SetArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
