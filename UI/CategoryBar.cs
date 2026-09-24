using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkbenchesPlus
{
    /// <summary>Category chips under repair, cloned from vanilla Craft/Repair buttons.</summary>
    internal static class CategoryBar
    {
        public static CraftCategory Active { get; private set; } = CraftCategory.All;

        private const float DefaultChipWidth = 96f;
        private const float ChipHeight = 26f;
        private const float ChipSpacing = 3f;
        private const float GapBelowRepair = 10f;
        private const float FontSize = 13f;
        private const float FontSizeMin = 10f;
        private const float FontSizeMax = 14f;

        private static GameObject _root;
        private static float _chipWidth = DefaultChipWidth;
        private static string _builtProfileKey;
        private static readonly List<Button> Buttons = new List<Button>();
        private static readonly List<TMP_Text> Labels = new List<TMP_Text>();
        private static readonly List<Image> Backgrounds = new List<Image>();
        private static readonly List<CraftCategory> Cats = new List<CraftCategory>();
        private static readonly HashSet<CraftCategory> Available = new HashSet<CraftCategory>();

        public static void Show(InventoryGui gui)
        {
            if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value || !Plugin.Settings.EnableCategories.Value)
            {
                Hide();
                return;
            }

            Player player = Player.m_localPlayer;
            CraftingStation station = player != null ? player.GetCurrentCraftingStation() : null;
            if (StationFilter.IsUpgrader(station))
            {
                if (Active != CraftCategory.All)
                    SetActive(CraftCategory.All, rebuild: false);
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

            string profileKey = StationCategoryProfiles.ProfileKey(station);
            if (_root != null && (_root.transform.parent != host || _builtProfileKey != profileKey))
            {
                if (Active != CraftCategory.All)
                    SetActive(CraftCategory.All, rebuild: false);
                DestroyUiOnly();
            }

            if (_root == null)
                Build(gui, host, station);

            PlaceUnderRepair(repair);
            ApplyVisibility();
            _root.SetActive(true);
            RefreshHighlights();
        }

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
            _builtProfileKey = null;
            Buttons.Clear();
            Labels.Clear();
            Backgrounds.Clear();
            Cats.Clear();
        }

        public static void SetActive(CraftCategory cat, bool rebuild = true)
        {
            if (Active == cat)
                return;
            Active = cat;
            RefreshHighlights();
            if (rebuild)
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

                // Prefer readable text width; never shrink below DefaultChipWidth.
                if (repairW > width)
                    width = repairW;

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

            _root.transform.SetAsFirstSibling();
        }

        private static void Build(InventoryGui gui, RectTransform host, CraftingStation station)
        {
            Buttons.Clear();
            Labels.Clear();
            Backgrounds.Clear();
            Cats.Clear();

            _root = new GameObject("WBP_Categories", typeof(RectTransform));
            _root.transform.SetParent(host, false);
            _builtProfileKey = StationCategoryProfiles.ProfileKey(station);

            var layout = _root.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = ChipSpacing;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Button template = ResolveTemplate(gui);
            TMP_FontAsset font = null;
            TMP_Text sample = host.GetComponentInChildren<TMP_Text>(true);
            if (sample != null)
                font = sample.font;

            CraftCategory[] order = StationCategoryProfiles.OrderFor(station);
            for (int i = 0; i < order.Length; i++)
            {
                CraftCategory cat = order[i];
                Cats.Add(cat);
                GameObject btnGo = MakeChip(RecipeCategories.Label(cat), template, font);
                btnGo.transform.SetParent(_root.transform, false);
                Button btn = btnGo.GetComponent<Button>();
                CraftCategory captured = cat;
                if (btn != null)
                    btn.onClick.AddListener(() => SetActive(captured));
                Buttons.Add(btn);
            }
        }

        /// <summary>
        /// Craft is the wood text button; Repair is the same chrome family. Prefer Craft for labels.
        /// </summary>
        private static Button ResolveTemplate(InventoryGui gui)
        {
            RectTransform craftRt = AccessToolsExt.CraftButton(gui);
            if (craftRt != null)
            {
                Button craft = craftRt.GetComponent<Button>();
                if (craft != null)
                    return craft;
            }

            RectTransform repairRt = AccessToolsExt.RepairButton(gui);
            if (repairRt != null)
                return repairRt.GetComponent<Button>();

            return null;
        }

        private static GameObject MakeChip(string text, Button template, TMP_FontAsset font)
        {
            GameObject go;
            if (template != null)
            {
                go = Object.Instantiate(template.gameObject);
                go.name = "Cat_" + text;
                StripListeners(go);
                PrepareClonedButton(go, text, font);
            }
            else
            {
                go = MakeFallbackChip(text, font);
            }

            ApplyChipLayout(go);
            return go;
        }

        private static void PrepareClonedButton(GameObject go, string text, TMP_FontAsset font)
        {
            // Keep root Image (wood/iron chrome). Hide icon-only child images (hammer etc.).
            Image rootImg = go.GetComponent<Image>();
            if (rootImg != null)
            {
                if (rootImg.type == Image.Type.Simple)
                    rootImg.type = Image.Type.Sliced;
                rootImg.color = Color.white;
                Backgrounds.Add(rootImg);
            }
            else
            {
                Backgrounds.Add(null);
            }

            Image[] images = go.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image img = images[i];
                if (img == null || img == rootImg)
                    continue;
                // Child graphics are usually icons, not the button box.
                img.enabled = false;
                img.raycastTarget = false;
            }

            TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(go.transform, false);
                RectTransform lrt = labelGo.transform as RectTransform;
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(6f, 1f);
                lrt.offsetMax = new Vector2(-6f, -1f);
                label = labelGo.GetComponent<TextMeshProUGUI>();
            }

            StyleLabel(label, text, font);
            Labels.Add(label);

            // Drop leftover vanilla text components that are not TMP.
            Text[] legacy = go.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacy.Length; i++)
            {
                if (legacy[i] != null)
                    legacy[i].enabled = false;
            }
        }

        private static GameObject MakeFallbackChip(string text, TMP_FontAsset font)
        {
            var go = new GameObject("Cat_" + text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Image img = go.GetComponent<Image>();
            img.color = new Color(0.12f, 0.11f, 0.1f, 0.9f);
            Backgrounds.Add(img);

            Button btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform lrt = labelGo.transform as RectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(6f, 1f);
            lrt.offsetMax = new Vector2(-6f, -1f);

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            StyleLabel(tmp, text, font);
            Labels.Add(tmp);
            return go;
        }

        private static void StyleLabel(TMP_Text label, string text, TMP_FontAsset font)
        {
            if (label == null)
                return;

            label.text = text;
            label.fontSize = FontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.92f, 0.82f, 0.55f, 1f);
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = FontSizeMin;
            label.fontSizeMax = FontSizeMax;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            if (font != null)
                label.font = font;

            RectTransform lrt = label.transform as RectTransform;
            if (lrt != null)
            {
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(6f, 1f);
                lrt.offsetMax = new Vector2(-6f, -1f);
                lrt.localScale = Vector3.one;
            }
        }

        private static void ApplyChipLayout(GameObject go)
        {
            RectTransform rt = go.transform as RectTransform;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, ChipHeight);
            rt.anchoredPosition = Vector2.zero;

            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = go.AddComponent<LayoutElement>();
            le.minWidth = _chipWidth;
            le.preferredWidth = _chipWidth;
            le.minHeight = ChipHeight;
            le.preferredHeight = ChipHeight;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;
        }

        private static void StripListeners(GameObject go)
        {
            Button btn = go.GetComponent<Button>();
            if (btn != null)
                btn.onClick.RemoveAllListeners();

            var triggers = go.GetComponentsInChildren<UnityEngine.EventSystems.EventTrigger>(true);
            for (int i = 0; i < triggers.Length; i++)
                Object.Destroy(triggers[i]);
        }

        private static void ResizeChips()
        {
            for (int i = 0; i < Buttons.Count; i++)
            {
                if (Buttons[i] == null)
                    continue;
                LayoutElement le = Buttons[i].GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.minWidth = _chipWidth;
                    le.preferredWidth = _chipWidth;
                    le.minHeight = ChipHeight;
                    le.preferredHeight = ChipHeight;
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
                if (i < Backgrounds.Count && Backgrounds[i] != null)
                {
                    // Tint only — keep vanilla wood/iron sprite readable.
                    Backgrounds[i].color = on
                        ? Color.white
                        : new Color(0.72f, 0.7f, 0.65f, 1f);
                }

                if (i < Labels.Count && Labels[i] != null)
                {
                    Labels[i].color = on
                        ? new Color(1f, 0.92f, 0.55f, 1f)
                        : new Color(0.82f, 0.76f, 0.58f, 1f);
                }
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
        private static readonly System.Reflection.FieldInfo CraftButtonField =
            HarmonyLib.AccessTools.Field(typeof(InventoryGui), "m_craftButton");
        private static readonly System.Reflection.FieldInfo TabCraftField =
            HarmonyLib.AccessTools.Field(typeof(InventoryGui), "m_tabCraft");
        private static readonly System.Reflection.FieldInfo TabUpgradeField =
            HarmonyLib.AccessTools.Field(typeof(InventoryGui), "m_tabUpgrade");
        private static readonly System.Reflection.FieldInfo QualityLevelUpField =
            HarmonyLib.AccessTools.Field(typeof(InventoryGui), "m_qualityLevelUp");
        private static readonly System.Reflection.FieldInfo QualityLevelDownField =
            HarmonyLib.AccessTools.Field(typeof(InventoryGui), "m_qualityLevelDown");
        private static readonly System.Reflection.MethodInfo UpdateCraftingPanel =
            HarmonyLib.AccessTools.Method(typeof(InventoryGui), "UpdateCraftingPanel", new[] { typeof(bool) });

        private static int _rebuildDepth;

        public static bool IsRebuilding => _rebuildDepth > 0;

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

        public static RectTransform CraftButton(InventoryGui gui)
        {
            if (CraftButtonField == null || gui == null)
                return null;
            Button btn = CraftButtonField.GetValue(gui) as Button;
            return btn != null ? btn.transform as RectTransform : null;
        }

        public static Button TabCraft(InventoryGui gui)
        {
            if (TabCraftField == null || gui == null)
                return null;
            return TabCraftField.GetValue(gui) as Button;
        }

        public static Button TabUpgrade(InventoryGui gui)
        {
            if (TabUpgradeField == null || gui == null)
                return null;
            return TabUpgradeField.GetValue(gui) as Button;
        }

        public static Button QualityLevelUp(InventoryGui gui)
        {
            if (QualityLevelUpField == null || gui == null)
                return null;
            return QualityLevelUpField.GetValue(gui) as Button;
        }

        public static Button QualityLevelDown(InventoryGui gui)
        {
            if (QualityLevelDownField == null || gui == null)
                return null;
            return QualityLevelDownField.GetValue(gui) as Button;
        }

        public static void RebuildCraftingPanel()
        {
            if (_rebuildDepth > 0)
                return;

            InventoryGui gui = InventoryGui.instance;
            if (gui == null || UpdateCraftingPanel == null)
                return;

            _rebuildDepth++;
            try
            {
                UpdateCraftingPanel.Invoke(gui, new object[] { false });
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("RebuildCraftingPanel: " + ex.Message);
            }
            finally
            {
                _rebuildDepth--;
            }
        }
    }
}
