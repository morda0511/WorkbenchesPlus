using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Non-clickable separator rows in the recipe list, e.g. left-aligned IRON / Arrows.
    /// </summary>
    internal static class MaterialSectionHeaders
    {
        private const string RootName = "WBP_MaterialHeaders";
        private const string HeaderPrefix = "WBP_MatHeader_";

        private static GameObject _root;
        private static readonly List<GameObject> Headers = new List<GameObject>();

        public static void Clear()
        {
            for (int i = 0; i < Headers.Count; i++)
            {
                if (Headers[i] != null)
                    Object.Destroy(Headers[i]);
            }
            Headers.Clear();

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
        }

        /// <summary>
        /// Insert a dashed header before each new material group. Shifts recipe Y positions.
        /// Returns the final list height in rows (recipes + headers) for scroll sizing callers.
        /// </summary>
        public static int Apply(
            Transform listParent,
            IList<Recipe> recipesInOrder,
            IList<GameObject> recipeElements,
            float rowSpace)
        {
            Clear();

            if (listParent == null || recipesInOrder == null || recipeElements == null)
                return recipesInOrder != null ? recipesInOrder.Count : 0;
            if (Plugin.Settings == null || !Plugin.Settings.EnableMaterialSectionHeaders.Value)
            {
                for (int i = 0; i < recipeElements.Count; i++)
                {
                    GameObject go = recipeElements[i];
                    if (go == null)
                        continue;
                    RectTransform rt = go.transform as RectTransform;
                    if (rt != null)
                        rt.anchoredPosition = new Vector2(0f, -i * rowSpace);
                }
                return recipeElements.Count;
            }

            _root = new GameObject(RootName, typeof(RectTransform));
            _root.transform.SetParent(listParent, false);
            RectTransform rootRt = _root.transform as RectTransform;
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(1f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.anchoredPosition = Vector2.zero;
            rootRt.sizeDelta = Vector2.zero;

            TMP_FontAsset font = null;
            GameObject sampleRow = null;
            for (int i = 0; i < recipeElements.Count; i++)
            {
                if (recipeElements[i] != null)
                {
                    sampleRow = recipeElements[i];
                    break;
                }
            }
            if (sampleRow != null)
            {
                TMP_Text sample = sampleRow.GetComponentInChildren<TMP_Text>(true);
                if (sample != null)
                    font = sample.font;
            }

            string lastSectionKey = null;
            int slot = 0;
            Player player = Player.m_localPlayer;
            bool craftSplit = Plugin.Settings != null && Plugin.Settings.UseCraftBuckets();

            for (int i = 0; i < recipesInOrder.Count; i++)
            {
                Recipe r = recipesInOrder[i];
                string headerKey = MaterialNameBucket.Resolve(r);

                // Section = material + craft tier, so craftable Bronze gets its own
                // "Bronze" label at the top and uncraftable Bronze can show "Bronze" again.
                int craftTier = 0;
                if (craftSplit && player != null && r != null)
                    craftTier = Craftability.ScoreBucket(player, r);

                string sectionKey = string.IsNullOrEmpty(headerKey)
                    ? null
                    : headerKey + "|" + craftTier;

                if (!string.IsNullOrEmpty(sectionKey)
                    && !string.Equals(sectionKey, lastSectionKey, System.StringComparison.OrdinalIgnoreCase))
                {
                    string label = MaterialNameBucket.Label(headerKey);
                    GameObject header = MakeHeader(label, font, sampleRow);
                    header.transform.SetParent(_root.transform, false);
                    RectTransform hrt = header.transform as RectTransform;
                    hrt.anchoredPosition = new Vector2(0f, -slot * rowSpace);
                    Headers.Add(header);
                    lastSectionKey = sectionKey;
                    slot++;
                }
                else if (!string.IsNullOrEmpty(sectionKey))
                {
                    lastSectionKey = sectionKey;
                }

                GameObject row = i < recipeElements.Count ? recipeElements[i] : null;
                if (row != null)
                {
                    RectTransform rt = row.transform as RectTransform;
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(0f, -slot * rowSpace);
                        rt.SetSiblingIndex(slot);
                    }
                }
                slot++;
            }

            // Grow scroll content so extra header rows are not clipped.
            RectTransform parentRt = listParent as RectTransform;
            if (parentRt != null && slot > 0)
            {
                Vector2 sd = parentRt.sizeDelta;
                float need = slot * rowSpace + 4f;
                if (sd.y < need)
                    parentRt.sizeDelta = new Vector2(sd.x, need);
            }

            return slot;
        }

        private static GameObject MakeHeader(string text, TMP_FontAsset font, GameObject sampleRow)
        {
            GameObject go;
            float height = 28f;
            float width = 200f;

            if (sampleRow != null)
            {
                RectTransform sampleRt = sampleRow.transform as RectTransform;
                if (sampleRt != null)
                {
                    if (sampleRt.rect.width > 8f)
                        width = sampleRt.rect.width;
                    else if (Mathf.Abs(sampleRt.sizeDelta.x) > 8f)
                        width = Mathf.Abs(sampleRt.sizeDelta.x);
                    if (sampleRt.rect.height > 4f)
                        height = sampleRt.rect.height;
                }

                // Soft empty panel: clone row chrome, strip interaction + icons.
                go = Object.Instantiate(sampleRow);
                go.name = HeaderPrefix + text;
                StripRowInteraction(go);
                HideChildGraphics(go);
            }
            else
            {
                go = new GameObject(HeaderPrefix + text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Image bg = go.GetComponent<Image>();
                bg.color = new Color(0.08f, 0.07f, 0.06f, 0.55f);
                bg.raycastTarget = false;
            }

            RectTransform rt = go.transform as RectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.sizeDelta = new Vector2(0f, height);

            TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(go.transform, false);
                RectTransform lrt = labelGo.transform as RectTransform;
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
                label = labelGo.GetComponent<TextMeshProUGUI>();
            }

            label.text = text;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 12f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.85f, 0.75f, 0.45f, 1f);
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 13f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            if (font != null)
                label.font = font;

            RectTransform labelRt = label.transform as RectTransform;
            if (labelRt != null)
            {
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = new Vector2(10f, 0f);
                labelRt.offsetMax = new Vector2(-6f, 0f);
            }

            // Ensure only one visible label shows our dashed text.
            TMP_Text[] all = go.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i] == label)
                    continue;
                all[i].text = "";
                all[i].enabled = false;
            }

            return go;
        }

        private static void StripRowInteraction(GameObject go)
        {
            Button[] buttons = go.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                    continue;
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].interactable = false;
                buttons[i].enabled = false;
            }

            var triggers = go.GetComponentsInChildren<UnityEngine.EventSystems.EventTrigger>(true);
            for (int i = 0; i < triggers.Length; i++)
                Object.Destroy(triggers[i]);

            Toggle[] toggles = go.GetComponentsInChildren<Toggle>(true);
            for (int i = 0; i < toggles.Length; i++)
            {
                if (toggles[i] != null)
                    toggles[i].enabled = false;
            }
        }

        private static void HideChildGraphics(GameObject go)
        {
            // Dim root image; hide icon-like children so the row reads as an empty separator.
            Image rootImg = go.GetComponent<Image>();
            if (rootImg != null)
            {
                Color c = rootImg.color;
                c.a = Mathf.Clamp01(c.a * 0.55f);
                rootImg.color = c;
                rootImg.raycastTarget = false;
            }

            Image[] images = go.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image img = images[i];
                if (img == null || img == rootImg)
                    continue;
                img.enabled = false;
                img.raycastTarget = false;
            }
        }
    }
}
