using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Up/down arrows beside Craft - drives vanilla multi-craft (materials + "Craft xN").
    /// </summary>
    internal static class CraftMultiplierBar
    {
        private const float GapRightOfCraft = 4f;
        private const float ArrowStackSpacing = 2f;
        private const float ExtraCraftInset = 4f;
        private const float ArrowScale = 0.5f;
        private const int VanillaMultiDefault = 5;

        private static readonly FieldInfo MultiCraftAmountField =
            AccessTools.Field(typeof(InventoryGui), "m_multiCraftAmount");
        private static readonly FieldInfo TouchMultiCraftingField =
            AccessTools.Field(typeof(InventoryGui), "m_touchMultiCrafting");
        private static readonly FieldInfo SelectedRecipeField =
            AccessTools.Field(typeof(InventoryGui), "m_selectedRecipe");
        private static readonly FieldInfo CraftRecipeField =
            AccessTools.Field(typeof(InventoryGui), "m_craftRecipe");
        private static readonly FieldInfo CraftUpgradeItemField =
            AccessTools.Field(typeof(InventoryGui), "m_craftUpgradeItem");

        private static GameObject _root;
        private static Button _up;
        private static Button _down;
        private static RectTransform _craftRt;
        private static Vector2 _origSizeDelta;
        private static Vector2 _origOffsetMin;
        private static Vector2 _origOffsetMax;
        private static Vector2 _origAnchoredPos;
        private static bool _craftCaptured;

        private static int _amount = 1;
        private static Recipe _trackedRecipe;

        public static int Amount => _amount;

        public static void Show(InventoryGui gui)
        {
            if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value
                || !Plugin.Settings.EnableCraftMultiplier.Value
                || DismantleMode.Active)
            {
                ForceHideArrows();
                return;
            }

            if (gui == null)
                return;

            RectTransform craft = AccessToolsExt.CraftButton(gui);
            if (craft == null)
                return;

            RectTransform host = craft.parent as RectTransform;
            if (host == null)
                return;

            if (_root != null && _root.transform.parent != host)
                DestroyUi();

            if (_root == null)
                Build(gui, host);

            if (_root == null)
                return;

            ShrinkCraftForArrows(craft);
            PlaceBesideCraft(craft);
            RefreshArrowStates(gui);
            _root.SetActive(true);
        }

        public static void Hide()
        {
            ForceHideArrows();
            _amount = 1;
            _trackedRecipe = null;
        }

        /// <summary>Hide arrows and restore Craft width without resetting multiplier amount.</summary>
        public static void ForceHideArrows()
        {
            RestoreCraftSize();
            if (_root != null)
                _root.SetActive(false);
        }

        public static void Destroy()
        {
            ForceHideArrows();
            DestroyUi();
            _amount = 1;
            _trackedRecipe = null;
        }

        /// <summary>
        /// Call from UpdateRecipe Prefix so vanilla requirement list + Craft label use our amount.
        /// </summary>
        public static void SyncBeforeUpdateRecipe(InventoryGui gui)
        {
            if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value
                || !Plugin.Settings.EnableCraftMultiplier.Value
                || DismantleMode.Active)
                return;
            if (gui == null || MultiCraftAmountField == null || TouchMultiCraftingField == null)
                return;

            Recipe selected = GetSelectedRecipe(gui);
            if (selected != _trackedRecipe)
            {
                _trackedRecipe = selected;
                _amount = 1;
            }

            ClampToAffordable(gui);
            ApplyVanillaFields(gui);
        }

        /// <summary>Call from UpdateRecipe Postfix - keep arrow interactable in sync.</summary>
        public static void SyncAfterUpdateRecipe(InventoryGui gui)
        {
            if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value
                || !Plugin.Settings.EnableCraftMultiplier.Value)
                return;
            if (gui == null)
                return;

            RefreshArrowStates(gui);
        }

        private static void ApplyVanillaFields(InventoryGui gui)
        {
            if (_amount > 1)
            {
                MultiCraftAmountField.SetValue(gui, _amount);
                TouchMultiCraftingField.SetValue(gui, true);
            }
            else
            {
                // Keep vanilla L2 / Shift multi-craft at its default 5× when UI is at 1×.
                MultiCraftAmountField.SetValue(gui, VanillaMultiDefault);
                TouchMultiCraftingField.SetValue(gui, false);
            }
        }

        private static void ClampToAffordable(InventoryGui gui)
        {
            int max = MaxAllowed();
            if (_amount > max)
                _amount = max;
            while (_amount > 1 && !CanAfford(gui, _amount))
                _amount--;
            if (_amount < 1)
                _amount = 1;
        }

        private static int MaxAllowed()
        {
            int m = 99;
            if (Plugin.Settings != null && Plugin.Settings.MaxCraftMultiplier != null)
                m = Plugin.Settings.MaxCraftMultiplier.Value;
            if (m < 1)
                m = 1;
            return m;
        }

        private static void RefreshArrowStates(InventoryGui gui)
        {
            if (_down != null)
                _down.interactable = _amount > 1;

            if (_up != null)
            {
                bool canUp = _amount < MaxAllowed() && CanAfford(gui, _amount + 1);
                _up.interactable = canUp;
            }
        }

        private static bool CanAfford(InventoryGui gui, int amount)
        {
            if (amount <= 1)
                return true;

            Player player = Player.m_localPlayer;
            if (player == null)
                return false;

            Recipe recipe = GetSelectedRecipe(gui);
            if (recipe == null)
                recipe = CraftRecipeField != null ? CraftRecipeField.GetValue(gui) as Recipe : null;
            if (recipe == null)
                return false;

            int quality = 1;
            if (CraftUpgradeItemField != null)
            {
                ItemDrop.ItemData upgrade = CraftUpgradeItemField.GetValue(gui) as ItemDrop.ItemData;
                if (upgrade != null)
                    quality = upgrade.m_quality;
            }

            try
            {
                return player.HaveRequirements(recipe, false, quality, amount);
            }
            catch
            {
                return false;
            }
        }

        private static Recipe GetSelectedRecipe(InventoryGui gui)
        {
            if (SelectedRecipeField == null || gui == null)
                return null;
            try
            {
                object pair = SelectedRecipeField.GetValue(gui);
                if (pair == null)
                    return null;
                PropertyInfo prop = AccessTools.Property(pair.GetType(), "Recipe");
                if (prop != null)
                    return prop.GetValue(pair, null) as Recipe;
            }
            catch
            {
            }
            return null;
        }

        private static void DestroyUi()
        {
            if (_root != null)
                Object.Destroy(_root);
            _root = null;
            _up = null;
            _down = null;
        }

        private static void Build(InventoryGui gui, RectTransform host)
        {
            Button upSrc = AccessToolsExt.QualityLevelUp(gui);
            Button downSrc = AccessToolsExt.QualityLevelDown(gui);
            if (upSrc == null || downSrc == null)
            {
                if (Plugin.Settings != null && Plugin.Settings.DebugLogging.Value)
                    Plugin.Log.LogWarning("CraftMultiplierBar: quality arrows missing - cannot clone look.");
                return;
            }

            _root = new GameObject("WBP_CraftMultiplier", typeof(RectTransform));
            _root.transform.SetParent(host, false);

            RectTransform rootRt = _root.transform as RectTransform;
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0f, 0.5f);

            GameObject upGo = Object.Instantiate(upSrc.gameObject, _root.transform, false);
            upGo.name = "WBP_MultUp";
            upGo.SetActive(true);
            StripListeners(upGo);
            _up = upGo.GetComponent<Button>();
            if (_up != null)
                _up.onClick.AddListener(OnUpClicked);

            GameObject downGo = Object.Instantiate(downSrc.gameObject, _root.transform, false);
            downGo.name = "WBP_MultDown";
            downGo.SetActive(true);
            StripListeners(downGo);
            _down = downGo.GetComponent<Button>();
            if (_down != null)
                _down.onClick.AddListener(OnDownClicked);

            RectTransform upRt = upGo.transform as RectTransform;
            RectTransform downRt = downGo.transform as RectTransform;
            if (upRt != null)
            {
                upRt.anchorMin = new Vector2(0.5f, 0.5f);
                upRt.anchorMax = new Vector2(0.5f, 0.5f);
                upRt.pivot = new Vector2(0.5f, 0.5f);
                upRt.localScale = new Vector3(ArrowScale, ArrowScale, 1f);
                upRt.localRotation = Quaternion.Euler(0f, 0f, 90f);
                float halfExtent = HalfHeight(upRt) * ArrowScale * 0.5f;
                upRt.anchoredPosition = new Vector2(0f, ArrowStackSpacing * 0.5f + halfExtent);
            }
            if (downRt != null)
            {
                downRt.anchorMin = new Vector2(0.5f, 0.5f);
                downRt.anchorMax = new Vector2(0.5f, 0.5f);
                downRt.pivot = new Vector2(0.5f, 0.5f);
                downRt.localScale = new Vector3(ArrowScale, ArrowScale, 1f);
                downRt.localRotation = Quaternion.Euler(0f, 0f, 90f);
                float halfExtent = HalfHeight(downRt) * ArrowScale * 0.5f;
                downRt.anchoredPosition = new Vector2(0f, -(ArrowStackSpacing * 0.5f + halfExtent));
            }

            float w = 14f;
            float h = 28f;
            if (upRt != null)
            {
                float uw = upRt.rect.height > 4f ? upRt.rect.height : Mathf.Abs(upRt.sizeDelta.y);
                float uh = upRt.rect.width > 4f ? upRt.rect.width : Mathf.Abs(upRt.sizeDelta.x);
                if (uw < 4f)
                    uw = 22f;
                if (uh < 4f)
                    uh = 18f;
                w = uw * ArrowScale;
                h = uh * ArrowScale * 2f + ArrowStackSpacing;
            }
            rootRt.sizeDelta = new Vector2(w, h);
        }

        private static float HalfHeight(RectTransform rt)
        {
            float h = rt.rect.height > 4f ? rt.rect.height : Mathf.Abs(rt.sizeDelta.y);
            if (h < 4f)
                h = 18f;
            float w = rt.rect.width > 4f ? rt.rect.width : Mathf.Abs(rt.sizeDelta.x);
            if (w > 4f)
                return w;
            return h;
        }

        private static void ShrinkCraftForArrows(RectTransform craft)
        {
            if (craft == null || _root == null)
                return;

            RectTransform rootRt = _root.transform as RectTransform;
            float need = rootRt.sizeDelta.x + GapRightOfCraft + ExtraCraftInset;
            if (need < 20f)
                need = 24f;

            CaptureCraftLayout(craft);

            bool stretchX = craft.anchorMax.x - craft.anchorMin.x > 0.01f;
            if (stretchX)
            {
                Vector2 max = _origOffsetMax;
                max.x = _origOffsetMax.x - need;
                craft.offsetMax = max;
                craft.offsetMin = _origOffsetMin;
            }
            else
            {
                float newW = _origSizeDelta.x - need;
                if (_origSizeDelta.x > 0f && newW < 40f)
                    newW = 40f;
                craft.sizeDelta = new Vector2(newW, _origSizeDelta.y);
                float widthDelta = _origSizeDelta.x - newW;
                craft.anchoredPosition = new Vector2(
                    _origAnchoredPos.x - widthDelta * craft.pivot.x,
                    _origAnchoredPos.y);
            }
        }

        private static void CaptureCraftLayout(RectTransform craft)
        {
            if (_craftCaptured && _craftRt == craft)
                return;

            _craftRt = craft;
            _origSizeDelta = craft.sizeDelta;
            _origOffsetMin = craft.offsetMin;
            _origOffsetMax = craft.offsetMax;
            _origAnchoredPos = craft.anchoredPosition;
            _craftCaptured = true;
        }

        private static void RestoreCraftSize()
        {
            if (_craftCaptured && _craftRt != null)
            {
                _craftRt.sizeDelta = _origSizeDelta;
                _craftRt.offsetMin = _origOffsetMin;
                _craftRt.offsetMax = _origOffsetMax;
                _craftRt.anchoredPosition = _origAnchoredPos;
            }
            _craftCaptured = false;
            _craftRt = null;
        }

        private static void PlaceBesideCraft(RectTransform craft)
        {
            if (_root == null || craft == null)
                return;

            RectTransform rt = _root.transform as RectTransform;
            rt.anchorMin = craft.anchorMin;
            rt.anchorMax = craft.anchorMax;
            rt.pivot = new Vector2(0f, 0.5f);

            float craftW = craft.rect.width;
            if (craftW < 8f)
                craftW = Mathf.Abs(craft.sizeDelta.x);
            if (craftW < 8f)
                craftW = 80f;

            float craftH = craft.rect.height;
            if (craftH < 8f)
                craftH = Mathf.Abs(craft.sizeDelta.y);

            float craftCenterY = craft.anchoredPosition.y
                + (0.5f - craft.pivot.y) * craftH;
            float craftRight = craft.anchoredPosition.x
                + (1f - craft.pivot.x) * craftW;

            rt.anchoredPosition = new Vector2(craftRight + GapRightOfCraft, craftCenterY);
            _root.transform.SetAsLastSibling();
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

        private static void OnUpClicked()
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
                return;
            if (_amount >= MaxAllowed())
                return;
            if (!CanAfford(gui, _amount + 1))
                return;

            _amount++;
            ApplyVanillaFields(gui);
            RefreshArrowStates(gui);
            // Force vanilla to redraw reqs + Craft xN label.
            AccessToolsExt.RebuildCraftingPanel();
        }

        private static void OnDownClicked()
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
                return;
            if (_amount <= 1)
                return;

            _amount--;
            ApplyVanillaFields(gui);
            RefreshArrowStates(gui);
            AccessToolsExt.RebuildCraftingPanel();
        }
    }
}
