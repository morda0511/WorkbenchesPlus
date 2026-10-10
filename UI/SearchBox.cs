using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Search field at the right end of the Craft / Upgrade / Dismantle tab row.
    /// Filters the recipe list by name or header (RecipeSort.Apply reads <see cref="Query"/>).
    /// </summary>
    internal static class SearchBox
    {
        private const string RootName = "WBP_Search";
        private const float ApplyDelay = 0.25f;

        private static GameObject _root;
        private static TMP_InputField _input;
        private static bool _silent;
        private static bool _pending;
        private static float _applyAt;

        public static string Query { get; private set; } = "";

        public static bool IsFocused
        {
            get { return _input != null && _input.isFocused; }
        }

        public static void Show(InventoryGui gui)
        {
            if (gui == null || !Allowed())
            {
                Hide(false);
                return;
            }

            EnsureBuilt(gui);
            if (_root == null)
                return;
            Place(gui);
            _root.SetActive(true);
        }

        public static void Hide(bool clearQuery)
        {
            if (_root != null)
                _root.SetActive(false);
            if (clearQuery)
                Reset();
        }

        /// <summary>Forget the search text without triggering a rebuild.</summary>
        public static void Reset()
        {
            Query = "";
            _pending = false;
            if (_input != null)
            {
                _silent = true;
                _input.text = "";
                _silent = false;
            }
        }

        public static void Tick()
        {
            if (!_pending || Time.unscaledTime < _applyAt || AccessToolsExt.IsRebuilding)
                return;

            _pending = false;
            bool focused = IsFocused;
            AccessToolsExt.RebuildCraftingPanel();
            if (focused && _input != null && !_input.isFocused)
            {
                _input.ActivateInputField();
                _input.MoveTextEnd(false);
            }
        }

        public static bool Matches(RecipeInfo info)
        {
            if (Query.Length == 0)
                return true;
            if (info == null)
                return false;
            return info.SortName.IndexOf(Query, System.StringComparison.OrdinalIgnoreCase) >= 0
                || info.Group.IndexOf(Query, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool Allowed()
        {
            if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value)
                return false;
            Player player = Player.m_localPlayer;
            CraftingStation station = player != null ? player.GetCurrentCraftingStation() : null;
            return station != null && !StationFilter.IsUpgrader(station) && !DismantleMode.Active;
        }

        private static void OnChanged(string value)
        {
            if (_silent)
                return;
            Query = (value ?? "").Trim();
            _pending = true;
            _applyAt = Time.unscaledTime + ApplyDelay;
        }

        private static void EnsureBuilt(InventoryGui gui)
        {
            if (_root != null)
                return;

            Button upgrade = AccessToolsExt.TabUpgrade(gui);
            Button template = upgrade != null ? upgrade : AccessToolsExt.TabCraft(gui);
            RectTransform templateRt = template != null ? template.transform as RectTransform : null;
            if (templateRt == null || templateRt.parent == null)
                return;

            TMP_Text sample = template.GetComponentInChildren<TMP_Text>(true);
            TMP_FontAsset font = sample != null ? sample.font : null;

            _root = new GameObject(RootName, typeof(RectTransform), typeof(Image));
            _root.SetActive(false); // wire everything up before the input field wakes up
            _root.transform.SetParent(templateRt.parent, false);

            Image bg = _root.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.07f, 0.06f, 0.9f);

            var area = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(_root.transform, false);
            Stretch(area.transform as RectTransform, 8f, 2f);

            TextMeshProUGUI text = MakeText("Text", area.transform, font, GroupColors.Hex(MaterialSectionHeaders.HeaderGold));
            TextMeshProUGUI hint = MakeText("Placeholder", area.transform, font, "8c8068");
            hint.text = Loc.T("search");
            hint.fontStyle = FontStyles.Italic;

            _input = _root.AddComponent<TMP_InputField>();
            _input.targetGraphic = bg;
            _input.textViewport = area.transform as RectTransform;
            _input.textComponent = text;
            _input.placeholder = hint;
            _input.fontAsset = font;
            _input.pointSize = text.fontSize;
            _input.characterLimit = 24;
            _input.lineType = TMP_InputField.LineType.SingleLine;
            _input.richText = false;
            _input.customCaretColor = true;
            _input.caretColor = MaterialSectionHeaders.HeaderGold;
            _input.onValueChanged.AddListener(OnChanged);
        }

        private static TextMeshProUGUI MakeText(string name, Transform parent, TMP_FontAsset font, string hexColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Stretch(go.transform as RectTransform, 0f, 0f);

            TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
            Color c;
            ColorUtility.TryParseHtmlString("#" + hexColor, out c);
            t.color = c;
            t.fontSize = 15f;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            if (font != null)
                t.font = font;
            return t;
        }

        private static void Stretch(RectTransform rt, float x, float y)
        {
            if (rt == null)
                return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(x, y);
            rt.offsetMax = new Vector2(-x, -y);
        }

        /// <summary>Right of the Dismantle tab, same row, same height as the tabs.</summary>
        private static void Place(InventoryGui gui)
        {
            Button upgrade = AccessToolsExt.TabUpgrade(gui);
            Button craft = AccessToolsExt.TabCraft(gui);
            RectTransform u = upgrade != null ? upgrade.transform as RectTransform : null;
            RectTransform c = craft != null ? craft.transform as RectTransform : null;
            RectTransform rt = _root.transform as RectTransform;
            if (u == null || rt == null)
                return;

            float w = u.rect.width;
            if (w < 8f)
                w = Mathf.Abs(u.sizeDelta.x);
            if (w < 8f)
                w = 70f;
            float h = u.rect.height;
            if (h < 8f)
                h = Mathf.Abs(u.sizeDelta.y);
            if (h < 8f)
                h = 28f;

            float step = c != null ? u.anchoredPosition.x - c.anchoredPosition.x : w + 4f;
            if (Mathf.Abs(step) < 1f)
                step = w + 4f;

            float dismantleRight = u.anchoredPosition.x + step + (1f - u.pivot.x) * w;
            rt.anchorMin = u.anchorMin;
            rt.anchorMax = u.anchorMax;
            rt.pivot = new Vector2(0f, u.pivot.y);
            rt.localScale = Vector3.one;
            float sw = LayoutOverrides.Get("search.w", 0f), sh = LayoutOverrides.Get("search.h", 0f);
            rt.sizeDelta = new Vector2(sw > 20f ? sw : w * 1.7f, sh > 8f ? sh : h);
            rt.anchoredPosition = new Vector2(dismantleRight + 8f, u.anchoredPosition.y);
            LayoutOverrides.Nudge(rt, "search");
        }
    }

    /// <summary>
    /// While the search field has focus the game must treat it like the chat box: otherwise typing
    /// "e" (Use) or Tab (Inventory) would close the window.
    /// </summary>
    [HarmonyPatch(typeof(Chat), nameof(Chat.HasFocus))]
    internal static class SearchChatFocusPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (!__result && SearchBox.IsFocused)
                __result = true;
        }
    }
}
