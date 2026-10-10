using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Category chips under repair, cloned from vanilla Craft/Repair buttons.
    /// Under a selected category a compact filter panel opens (Weapons: 1-Hand | 2-Hand and a grid of
    /// weapon-type icons).
    /// </summary>
    internal static class CategoryBar
    {
        // One chip set for every station (empty chips are hidden), so the bar is built once.
        private const string ProfileKey = "chips";

        // Valheim gold (same as the section headers); active chip a bit brighter.
        private static readonly Color ChipGold = new Color(1f, 0.78f, 0.3f, 1f);
        private static readonly Color ChipGoldActive = new Color(1f, 0.9f, 0.45f, 1f);
        private static readonly Color Dim = new Color(0.72f, 0.7f, 0.65f, 1f);

        public static CraftCategory Active { get; private set; } = CraftCategory.All;

        private const float DefaultChipWidth = 96f;
        private const float ChipHeight = 26f;
        private const float ChipSpacing = 3f;
        private const float GapBelowRepair = 10f;
        private const float FontSize = 13f;
        private const float FontSizeMin = 10f;
        private const float FontSizeMax = 14f;

        // Filter panel: one grid of symbols, positioned by hand (no layout groups: the chip column
        // takes row heights from the RectTransform, not from LayoutElement).
        // (values come from the layout file, see LayoutOverrides)
        private static int GridColumns { get { return Mathf.Clamp(Mathf.RoundToInt(LayoutOverrides.Get("sub.columns", 4f)), 1, 8); } }
        private static float CellSize { get { return Mathf.Clamp(LayoutOverrides.Get("sub.cell", 26f), 16f, 56f); } }
        private static float CellGap { get { return Mathf.Clamp(LayoutOverrides.Get("sub.gap", 2f), 0f, 12f); } }
        private static float PanelWidth { get { return GridColumns * CellSize + (GridColumns - 1) * CellGap; } }
        private static readonly Color IconOn = new Color(1f, 0.92f, 0.5f, 1f);
        private static readonly Color IconOff = new Color(0.86f, 0.80f, 0.60f, 1f);
        // Own PNG icons keep their colours: only brightness changes between selected and not.
        private static readonly Color PngOn = Color.white;
        private static readonly Color PngOff = new Color(0.72f, 0.72f, 0.72f, 1f);

        private sealed class SubCell
        {
            public string Key;
            public GameObject Go;
            public Image Background;
            public Image Icon;
            public bool Boxed;          // the PNG is a finished box (frame + symbol)
            public Outline Selection;   // gold edge while selected
        }

        private sealed class SubPanel
        {
            public CraftCategory Category;
            public GameObject Root;
            public RectTransform Content;
            public readonly List<SubCell> Cells = new List<SubCell>();
        }

        private static GameObject _root;
        private static float _chipWidth = DefaultChipWidth;
        private static string _builtProfileKey;
        private static readonly List<Button> Buttons = new List<Button>();
        private static readonly List<TMP_Text> Labels = new List<TMP_Text>();
        private static readonly List<Image> Backgrounds = new List<Image>();
        private static readonly List<CraftCategory> Cats = new List<CraftCategory>();
        private static readonly HashSet<CraftCategory> Available = new HashSet<CraftCategory>();
        private static readonly Dictionary<CraftCategory, SubPanel> Panels = new Dictionary<CraftCategory, SubPanel>();
        private static readonly HashSet<string> AvailableSubs = new HashSet<string>();
        private static readonly HashSet<string> ActiveSubs = new HashSet<string>();

        public static bool HasSubFilter
        {
            get { return ActiveSubs.Count > 0; }
        }

        /// <summary>A recipe passes when it carries every selected sub filter (1-Hand AND Axes).</summary>
        public static bool PassesSubFilter(RecipeInfo info)
        {
            if (ActiveSubs.Count == 0)
                return true;
            if (info == null)
                return false;
            foreach (string key in ActiveSubs)
            {
                if (!info.HasSub(key))
                    return false;
            }
            return true;
        }

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

            string profileKey = ProfileKey;
            if (_root != null && (_root.transform.parent != host || _builtProfileKey != profileKey))
            {
                if (Active != CraftCategory.All)
                    SetActive(CraftCategory.All, rebuild: false);
                DestroyUiOnly();
            }

            if (_root == null)
                Build(gui, host);

            PlaceUnderRepair(repair);
            ApplyVisibility();
            _root.SetActive(true);
            RefreshHighlights();
        }

        public static void SyncAvailable(IList<Recipe> recipes)
        {
            Available.Clear();
            AvailableSubs.Clear();
            Available.Add(CraftCategory.All);

            if (recipes != null)
            {
                for (int i = 0; i < recipes.Count; i++)
                {
                    Recipe r = recipes[i];
                    if (r == null)
                        continue;
                    RecipeInfo info = SortEngine.Info(r);
                    Available.Add(info != null ? info.Category : CraftCategory.Misc);
                    if (info == null)
                        continue;

                    for (int k = 0; k < info.Subs.Length; k++)
                        AvailableSubs.Add(info.Subs[k]);
                }
            }

            if (Active != CraftCategory.All && !Available.Contains(Active))
            {
                Active = CraftCategory.All;
                ActiveSubs.Clear();
            }
            ActiveSubs.RemoveWhere(k => !AvailableSubs.Contains(k));

            if (EnsureUiAlive())
            {
                ApplyVisibility();
                RefreshHighlights();
            }
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
            AvailableSubs.Clear();
            Active = CraftCategory.All;
            ActiveSubs.Clear();
        }

        private static void DestroyUiOnly()
        {
            // Unity overloaded == : destroyed objects compare as null.
            if (_root != null)
                Object.Destroy(_root);
            ClearUiLists();
        }

        /// <summary>
        /// Category chips live under InventoryGui; opening chests / rebuilding the panel can
        /// destroy them without our Hide/Destroy. Drop stale refs so SyncAvailable cannot NRE.
        /// </summary>
        private static bool EnsureUiAlive()
        {
            if (_root != null)
                return true;
            ClearUiLists();
            return false;
        }

        private static void ClearUiLists()
        {
            _root = null;
            _builtProfileKey = null;
            Buttons.Clear();
            Labels.Clear();
            Backgrounds.Clear();
            Cats.Clear();
            Panels.Clear();
        }

        public static void SetActive(CraftCategory cat, bool rebuild = true)
        {
            if (Active == cat)
                return;
            Active = cat;
            ActiveSubs.Clear();
            ApplyVisibility();
            RefreshHighlights();
            if (rebuild)
                AccessToolsExt.RebuildCraftingPanel();
        }

        /// <summary>Main chip click: select the category, or clear its sub filters when it is already selected.</summary>
        private static void OnMainClick(CraftCategory cat)
        {
            if (Active == cat && ActiveSubs.Count > 0)
            {
                ActiveSubs.Clear();
                RefreshHighlights();
                AccessToolsExt.RebuildCraftingPanel();
                return;
            }
            SetActive(cat);
        }

        /// <summary>
        /// Sub filter click. One choice per group: 1-Hand replaces 2-Hand, Axes replaces Swords;
        /// clicking the selected one clears it. Hand and type combine (2-Hand + Axes).
        /// </summary>
        private static void OnSubClick(string key)
        {
            if (ActiveSubs.Contains(key))
            {
                ActiveSubs.Remove(key);
            }
            else
            {
                // only one symbol at a time (1-Hand, Axes ... are alternatives, not combinable)
                ActiveSubs.Clear();
                ActiveSubs.Add(key);
            }
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

            float widthOverride = LayoutOverrides.Get("categories.width", 0f);
            if (widthOverride > 20f)
                width = widthOverride;
            LayoutOverrides.Nudge(rt, "categories");

            if (Mathf.Abs(_chipWidth - width) > 0.5f)
            {
                _chipWidth = width;
                ResizeChips();
            }

            rt.sizeDelta = new Vector2(_chipWidth, StackHeight());

            _root.transform.SetAsFirstSibling();
            foreach (SubPanel p in Panels.Values)
            {
                if (p.Root != null && p.Root.activeSelf)
                    ArrangePanel(p, p.Root.transform as RectTransform, PanelHeight(p));
            }
        }

        private static void Build(InventoryGui gui, RectTransform host)
        {
            Buttons.Clear();
            Labels.Clear();
            Backgrounds.Clear();
            Cats.Clear();
            Panels.Clear();

            _root = new GameObject("WBP_Categories", typeof(RectTransform));
            _root.transform.SetParent(host, false);
            _builtProfileKey = ProfileKey;

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

            CraftCategory[] order = CraftCategories.ChipOrder;
            for (int i = 0; i < order.Length; i++)
            {
                CraftCategory cat = order[i];
                Cats.Add(cat);
                GameObject btnGo = MakeChip(CraftCategories.Label(cat), template, font);
                btnGo.transform.SetParent(_root.transform, false);
                Button btn = btnGo.GetComponent<Button>();
                CraftCategory captured = cat;
                if (btn != null)
                    btn.onClick.AddListener(() => OnMainClick(captured));
                Buttons.Add(btn);

                // Filter panel directly under its category (only visible while it is selected).
                if (SubCategories.For(cat).Length > 0)
                    BuildPanel(cat, template, font);
            }
        }

        private static void BuildPanel(CraftCategory cat, Button template, TMP_FontAsset font)
        {
            var panel = new SubPanel { Category = cat };

            // The chip column reads the row height from this RectTransform (set in ApplyVisibility).
            panel.Root = new GameObject("WBP_Sub_" + cat, typeof(RectTransform));
            panel.Root.transform.SetParent(_root.transform, false);
            RectTransform rootRt = panel.Root.transform as RectTransform;
            rootRt.sizeDelta = new Vector2(0f, CellSize);

            // Fixed-size content box, centered in the column, cells placed by hand inside it.
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(panel.Root.transform, false);
            panel.Content = content.transform as RectTransform;
            panel.Content.anchorMin = new Vector2(0.5f, 1f);
            panel.Content.anchorMax = new Vector2(0.5f, 1f);
            panel.Content.pivot = new Vector2(0.5f, 1f);
            panel.Content.anchoredPosition = Vector2.zero;
            panel.Content.sizeDelta = new Vector2(PanelWidth, CellSize);

            string[] keys = SubCategories.For(cat);
            for (int i = 0; i < keys.Length; i++)
                panel.Cells.Add(MakeCell(panel, keys[i], template, font));

            Panels[cat] = panel;
        }

        private static SubCell MakeCell(SubPanel panel, string key, Button template, TMP_FontAsset font)
        {
            string title = Loc.T("sub_" + key);
            GameObject go = MakeChip(title, template, font);

            // MakeChip registered the chip in the main-chip lists; the cell keeps its own references.
            Image bg = Backgrounds.Count > 0 ? Backgrounds[Backgrounds.Count - 1] : null;
            TMP_Text label = Labels.Count > 0 ? Labels[Labels.Count - 1] : null;
            if (Backgrounds.Count > 0)
                Backgrounds.RemoveAt(Backgrounds.Count - 1);
            if (Labels.Count > 0)
                Labels.RemoveAt(Labels.Count - 1);
            if (label != null)
            {
                label.text = "";
                label.enabled = false; // symbol only; the name is in the tooltip
            }

            go.name = "Sub_" + key;
            go.transform.SetParent(panel.Content, false);

            // Not part of any layout: fixed square, positioned in ApplyVisibility.
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le != null)
                Object.Destroy(le);
            RectTransform rt = go.transform as RectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(CellSize, CellSize);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            RectTransform irt = iconGo.transform as RectTransform;
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(3f, 3f);
            irt.offsetMax = new Vector2(-3f, -3f);
            Image icon = iconGo.GetComponent<Image>();
            icon.sprite = GlyphIcons.Get(key);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // A PNG from Assets/Icons is a finished box (frame + symbol): it becomes the whole cell.
            bool boxed = GlyphIcons.IsCustom(key) && bg != null;
            Outline selection = null;
            if (boxed)
            {
                bg.sprite = icon.sprite;
                bg.type = Image.Type.Simple;
                bg.preserveAspect = true;
                icon.enabled = false;
                selection = go.AddComponent<Outline>();
                selection.effectColor = new Color(1f, 0.82f, 0.3f, 1f);
                selection.effectDistance = new Vector2(1.5f, -1.5f);
                selection.enabled = false;
            }
            else
            {
                icon.color = IconOff;
            }

            Button btn = go.GetComponent<Button>();
            if (btn != null)
            {
                string captured = key;
                btn.onClick.AddListener(() => OnSubClick(captured));
            }

            // Hover name (the Craft button clone already carries the tooltip prefab).
            UITooltip tip = go.GetComponent<UITooltip>();
            if (tip != null)
            {
                tip.m_text = title;
                tip.m_topic = "";
            }

            return new SubCell { Key = key, Go = go, Background = bg, Icon = icon, Boxed = boxed, Selection = selection };
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

                // The Craft button's tooltip text ("missing requirements") must not come along.
                UITooltip tip = go.GetComponent<UITooltip>();
                if (tip != null)
                {
                    tip.m_text = "";
                    tip.m_topic = "";
                }
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
            // The template is the Craft button: vanilla greys it (interactable=false) when materials
            // are missing. Chips must stay clickable regardless.
            Button cloneBtn = go.GetComponent<Button>();
            if (cloneBtn != null)
            {
                // No colour tint: the Craft template's disabled tint would grey the label.
                cloneBtn.transition = Selectable.Transition.None;
                cloneBtn.interactable = true;
            }

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
            label.color = ChipGold;
            label.canvasRenderer.SetColor(Color.white);
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
            if (!EnsureUiAlive())
                return;

            for (int i = 0; i < Buttons.Count; i++)
            {
                Button btn = Buttons[i];
                if (btn == null)
                    continue;
                LayoutElement le = btn.GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.minWidth = _chipWidth;
                    le.preferredWidth = _chipWidth;
                    le.minHeight = ChipHeight;
                    le.preferredHeight = ChipHeight;
                }
            }
        }

        // ---- visibility and height ---------------------------------------------------------------

        private static bool MainShown(int i)
        {
            return Available.Count == 0 || Available.Contains(Cats[i]);
        }

        /// <summary>Number of symbols of the panel that exist at this station.</summary>
        private static int CountCells(SubPanel panel)
        {
            int n = 0;
            for (int i = 0; i < panel.Cells.Count; i++)
            {
                if (AvailableSubs.Contains(panel.Cells[i].Key))
                    n++;
            }
            return n;
        }

        private static float PanelHeight(SubPanel panel)
        {
            int n = CountCells(panel);
            if (n == 0)
                return 0f;
            int rows = (n + GridColumns - 1) / GridColumns;
            return rows * CellSize + (rows - 1) * CellGap;
        }

        /// <summary>True while a filter-symbol panel (Weapons) is on screen; used by the layout file.</summary>
        internal static bool SubPanelVisible
        {
            get
            {
                foreach (SubPanel p in Panels.Values)
                {
                    if (p.Root != null && p.Root.activeInHierarchy && PanelShown(p))
                        return true;
                }
                return false;
            }
        }

        private static bool PanelShown(SubPanel panel)
        {
            return Active == panel.Category && PanelHeight(panel) > 0f;
        }

        private static float StackHeight()
        {
            float h = 4f;
            int shown = 0;
            for (int i = 0; i < Buttons.Count; i++)
            {
                if (MainShown(i))
                {
                    h += ChipHeight;
                    shown++;
                }

                SubPanel panel;
                if (i < Cats.Count && Panels.TryGetValue(Cats[i], out panel) && MainShown(i) && PanelShown(panel) && !Detached)
                {
                    h += PanelHeight(panel);
                    shown++;
                }
            }
            if (shown > 1)
                h += (shown - 1) * ChipSpacing;
            return h;
        }

        /// <summary>Layout file: show the filter symbols as their own box instead of inside the chip column.</summary>
        private static bool Detached
        {
            get { return LayoutOverrides.Get("sub.detached", 0f) > 0.5f; }
        }

        /// <summary>Inside the chip column (default) or free-standing right of it, moved by "subpanel.dx/dy".</summary>
        private static void ArrangePanel(SubPanel panel, RectTransform rootRt, float height)
        {
            RectTransform column = _root != null ? _root.transform as RectTransform : null;
            if (column == null)
                return;

            if (!Detached)
            {
                if (rootRt.parent != column)
                {
                    rootRt.SetParent(column, false);
                    int idx = Cats.IndexOf(panel.Category);
                    if (idx >= 0 && idx < Buttons.Count && Buttons[idx] != null)
                        rootRt.SetSiblingIndex(Buttons[idx].transform.GetSiblingIndex() + 1);
                }
                rootRt.sizeDelta = new Vector2(0f, height);
                return;
            }

            Transform host = column.parent;
            if (host == null)
                return;
            if (rootRt.parent != host)
                rootRt.SetParent(host, false);
            rootRt.anchorMin = column.anchorMin;
            rootRt.anchorMax = column.anchorMax;
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.sizeDelta = new Vector2(PanelWidth, height);
            rootRt.anchoredPosition = column.anchoredPosition + new Vector2(_chipWidth * 0.5f + 8f + PanelWidth * 0.5f, 0f);
            LayoutOverrides.Nudge(rootRt, "subpanel");
            rootRt.SetAsLastSibling();
        }

        private static void ApplyVisibility()
        {
            if (!EnsureUiAlive() || Buttons.Count == 0)
                return;

            for (int i = 0; i < Buttons.Count; i++)
            {
                Button btn = Buttons[i];
                if (btn != null)
                    btn.gameObject.SetActive(MainShown(i));
            }

            foreach (SubPanel panel in Panels.Values)
            {
                if (panel.Root == null)
                    continue;

                bool show = PanelShown(panel);
                panel.Root.SetActive(show);
                if (!show)
                    continue;

                // Place the symbols that exist here, row by row, left to right.
                int slot = 0;
                for (int i = 0; i < panel.Cells.Count; i++)
                {
                    SubCell cell = panel.Cells[i];
                    if (cell.Go == null)
                        continue;
                    bool exists = AvailableSubs.Contains(cell.Key);
                    cell.Go.SetActive(exists);
                    if (!exists)
                        continue;

                    int col = slot % GridColumns;
                    int row = slot / GridColumns;
                    RectTransform rt = cell.Go.transform as RectTransform;
                    rt.sizeDelta = new Vector2(CellSize, CellSize);
                    rt.anchoredPosition = new Vector2(col * (CellSize + CellGap), -row * (CellSize + CellGap));
                    slot++;
                }

                float height = PanelHeight(panel);
                panel.Content.sizeDelta = new Vector2(PanelWidth, height);
                RectTransform rootRt = panel.Root.transform as RectTransform;
                ArrangePanel(panel, rootRt, height);
            }

            if (_root != null)
            {
                RectTransform rt = _root.transform as RectTransform;
                if (rt != null)
                    rt.sizeDelta = new Vector2(_chipWidth, StackHeight());
            }
        }

        private static void RefreshHighlights()
        {
            if (!EnsureUiAlive())
                return;

            try
            {
                for (int i = 0; i < Buttons.Count; i++)
                {
                    if (Buttons[i] != null && !Buttons[i].interactable)
                        Buttons[i].interactable = true;
                    bool on = i < Cats.Count && Cats[i] == Active;
                    Paint(i < Backgrounds.Count ? Backgrounds[i] : null,
                        i < Labels.Count ? Labels[i] : null, on);
                }

                foreach (SubPanel panel in Panels.Values)
                {
                    for (int i = 0; i < panel.Cells.Count; i++)
                    {
                        SubCell cell = panel.Cells[i];
                        bool on = ActiveSubs.Contains(cell.Key);
                        if (cell.Boxed)
                        {
                            if (cell.Background != null)
                                cell.Background.color = on ? PngOn : PngOff;
                            if (cell.Selection != null)
                                cell.Selection.enabled = on;
                        }
                        else
                        {
                            Paint(cell.Background, null, on);
                            if (cell.Icon != null)
                                cell.Icon.color = on ? IconOn : IconOff;
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                // Stale chip refs after InventoryGui rebuild - drop and rebuild on next Show.
                ClearUiLists();
            }
        }

        private static void Paint(Image bg, TMP_Text label, bool on)
        {
            // Tint only - keep vanilla wood/iron sprite readable.
            if (bg != null)
                bg.color = on ? Color.white : Dim;

            if (label != null)
            {
                label.color = on ? ChipGoldActive : ChipGold;
                label.canvasRenderer.SetColor(Color.white);
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
