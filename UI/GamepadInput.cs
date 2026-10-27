using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Gamepad presses for our crafting-panel controls. Vanilla drives its own crafting buttons through
    /// UIGamePad hotkeys (X craft, LT/RT tabs, right-stick click repair, D-pad/left stick up/down recipes);
    /// we only read buttons no vanilla crafting control uses: right stick tilt and D-pad left/right. ZInput
    /// repeats those while held (GetButtonDown again after 0.3 s, then every 0.1 s).
    /// </summary>
    internal static class GamepadInput
    {
        /// <summary>
        /// True while the crafting panel has gamepad focus (LB/RB put it there), with nothing on top:
        /// the same conditions a vanilla UIGamePad hotkey checks.
        /// </summary>
        public static bool CraftingFocused(InventoryGui gui)
        {
            if (gui == null || !InventoryGui.IsVisible() || ZInput.IsGamepadMouseActive()
                || Console.IsVisible() || SearchBox.IsFocused)
                return false;
            RectTransform crafting = AccessToolsExt.CraftingPanel(gui);
            UIGroupHandler group = crafting != null ? crafting.GetComponent<UIGroupHandler>() : null;
            return group != null && group.IsActive;
        }

        /// <summary>Where a vanilla UIGamePad shows its hint: crafting panel focused, gamepad in use, no mouse.</summary>
        public static bool HintsShown(InventoryGui gui)
        {
            return CraftingFocused(gui) && ZInput.IsGamepadActive() && !ZInput.IsMouseActive();
        }

        /// <summary>
        /// Rich-text glyph of the controller sheet in use (xbox, ps5, switch2), e.g. "rstick_updown". Vanilla
        /// keys name one direction each ("$KEY_RStickUp" is &lt;sprite="xbox" name="rstick_up"&gt;), so take
        /// the sheet from that and swap the sprite. Gamepad only: without one the key does not resolve.
        /// </summary>
        public static string Glyph(string sprite)
        {
            string probe = Localization.instance != null ? Localization.instance.Localize("$KEY_RStickUp") : "";
            int at = probe.IndexOf(" name=\"", System.StringComparison.Ordinal);
            if (!probe.StartsWith("<sprite=", System.StringComparison.Ordinal) || at < 0)
                return "";
            string sheet = probe.Substring(0, at);
            // The Switch sheet lacks the combined left/right sprites.
            if (sheet.Contains("switch2"))
            {
                if (sprite == "rstick_leftright")
                    sprite = "rstick";
                else if (sprite == "dpad_leftright")
                    sprite = "dpad_left_and_right";
            }
            return sheet + " name=\"" + sprite + "\">";
        }

        /// <summary>
        /// Show a glyph badge like vanilla's button hints: a copy of the Craft tab's "gamepad_hint", made on
        /// first use (and again if the parent got destroyed), centered on <paramref name="pos"/> from the
        /// <paramref name="anchor"/> point of <paramref name="parent"/>, moved by "&lt;key&gt;.dx/dy" in the layout file.
        /// </summary>
        public static void ShowHint(ref GameObject hint, InventoryGui gui, Transform parent, Vector2 anchor,
            Vector2 pos, string key, string sprite)
        {
            string glyph = Glyph(sprite);
            if (parent == null || glyph.Length == 0)
            {
                HideHint(hint);
                return;
            }
            if (hint == null)
            {
                Button tab = AccessToolsExt.TabCraft(gui);
                Transform src = tab != null ? tab.transform.Find("gamepad_hint") : null;
                if (src == null)
                    return;
                hint = Object.Instantiate(src.gameObject, parent, false);
                hint.name = "WBP_PadHint";
                Graphic[] graphics = hint.GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < graphics.Length; i++)
                    graphics[i].raycastTarget = false;
                // The tab's glyph sits a little off its badge's center (pos -1, 1.7); ours are placed by their center.
                TMP_Text glyphText = hint.GetComponentInChildren<TMP_Text>(true);
                if (glyphText != null)
                    glyphText.rectTransform.anchoredPosition = Vector2.zero;
                // Never part of the chip column's layout.
                LayoutElement le = hint.GetComponent<LayoutElement>();
                if (le == null)
                    le = hint.AddComponent<LayoutElement>();
                le.ignoreLayout = true;
            }

            RectTransform rt = (RectTransform)hint.transform;
            if (rt.parent != parent)
                rt.SetParent(parent, false);
            if (rt.GetSiblingIndex() != parent.childCount - 1)
                rt.SetAsLastSibling();
            if (rt.anchorMin != anchor || rt.anchorMax != anchor)
            {
                rt.anchorMin = anchor;
                rt.anchorMax = anchor;
            }
            pos += new Vector2(LayoutOverrides.Get(key + ".dx", 0f), LayoutOverrides.Get(key + ".dy", 0f));
            if (rt.anchoredPosition != pos)
                rt.anchoredPosition = pos;

            TMP_Text text = hint.GetComponentInChildren<TMP_Text>(true);
            if (text != null && text.text != glyph)
                text.text = glyph;
            if (!hint.activeSelf)
                hint.SetActive(true);
        }

        public static void HideHint(GameObject hint)
        {
            if (hint != null && hint.activeSelf)
                hint.SetActive(false);
        }
    }
}
