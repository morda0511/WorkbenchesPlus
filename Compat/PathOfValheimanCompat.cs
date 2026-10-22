using System.Reflection;
using BepInEx.Bootstrap;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Path of Valheiman (treextr.pathofvalheiman) draws parts of the crafting window itself:
    /// - its Salvage / Disenchant tab replaces the recipe list with its own salvage rows,
    /// - its optional category grid ("Crafting list by category") hides the rows and shows a grid instead.
    /// While either is on screen, Workbenches+ keeps out of the list (no chips, search, sorting, headers,
    /// dismantle), so the two mods never fight over the same rows. Read by reflection: no hard dependency.
    /// </summary>
    internal static class PathOfValheimanCompat
    {
        private const string Guid = "treextr.pathofvalheiman";

        private static bool _bound;
        private static bool _loaded;
        private static PropertyInfo _salvaging;   // PassiveTree.CraftSalvage.Salvaging (internal static bool)
        private static PropertyInfo _gridOn;      // PassiveTree.UI.CraftCategories.On (private static bool)
        private static PropertyInfo _salvageTab;  // PassiveTree.CraftSalvage.Tab (internal static Button)
        private static MethodInfo _setSalvaging;  // PassiveTree.CraftSalvage.TestSalvaging(bool)
        private static MethodInfo _isRuneCarver;  // PassiveTree.RuneCarverTable.Is(CraftingStation)
        private static MethodInfo _placeSalvage;  // PassiveTree.CraftSalvage.Place(InventoryGui)

        /// <summary>Name of Path of Valheiman's tab button.</summary>
        public const string SalvageTabName = "PT_TabSalvage";

        public static bool Loaded
        {
            get
            {
                Bind();
                return _loaded;
            }
        }

        /// <summary>Path of Valheiman's Salvage / Disenchant tab is the open tab.</summary>
        public static bool Salvaging
        {
            get { return Loaded && Read(_salvaging); }
        }

        /// <summary>Path of Valheiman's category grid replaces the craft list (its config, craft tab only).</summary>
        public static bool CategoryGridOn
        {
            get { return Loaded && Read(_gridOn); }
        }

        /// <summary>Path of Valheiman owns what the recipe list shows right now.</summary>
        public static bool OwnsList(InventoryGui gui)
        {
            if (!Loaded)
                return false;
            if (Salvaging)
                return true;
            return CategoryGridOn && gui != null && gui.InCraftTab() && !DismantleMode.Active;
        }

        /// <summary>
        /// Config HidePathOfValheimanSalvage: keep its Salvage tab hidden at normal stations while our Dismantle
        /// tab is there. Its Disenchant tab at the Rune Carver Table is never touched. Called every frame
        /// (Path of Valheiman switches its tab on again whenever it rebuilds the panel).
        /// </summary>
        /// <summary>At the current station the option hides Path of Valheiman's Salvage tab.</summary>
        public static bool SalvageHiddenHere()
        {
            if (Plugin.Settings == null || !Plugin.Settings.HidePathOfValheimanSalvage.Value || !Loaded)
                return false;
            if (!Plugin.Settings.EnableMod.Value || !Plugin.Settings.EnableDismantle.Value || CategoryGridOn)
                return false;   // no Dismantle tab of ours: leave theirs alone
            Player player = Player.m_localPlayer;
            CraftingStation station = player != null ? player.GetCurrentCraftingStation() : null;
            return station != null && !StationFilter.IsUpgrader(station) && !IsRuneCarver(station);
        }

        /// <summary>
        /// Let Path of Valheiman place its tab again once our Dismantle tab exists: it moves its own tab past
        /// other tabs, but only those already there when it placed it (first open of a station: on top of ours).
        /// </summary>
        public static void PlaceSalvageTab(InventoryGui gui)
        {
            if (gui == null || !Loaded || _placeSalvage == null)
                return;
            try
            {
                _placeSalvage.Invoke(null, new object[] { gui });
            }
            catch
            {
            }
        }

        public static void EnforceSalvageHidden(InventoryGui gui)
        {
            if (gui == null || _salvageTab == null || !SalvageHiddenHere())
                return;

            UnityEngine.UI.Button tab;
            try
            {
                tab = _salvageTab.GetValue(null, null) as UnityEngine.UI.Button;
            }
            catch
            {
                return;
            }
            if (tab == null || !tab.gameObject.activeSelf)
                return;

            tab.gameObject.SetActive(false);
            if (Salvaging && _setSalvaging != null)
            {
                try
                {
                    _setSalvaging.Invoke(null, new object[] { false });
                    gui.OnTabCraftPressed();
                }
                catch
                {
                }
            }
        }

        private static bool IsRuneCarver(CraftingStation station)
        {
            if (_isRuneCarver == null)
                return false;
            try
            {
                return _isRuneCarver.Invoke(null, new object[] { station }) is bool b && b;
            }
            catch
            {
                return false;
            }
        }

        private static bool Read(PropertyInfo p)
        {
            if (p == null)
                return false;
            try
            {
                return p.GetValue(null, null) is bool b && b;
            }
            catch
            {
                return false;
            }
        }

        private static void Bind()
        {
            if (_bound)
                return;
            _bound = true;

            BepInEx.PluginInfo info;
            if (!Chainloader.PluginInfos.TryGetValue(Guid, out info) || info == null || info.Instance == null)
                return;

            _loaded = true;
            Assembly asm = info.Instance.GetType().Assembly;
            const BindingFlags Any = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            _salvaging = asm.GetType("PassiveTree.CraftSalvage")?.GetProperty("Salvaging", Any);
            _gridOn = asm.GetType("PassiveTree.UI.CraftCategories")?.GetProperty("On", Any);
            System.Type salvage = asm.GetType("PassiveTree.CraftSalvage");
            _salvageTab = salvage?.GetProperty("Tab", Any);
            _setSalvaging = salvage?.GetMethod("TestSalvaging", Any);
            _placeSalvage = salvage?.GetMethod("Place", Any, null, new[] { typeof(InventoryGui) }, null);
            _isRuneCarver = asm.GetType("PassiveTree.RuneCarverTable")?.GetMethod("Is", Any, null, new[] { typeof(CraftingStation) }, null);

            if (Plugin.Log != null)
                Plugin.Log.LogInfo("Path of Valheiman found: Workbenches+ steps aside while its Salvage tab or category grid is shown"
                    + (_salvaging == null || _gridOn == null ? " (some hooks not found, check after updates)." : "."));
        }
    }
}
