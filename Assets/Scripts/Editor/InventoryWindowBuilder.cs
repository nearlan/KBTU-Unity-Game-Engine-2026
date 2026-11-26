using RUSH.Core;
using RUSH.Items;
using RUSH.Players;
using RUSH.UI;
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
        private const int HandColumns = 2;
        private const float Margin = 60f;
        private const float ColumnGap = 50f;
        private const float ContentTop = 170f;

        private static readonly Color PanelColor = new(0.07f, 0.08f, 0.07f, 0.94f);
        private static readonly Color DimColor = new(0f, 0f, 0f, 0.55f);
        private static readonly Color CellColor = new(0.16f, 0.17f, 0.15f, 0.95f);
        private static readonly Color ItemColor = new(0.3f, 0.31f, 0.27f, 0.97f);
        private static readonly Color ValidColor = new(0.25f, 0.55f, 0.25f, 0.95f);
        private static readonly Color InvalidColor = new(0.6f, 0.18f, 0.15f, 0.95f);
        private static readonly Color HighlightColor = new(1f, 1f, 1f, 0.14f);
        private static readonly Color AccentColor = new(0.96f, 0.78f, 0.25f, 1f);
        private static readonly Color TextColor = new(0.92f, 0.9f, 0.85f, 1f);
        private static readonly Color MutedTextColor = new(0.92f, 0.9f, 0.85f, 0.55f);

        private static readonly string[] HandTitles = { "ОСНОВНОЕ", "ЗАПАСНОЕ" };

        private static float Step => CellSize + CellSpacing;

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

            float slotWidth = GridLength(HandColumns);
            float gridWidth = GridLength(inventory.Columns);
            float gridHeight = GridLength(inventory.Rows);
            float slotsHeight = HandTitles.Length * (36f + CellSize) + (HandTitles.Length - 1) * 24f;
            float panelWidth = Margin + slotWidth + ColumnGap + gridWidth + Margin;
            float panelHeight = ContentTop + Mathf.Max(gridHeight, slotsHeight) + Margin;

            Image panel = CreateImage("Panel", window, LoadSprite("Box_Large_02"), PanelColor, 2f);
            panel.rectTransform.sizeDelta = new Vector2(panelWidth, panelHeight);

            TMP_Text title = CreateText("Title", panel.transform, "ИНВЕНТАРЬ", "Header", 54f, TextColor, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(Margin, -40f), new Vector2(600f, 64f));

            Image line = CreateImage("Divider", panel.transform, LoadSprite("Line_01"), AccentColor);
            Place(line.rectTransform, new Vector2(Margin, -108f), new Vector2(panelWidth - 2f * Margin, 6f));

            InventoryItemView itemTemplate = CreateItemView("ItemTemplate", window);
            itemTemplate.gameObject.SetActive(false);

            InventoryGridView[] handGrids = new InventoryGridView[HandTitles.Length];
            float slotTop = ContentTop - 45f;
            for (int i = 0; i < handGrids.Length; i++)
            {
                TMP_Text slotTitle = CreateText($"Hands{i}Label", panel.transform, HandTitles[i], "Bold", 26f, AccentColor, TextAlignmentOptions.Left);
                Place(slotTitle.rectTransform, new Vector2(Margin, -slotTop), new Vector2(slotWidth, 36f));

                handGrids[i] = CreateGrid($"Hands_{i}", panel.transform, InventoryGridKind.Hands, i, HandColumns, 1, itemTemplate);
                Place((RectTransform)handGrids[i].transform, new Vector2(Margin, -(slotTop + 40f)), new Vector2(slotWidth, CellSize));

                slotTop += 36f + CellSize + 24f + 4f;
            }

            float gridLeft = Margin + slotWidth + ColumnGap;
            TMP_Text backpackLabel = CreateText("BackpackLabel", panel.transform, "РЮКЗАК", "Bold", 26f, AccentColor, TextAlignmentOptions.Left);
            Place(backpackLabel.rectTransform, new Vector2(gridLeft, -(ContentTop - 45f)), new Vector2(gridWidth, 36f));

            InventoryGridView backpack = CreateGrid("Backpack", panel.transform, InventoryGridKind.Backpack, 0, inventory.Columns, inventory.Rows, itemTemplate);
            Place((RectTransform)backpack.transform, new Vector2(gridLeft, -(ContentTop - 5f)), new Vector2(gridWidth, gridHeight));

            InventoryItemView ghost = CreateItemView("DragGhost", window);
            ghost.gameObject.SetActive(false);

            InventoryWindow view = window.gameObject.AddComponent<InventoryWindow>();
            SerializedObject so = new(view);
            so.FindProperty("owner").objectReferenceValue = Require<Entity>(playerRoot);
            so.FindProperty("input").objectReferenceValue = Require<PlayerInputReader>(playerRoot);
            so.FindProperty("inventory").objectReferenceValue = inventory;
            so.FindProperty("itemActions").objectReferenceValue = Require<PlayerItemActions>(playerRoot);
            so.FindProperty("weaponHandler").objectReferenceValue = Require<PlayerWeaponHandler>(playerRoot);
            so.FindProperty("look").objectReferenceValue = Require<PlayerLook>(playerRoot);
            so.FindProperty("panel").objectReferenceValue = panel.rectTransform;
            so.FindProperty("backpackGrid").objectReferenceValue = backpack;
            so.FindProperty("dragGhost").objectReferenceValue = ghost;
            SetArray(so.FindProperty("handGrids"), handGrids);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static InventoryGridView CreateGrid(string name, Transform parent, InventoryGridKind kind, int rowOffset,
            int columns, int rows, InventoryItemView itemTemplate)
        {
            Image hitArea = CreateImage(name, parent, null, Color.clear);
            RectTransform grid = hitArea.rectTransform;

            Image[] cells = new Image[columns * rows];
            for (int i = 0; i < cells.Length; i++)
            {
                Image cell = CreateImage($"Cell_{i:00}", grid, LoadSprite("Box_Medium_02"), CellColor, 2.5f);
                Place(cell.rectTransform, new Vector2(i % columns * Step, -(i / columns) * Step), new Vector2(CellSize, CellSize));
                cells[i] = cell;
            }

            RectTransform items = CreateRect("Items", grid);
            Stretch(items);

            InventoryGridView view = grid.gameObject.AddComponent<InventoryGridView>();
            SerializedObject so = new(view);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("rowOffset").intValue = rowOffset;
            so.FindProperty("columns").intValue = columns;
            so.FindProperty("rows").intValue = rows;
            so.FindProperty("cellSize").floatValue = CellSize;
            so.FindProperty("spacing").floatValue = CellSpacing;
            so.FindProperty("itemsRoot").objectReferenceValue = items;
            so.FindProperty("itemTemplate").objectReferenceValue = itemTemplate;
            so.FindProperty("cellColor").colorValue = CellColor;
            so.FindProperty("validColor").colorValue = ValidColor;
            so.FindProperty("invalidColor").colorValue = InvalidColor;
            SetArray(so.FindProperty("cells"), cells);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static InventoryItemView CreateItemView(string name, Transform parent)
        {
            RectTransform root = CreateRect(name, parent);
            root.sizeDelta = new Vector2(CellSize, CellSize);

            Image background = CreateImage("Background", root, LoadSprite("Box_Medium_02"), ItemColor, 2.5f);
            Stretch(background.rectTransform);

            Image icon = CreateImage("Icon", root, null, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TMP_Text label = CreateText("Label", root, "", "Bold", 24f, TextColor, TextAlignmentOptions.BottomRight);
            Stretch(label.rectTransform, 8f);

            Image highlight = CreateImage("Highlight", root, LoadSprite("Box_Medium_02"), HighlightColor, 2.5f);
            Stretch(highlight.rectTransform);
            highlight.raycastTarget = false;
            highlight.gameObject.SetActive(false);

            Image selected = CreateImage("Selected", root, LoadSprite("Frame_Large_06"), AccentColor, 2.5f);
            Stretch(selected.rectTransform, -4f);
            selected.raycastTarget = false;
            selected.gameObject.SetActive(false);

            InventoryItemView view = root.gameObject.AddComponent<InventoryItemView>();
            SerializedObject so = new(view);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("highlight").objectReferenceValue = highlight.gameObject;
            so.FindProperty("selectedMarker").objectReferenceValue = selected.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static float GridLength(int cells)
        {
            return cells * CellSize + (cells - 1) * CellSpacing;
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
