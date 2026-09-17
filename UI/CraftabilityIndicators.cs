using System.Collections;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace WorkbenchesPlus
{
    /// <summary>Subtle craftability mark on recipe list rows after vanilla builds them.</summary>
    internal static class CraftabilityIndicators
    {
        private static readonly FieldInfo AvailableRecipes =
            AccessTools.Field(typeof(InventoryGui), "m_availableRecipes");

        private static PropertyInfo _recipeProp;
        private static PropertyInfo _elementProp;
        private static bool _propsResolved;

        public static void Apply(InventoryGui gui)
        {
            if (gui == null || AvailableRecipes == null)
                return;

            object listObj = AvailableRecipes.GetValue(gui);
            IList list = listObj as IList;
            if (list == null || list.Count == 0)
                return;

            bool show = Plugin.Settings != null && Plugin.Settings.ShowCraftabilityIndicators.Value;
            if (!show)
            {
                ClearMarks(list);
                return;
            }

            EnsureProps(list[0]);
            Player player = Player.m_localPlayer;
            for (int i = 0; i < list.Count; i++)
            {
                object pair = list[i];
                if (pair == null)
                    continue;

                Recipe recipe = GetRecipe(pair);
                GameObject go = GetElement(pair);
                if (recipe == null || go == null)
                    continue;

                int bucket = Craftability.ScoreBucket(player, recipe);
                ApplyMark(go, bucket);
            }
        }

        private static void ClearMarks(IList list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                object pair = list[i];
                if (pair == null)
                    continue;
                EnsureProps(pair);
                GameObject go = GetElement(pair);
                if (go == null)
                    continue;
                Transform existing = go.transform.Find("WBP_Mark");
                if (existing != null)
                    Object.Destroy(existing.gameObject);
            }
        }

        private static void EnsureProps(object sample)
        {
            if (_propsResolved || sample == null)
                return;
            System.Type t = sample.GetType();
            _recipeProp = AccessTools.Property(t, "Recipe")
                ?? AccessTools.Property(t, "recipe");
            _elementProp = AccessTools.Property(t, "InterfaceElement")
                ?? AccessTools.Property(t, "Element")
                ?? AccessTools.Property(t, "element");
            _propsResolved = true;
        }

        private static Recipe GetRecipe(object pair)
        {
            if (_recipeProp != null)
                return _recipeProp.GetValue(pair, null) as Recipe;

            FieldInfo f = AccessTools.Field(pair.GetType(), "Recipe")
                ?? AccessTools.Field(pair.GetType(), "<Recipe>k__BackingField")
                ?? AccessTools.Field(pair.GetType(), "m_recipe");
            return f != null ? f.GetValue(pair) as Recipe : null;
        }

        private static GameObject GetElement(object pair)
        {
            if (_elementProp != null)
            {
                object v = _elementProp.GetValue(pair, null);
                if (v is GameObject g)
                    return g;
                if (v is Component c)
                    return c.gameObject;
            }

            FieldInfo f = AccessTools.Field(pair.GetType(), "InterfaceElement")
                ?? AccessTools.Field(pair.GetType(), "<InterfaceElement>k__BackingField")
                ?? AccessTools.Field(pair.GetType(), "m_element");
            if (f == null)
                return null;
            object val = f.GetValue(pair);
            if (val is GameObject go)
                return go;
            if (val is Component comp)
                return comp.gameObject;
            return null;
        }

        private static void ApplyMark(GameObject row, int bucket)
        {
            Transform existing = row.transform.Find("WBP_Mark");
            if (bucket != Craftability.BucketFully)
            {
                if (existing != null)
                    existing.gameObject.SetActive(false);
                return;
            }

            TMP_Text mark;
            if (existing == null)
            {
                var go = new GameObject("WBP_Mark", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(row.transform, false);
                RectTransform rt = go.transform as RectTransform;
                rt.anchorMin = new Vector2(1f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-6f, 0f);
                rt.sizeDelta = new Vector2(18f, 18f);
                mark = go.GetComponent<TextMeshProUGUI>();
                mark.fontSize = 14f;
                mark.alignment = TextAlignmentOptions.Center;
                mark.raycastTarget = false;
                TMP_Text sample = row.GetComponentInChildren<TMP_Text>(true);
                if (sample != null && sample.font != null)
                    mark.font = sample.font;
            }
            else
            {
                existing.gameObject.SetActive(true);
                mark = existing.GetComponent<TMP_Text>();
            }

            if (mark != null)
            {
                mark.text = "✓";
                mark.color = new Color(0.45f, 0.9f, 0.4f, 1f);
            }
        }
    }
}
