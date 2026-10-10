using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Non-clickable separator rows in the recipe list, e.g. left-aligned IRON / Arrows.
    /// </summary>
    /// <summary>One list row in display order: which header it sits under (null = none).</summary>
    internal struct HeaderSlot
    {
        public string Key;
        public string Label;
        public Color Color;
    }

    internal static class MaterialSectionHeaders
    {
        private const string RootName = "WBP_MaterialHeaders";
        private const string HeaderPrefix = "WBP_MatHeader_";

        // Valheim's gold (same family as the recipe title / selected tab).
        public static readonly Color HeaderGold = new Color(1f, 0.78f, 0.3f, 1f);

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
            IList<HeaderSlot> slotsInOrder,
            IList<GameObject> recipeElements,
            float rowSpace)
        {
            Clear();

            if (listParent == null || slotsInOrder == null || recipeElements == null)
                return slotsInOrder != null ? slotsInOrder.Count : 0;
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

            for (int i = 0; i < slotsInOrder.Count; i++)
            {
                string sectionKey = slotsInOrder[i].Key;

                if (!string.IsNullOrEmpty(sectionKey)
                    && !string.Equals(sectionKey, lastSectionKey, System.StringComparison.OrdinalIgnoreCase))
                {
                    GameObject header = MakeHeader(slotsInOrder[i].Label, slotsInOrder[i].Color, font, sampleRow);
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
                // Vanilla sizes the list with SetSizeWithCurrentAnchors (rows only, no headers). Use the
                // real height: sizeDelta.y is only an offset when the list is stretched vertically.
                float need = slot * rowSpace;
                if (parentRt.rect.height < need)
                    parentRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, need);
            }

            return slot;
        }

        private static GameObject MakeHeader(string text, Color color, TMP_FontAsset font, GameObject sampleRow)
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
            label.color = color;
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

            // The cloned row was instantiated while its Button was in a tinted state: reset it.
            Graphic[] graphics = go.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                    graphics[i].canvasRenderer.SetColor(Color.white);
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
                // No colour-tint state change: a disabled tint would stay on the clone's text.
                buttons[i].transition = Selectable.Transition.None;
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
