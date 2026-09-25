using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Third tab beside Craft / Upgrade - enters dismantle mode.
    /// Vanilla selects a tab by setting interactable=false (disabled/orange look).
    /// </summary>
    internal static class DismantleTab
    {
        private const string RootName = "WBP_TabDismantle";
        private const string TabLabel = "DISMANTLE";

        private static GameObject _tab;
        private static Button _button;
        private static TMP_Text _label;
        private static Color _labelSelected = new Color(1f, 0.92f, 0.55f, 1f);
        private static Color _labelNormal = new Color(0.82f, 0.76f, 0.58f, 1f);
        private static bool _labelColorsReady;

        public static void Show(InventoryGui gui)
        {
            if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value
                || !Plugin.Settings.EnableDismantle.Value)
            {
                Hide();
                return;
            }

            if (gui == null)
                return;

            EnsureTab(gui);
            if (_tab == null)
                return;

            bool atStation = Player.m_localPlayer != null
                && Player.m_localPlayer.GetCurrentCraftingStation() != null;
            CraftingStation station = Player.m_localPlayer != null
                ? Player.m_localPlayer.GetCurrentCraftingStation()
                : null;
            bool show = atStation && !StationFilter.IsUpgrader(station);
            _tab.SetActive(show);
            if (show)
            {
                EnsureLabelFit(gui);
                EnsurePlacement(gui);
                EnsureLayer(gui);
                RefreshVisuals(gui);
            }
        }

        private static void EnsureLabelFit(InventoryGui gui)
        {
            if (_label == null)
                return;
            Button craft = AccessToolsExt.TabCraft(gui);
            TMP_Text sample = craft != null ? craft.GetComponentInChildren<TMP_Text>(true) : null;
            if (sample == null)
            {
                Button upgrade = AccessToolsExt.TabUpgrade(gui);
                sample = upgrade != null ? upgrade.GetComponentInChildren<TMP_Text>(true) : null;
            }
            FitTabLabel(_label, sample);
        }

        private static void EnsurePlacement(InventoryGui gui)
        {
            Button upgrade = AccessToolsExt.TabUpgrade(gui);
            Button craft = AccessToolsExt.TabCraft(gui);
            if (upgrade == null)
                return;
            PlaceBeside(upgrade.transform as RectTransform,
                craft != null ? craft.transform as RectTransform : null);
        }

        private static void EnsureLayer(InventoryGui gui)
        {
            Button upgrade = AccessToolsExt.TabUpgrade(gui);
            if (upgrade != null)
                MatchTabLayer(upgrade.transform as RectTransform);
        }

        public static void Hide()
        {
            if (_tab != null)
                _tab.SetActive(false);
            DismantleMode.SetActive(false);
        }

        public static void Destroy()
        {
            if (_tab != null)
                Object.Destroy(_tab);
            _tab = null;
            _button = null;
            _label = null;
            _labelColorsReady = false;
            DismantleMode.SetActive(false);
        }

        public static void RefreshVisuals(InventoryGui gui)
        {
            if (_button == null)
                return;

            CaptureLabelColors(gui);
            bool on = DismantleMode.Active;

            Button craft = AccessToolsExt.TabCraft(gui);
            Button upgrade = AccessToolsExt.TabUpgrade(gui);

            if (on)
            {
                // Neither Craft nor Upgrade selected - both must stay clickable.
                if (craft != null)
                    craft.interactable = true;
                if (upgrade != null)
                    upgrade.interactable = true;
                // Selected look = interactable false (same as vanilla Craft/Upgrade).
                _button.interactable = false;
            }
            else
            {
                _button.interactable = true;
            }

            if (_label != null)
            {
                _label.text = TabLabel;
                _label.color = on ? _labelSelected : _labelNormal;
            }

            // Match Craft/Upgrade label colors to their real selection state.
            ApplyVanillaTabLabel(craft);
            ApplyVanillaTabLabel(upgrade);
        }

        private static void ApplyVanillaTabLabel(Button tab)
        {
            if (tab == null)
                return;
            TMP_Text label = tab.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return;
            // Selected tab is non-interactable in vanilla.
            label.color = tab.interactable ? _labelNormal : _labelSelected;
        }

        private static void CaptureLabelColors(InventoryGui gui)
        {
            if (_labelColorsReady)
                return;

            Button craft = AccessToolsExt.TabCraft(gui);
            Button upgrade = AccessToolsExt.TabUpgrade(gui);
            TMP_Text craftLabel = craft != null ? craft.GetComponentInChildren<TMP_Text>(true) : null;
            TMP_Text upgradeLabel = upgrade != null ? upgrade.GetComponentInChildren<TMP_Text>(true) : null;

            // Selected = the non-interactable tab; normal = the interactable one.
            if (craft != null && craftLabel != null && !craft.interactable)
                _labelSelected = craftLabel.color;
            else if (upgrade != null && upgradeLabel != null && !upgrade.interactable)
                _labelSelected = upgradeLabel.color;

            if (craft != null && craftLabel != null && craft.interactable)
                _labelNormal = craftLabel.color;
            else if (upgrade != null && upgradeLabel != null && upgrade.interactable)
                _labelNormal = upgradeLabel.color;

            _labelColorsReady = true;
        }

        private static void EnsureTab(InventoryGui gui)
        {
            if (_tab != null)
                return;

            Button upgrade = AccessToolsExt.TabUpgrade(gui);
            Button craft = AccessToolsExt.TabCraft(gui);
            Button template = upgrade != null ? upgrade : craft;
            if (template == null)
                return;

            RectTransform templateRt = template.transform as RectTransform;
            Transform parent = templateRt != null ? templateRt.parent : null;
            if (parent == null)
                return;

            _tab = Object.Instantiate(template.gameObject, parent, false);
            _tab.name = RootName;
            StripListeners(_tab);

            _button = _tab.GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClicked);
                _button.interactable = true;
            }

            _label = _tab.GetComponentInChildren<TMP_Text>(true);
            if (_label != null)
            {
                TMP_Text sample = null;
                if (craft != null)
                    sample = craft.GetComponentInChildren<TMP_Text>(true);
                if (sample == null && upgrade != null)
                    sample = upgrade.GetComponentInChildren<TMP_Text>(true);

                FitTabLabel(_label, sample);
            }

            PlaceBeside(templateRt, craft != null ? craft.transform as RectTransform : null);
            // Same draw order as Craft/Upgrade: under the item panel, not on top of it.
            MatchTabLayer(templateRt);
            CaptureLabelColors(gui);
        }

        private static void FitTabLabel(TMP_Text label, TMP_Text sample)
        {
            if (label == null)
                return;

            label.text = TabLabel;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.Center;

            if (sample != null)
            {
                label.font = sample.font;
                label.fontStyle = sample.fontStyle;
                label.fontSize = sample.fontSize;
                label.characterSpacing = sample.characterSpacing;
                label.margin = sample.margin;
            }
            else
            {
                label.fontStyle = FontStyles.Normal;
            }

            // "DISMANTLE" is longer than CRAFT/UPGRADE - shrink slightly so it stays inside the box.
            float baseSize = label.fontSize;
            if (baseSize > 1f)
                label.fontSize = Mathf.Max(10f, baseSize * 0.82f);

            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(8f, label.fontSize * 0.75f);
            label.fontSizeMax = label.fontSize;
        }

        private static void MatchTabLayer(RectTransform upgradeRt)
        {
            if (_tab == null || upgradeRt == null)
                return;
            // Insert directly after Upgrade so we share its layer (under the recipe panel).
            _tab.transform.SetSiblingIndex(upgradeRt.GetSiblingIndex() + 1);
        }

        private static void PlaceBeside(RectTransform upgradeRt, RectTransform craftRt)
        {
            RectTransform rt = _tab.transform as RectTransform;
            if (rt == null || upgradeRt == null)
                return;

            rt.anchorMin = upgradeRt.anchorMin;
            rt.anchorMax = upgradeRt.anchorMax;
            rt.pivot = upgradeRt.pivot;
            rt.sizeDelta = upgradeRt.sizeDelta;
            rt.localScale = upgradeRt.localScale;

            // Same center-to-center step as Craft → Upgrade, so the visual gap matches.
            if (craftRt != null)
            {
                float step = upgradeRt.anchoredPosition.x - craftRt.anchoredPosition.x;
                if (Mathf.Abs(step) < 1f)
                {
                    float w = upgradeRt.rect.width;
                    if (w < 8f)
                        w = Mathf.Abs(upgradeRt.sizeDelta.x);
                    if (w < 8f)
                        w = 70f;
                    step = w + 4f;
                }

                rt.anchoredPosition = new Vector2(
                    upgradeRt.anchoredPosition.x + step,
                    upgradeRt.anchoredPosition.y);
                return;
            }

            float width = upgradeRt.rect.width;
            if (width < 8f)
                width = Mathf.Abs(upgradeRt.sizeDelta.x);
            if (width < 8f)
                width = 70f;

            rt.anchoredPosition = new Vector2(
                upgradeRt.anchoredPosition.x + width + 4f,
                upgradeRt.anchoredPosition.y);
        }

        private static void OnClicked()
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
                return;
            DismantleMode.EnterFromUi(gui);
        }

        private static void StripListeners(GameObject go)
        {
            Button btn = go.GetComponent<Button>();
            if (btn != null)
                btn.onClick.RemoveAllListeners();

            var triggers = go.GetComponents<UnityEngine.EventSystems.EventTrigger>();
            for (int i = 0; i < triggers.Length; i++)
                Object.Destroy(triggers[i]);
        }
    }
}
