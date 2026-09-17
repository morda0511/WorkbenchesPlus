using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkbenchesPlus
{
    /// <summary>Narrow vertical category chips stacked under the repair button.</summary>
    internal static class CategoryBar
    {
        public static CraftCategory Active { get; private set; } = CraftCategory.All;

        private const float DefaultChipWidth = 78f;
        private const float ChipHeight = 17f;
        private const float ChipSpacing = 2f;
        private const float GapBelowRepair = 6f;

        private static GameObject _root;
        private static float _chipWidth = DefaultChipWidth;
        private static readonly List<Button> Buttons = new List<Button>();
        private static readonly List<TMP_Text> Labels = new List<TMP_Text>();
        private static readonly List<CraftCategory> Cats = new List<CraftCategory>();
        private static readonly HashSet<CraftCategory> Available = new HashSet<CraftCategory>();

        public static void Show(InventoryGui gui)
        {
            if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value || !Plugin.Settings.EnableCategories.Value)
            {
                Hide();
                return;
            }

            if (gui == null)
                return;

            RectTransform repair = AccessToolsExt.RepairButton(gui);
            RectTransform host = repair != null ? repair.parent as RectTransform : null;
            if (host == null)
                host = AccessToolsExt.CraftingPanel(gui);
            if (host == null)
                host = AccessToolsExt.RecipeListRoot(gui);
            if (host == null)
                return;

            // Always rebuild under the correct parent so a bad first host does not stick.
            if (_root != null && _root.transform.parent != host)
                DestroyUiOnly();

            if (_root == null)
                Build(host);

            PlaceUnderRepair(repair);
            ApplyVisibility();
            _root.SetActive(true);
            RefreshHighlights();
        }

        /// <summary>
        /// Show only category chips that appear in this station's recipe list.
        /// Call with the full (pre-filter) list. Silent-resets Active if it vanishes.
        /// </summary>
        public static void SyncAvailable(IList<Recipe> recipes)
        {
            Available.Clear();
            Available.Add(CraftCategory.All);
            if (recipes != null)
            {
                for (int i = 0; i < recipes.Count; i++)
                {
                    Recipe r = recipes[i];
                    if (r == null)
                        continue;
                    Available.Add(RecipeCategories.Classify(r));
                }
            }

            if (Active != CraftCategory.All && !Available.Contains(Active))
                Active = CraftCategory.All;

            ApplyVisibility();
            RefreshHighlights();
        }

        public static void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        public static void Destroy()
        {
            DestroyUiOnly();
            Available.Clear();
            Active = CraftCategory.All;
        }

        private static void DestroyUiOnly()
        {
            if (_root != null)
                Object.Destroy(_root);
            _root = null;
            Buttons.Clear();
            Labels.Clear();
            Cats.Clear();
        }

        public static void SetActive(CraftCategory cat)
        {
            if (Active == cat)
                return;
            Active = cat;
            RefreshHighlights();
            AccessToolsExt.RebuildCraftingPanel();
        }

        private static void PlaceUnderRepair(RectTransform repair)
        {
            if (_root == null)
                return;

            RectTransform rt = _root.transform as RectTransform;
            float width = DefaultChipWidth;

            if (repair != null)
            {
                float repairW = repair.rect.width;
                if (repairW < 8f)
                    repairW = Mathf.Abs(repair.sizeDelta.x);
                if (repairW < 8f)
                    repairW = 40f;

                float repairH = repair.rect.height;
                if (repairH < 8f)
                    repairH = Mathf.Abs(repair.sizeDelta.y);
                if (repairH < 8f)
                    repairH = repairW;

                // Same anchor space as repair; X = repair button, Y stays under it.
                rt.anchorMin = repair.anchorMin;
                rt.anchorMax = repair.anchorMax;
                rt.pivot = new Vector2(0.5f, 1f);

                float repairBottom = repair.anchoredPosition.y - repair.pivot.y * repairH;
                rt.anchoredPosition = new Vector2(
                    repair.anchoredPosition.x,
                    repairBottom - GapBelowRepair);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(28f, -92f);
            }

            if (Mathf.Abs(_chipWidth - width) > 0.5f)
            {
                _chipWidth = width;
                ResizeChips();
            }

            int visible = CountVisible();
            if (visible < 1)
                visible = 1;
            float height = visible * ChipHeight + (visible - 1) * ChipSpacing + 4f;
            rt.sizeDelta = new Vector2(_chipWidth, height);

            // Behind workbench chrome (layer under).
            _root.transform.SetAsFirstSibling();
        }

        private static void Build(RectTransform host)
        {
            Buttons.Clear();
            Labels.Clear();
            Cats.Clear();

            _root = new GameObject("WBP_Categories", typeof(RectTransform));
            _root.transform.SetParent(host, false);

            var layout = _root.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = ChipSpacing;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TMP_FontAsset font = null;
            TMP_Text sample = host.GetComponentInChildren<TMP_Text>(true);
            if (sample != null)
                font = sample.font;

            for (int i = 0; i < RecipeCategories.Order.Length; i++)
            {
                CraftCategory cat = RecipeCategories.Order[i];
                Cats.Add(cat);
                GameObject btnGo = MakeChip(RecipeCategories.Label(cat), font);
                btnGo.transform.SetParent(_root.transform, false);
                Button btn = btnGo.GetComponent<Button>();
                CraftCategory captured = cat;
                btn.onClick.AddListener(() => SetActive(captured));
                Buttons.Add(btn);
                Labels.Add(btnGo.GetComponentInChildren<TMP_Text>());
            }
        }

        private static GameObject MakeChip(string text, TMP_FontAsset font)
        {
            var go = new GameObject("Cat_" + text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rt = go.transform as RectTransform;
            rt.sizeDelta = new Vector2(_chipWidth, ChipHeight);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minWidth = _chipWidth;
            le.preferredWidth = _chipWidth;
            le.minHeight = ChipHeight;
            le.preferredHeight = ChipHeight;
            le.flexibleWidth = 0f;

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.12f, 0.11f, 0.1f, 0.9f);

            Button btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform lrt = labelGo.transform as RectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4f, 0f);
            lrt.offsetMax = new Vector2(-2f, 0f);

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 11f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(0.92f, 0.82f, 0.55f, 1f);
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 8f;
            tmp.fontSizeMax = 11f;
            tmp.overflowMode = TextOverflowModes.Overflow;
            if (font != null)
                tmp.font = font;

            return go;
        }

        private static void ResizeChips()
        {
            for (int i = 0; i < Buttons.Count; i++)
            {
                if (Buttons[i] == null)
                    continue;
                RectTransform rt = Buttons[i].transform as RectTransform;
                if (rt != null)
                    rt.sizeDelta = new Vector2(_chipWidth, ChipHeight);
                LayoutElement le = Buttons[i].GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.minWidth = _chipWidth;
                    le.preferredWidth = _chipWidth;
                }
            }
        }

        private static int CountVisible()
        {
            bool synced = Available.Count > 0;
            int visible = 0;
            for (int i = 0; i < Cats.Count; i++)
            {
                if (!synced || Available.Contains(Cats[i]))
                    visible++;
            }
            return visible;
        }

        private static void ApplyVisibility()
        {
            if (Buttons.Count == 0)
                return;

            bool synced = Available.Count > 0;
            int visible = 0;
            for (int i = 0; i < Buttons.Count; i++)
            {
                bool show = !synced || Available.Contains(Cats[i]);
                if (Buttons[i] != null)
                    Buttons[i].gameObject.SetActive(show);
                if (show)
                    visible++;
            }

            if (_root != null && visible > 0)
            {
                RectTransform rt = _root.transform as RectTransform;
                if (rt != null)
                {
                    float height = visible * ChipHeight + (visible - 1) * ChipSpacing + 4f;
                    rt.sizeDelta = new Vector2(_chipWidth, height);
                }
            }
        }

        private static void RefreshHighlights()
        {
            for (int i = 0; i < Buttons.Count; i++)
            {
                bool on = Cats[i] == Active;
                Image img = Buttons[i].GetComponent<Image>();
                if (img != null)
                    img.color = on
                        ? new Color(0.35f, 0.28f, 0.12f, 0.95f)
                        : new Color(0.12f, 0.11f, 0.1f, 0.9f);
                if (i < Labels.Count && Labels[i] != null)
                    Labels[i].color = on
                        ? new Color(1f, 0.92f, 0.55f, 1f)
                        : new Color(0.75f, 0.7f, 0.55f, 1f);
            }
        }
    }

    internal static class AccessToolsExt
    {
        private static readonly System.Reflection.FieldInfo CraftingField =
            HarmonyLib.AccessTools.Field(typeof(InventoryGui), "m_crafting");
        private static readonly System.Reflection.FieldInfo RecipeListRootField =
            HarmonyLib.AccessTools.Field(typeof(InventoryGui), "m_recipeListRoot");
        private static readonly System.Reflection.FieldInfo RepairButtonField =
            HarmonyLib.AccessTools.Field(typeof(InventoryGui), "m_repairButton");
        private static readonly System.Reflection.MethodInfo UpdateCraftingPanel =
            HarmonyLib.AccessTools.Method(typeof(InventoryGui), "UpdateCraftingPanel", new[] { typeof(bool) });

        public static RectTransform CraftingPanel(InventoryGui gui)
        {
            return CraftingField != null ? CraftingField.GetValue(gui) as RectTransform : null;
        }

        public static RectTransform RecipeListRoot(InventoryGui gui)
        {
            return RecipeListRootField != null ? RecipeListRootField.GetValue(gui) as RectTransform : null;
        }

        public static RectTransform RepairButton(InventoryGui gui)
        {
            if (RepairButtonField == null || gui == null)
                return null;
            Button btn = RepairButtonField.GetValue(gui) as Button;
            return btn != null ? btn.transform as RectTransform : null;
        }

        public static void RebuildCraftingPanel()
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || UpdateCraftingPanel == null)
                return;
            UpdateCraftingPanel.Invoke(gui, new object[] { false });
        }
    }
}
