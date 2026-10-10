using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Favourite recipes, kept as item prefab names in BepInEx/config/WorkbenchesPlus_favorites.txt
    /// (one per line). Right-click a recipe row to toggle.
    /// </summary>
    internal static class FavoritesStore
    {
        private static HashSet<string> _set;

        private static string FilePath
        {
            get { return Path.Combine(Paths.ConfigPath, "WorkbenchesPlus_favorites.txt"); }
        }

        public static bool Is(Recipe recipe)
        {
            string key = KeyOf(recipe);
            return key != null && Set().Contains(key);
        }

        public static void Toggle(Recipe recipe)
        {
            string key = KeyOf(recipe);
            if (key == null)
                return;

            HashSet<string> set = Set();
            if (!set.Remove(key))
                set.Add(key);
            Save(set);
        }

        private static string KeyOf(Recipe recipe)
        {
            RecipeInfo info = SortEngine.Info(recipe);
            return info != null ? info.Facts.Prefab : null;
        }

        private static HashSet<string> Set()
        {
            if (_set != null)
                return _set;

            _set = new HashSet<string>();
            try
            {
                if (File.Exists(FilePath))
                {
                    foreach (string line in File.ReadAllLines(FilePath))
                    {
                        string s = line.Trim();
                        if (s.Length > 0)
                            _set.Add(s);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning("Favorites load: " + ex.Message);
            }
            return _set;
        }

        private static void Save(HashSet<string> set)
        {
            try
            {
                File.WriteAllLines(FilePath, new List<string>(set).ToArray());
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning("Favorites save: " + ex.Message);
            }
        }
    }

    /// <summary>Right-click on a recipe row toggles its favourite state.</summary>
    internal sealed class FavoriteRowClick : MonoBehaviour, IPointerClickHandler
    {
        private Recipe _recipe;

        public static void Attach(GameObject row, Recipe recipe)
        {
            if (row == null || recipe == null)
                return;
            FavoriteRowClick c = row.GetComponent<FavoriteRowClick>();
            if (c == null)
                c = row.AddComponent<FavoriteRowClick>();
            c._recipe = recipe;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Right || _recipe == null)
                return;
            FavoritesStore.Toggle(_recipe);
            AccessToolsExt.RebuildCraftingPanel();
        }
    }
}
