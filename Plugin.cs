using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace WorkbenchesPlus
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.morda.workbenchesplus";
        public const string ModName = "Workbenches+";
        public const string ModVersion = ModBuild.Version;
        public const string ModAuthor = "Morda";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }
        internal static ModConfig Settings { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Settings = new ModConfig(Config);
            try
            {
                Config.Save();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Config save: " + ex.Message);
            }

            _harmony = new Harmony(ModGuid);
            PatchAllSafe();
            UnityEngine.Canvas.willRenderCanvases += SafeVanillaTick;
            ReflectionCheck.Run();
            Config.SettingChanged += OnSettingChanged;
            Logger.LogInfo(ModName + " v" + ModVersion + " by " + ModAuthor + " loaded.");
        }

        /// <summary>
        /// PatchAll aborts on the first broken patch class. Patch class by class so one missing
        /// vanilla method only disables its own feature after a game update.

        private static bool _tickFailed;

        private static void SafeVanillaTick()
        {
            if (_tickFailed)
                return;
            try
            {
                VanillaLayout.Tick();
            }
            catch (Exception ex)
            {
                _tickFailed = true;
                Log.LogWarning("VanillaLayout disabled: " + ex);
            }
        }
        /// </summary>
        private void PatchAllSafe()
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!type.IsDefined(typeof(HarmonyPatch), false))
                    continue;
                try
                {
                    _harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception ex)
                {
                    Logger.LogError("Patch " + type.Name + " failed (feature disabled): " + ex.Message);
                }
            }
        }

        private static void OnSettingChanged(object sender, EventArgs e)
        {
            try
            {
                InventoryGui gui = InventoryGui.instance;
                if (gui != null && gui.isActiveAndEnabled)
                    AccessToolsExt.RebuildCraftingPanel();
            }
            catch (Exception ex)
            {
                if (Log != null)
                    Log.LogWarning("Setting refresh: " + ex.Message);
            }
        }

        private void Update()
        {
            try
            {
                AutoRepair.Tick();
                RecipeDump.Tick();
                LayoutDump.Tick();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("AutoRepair: " + ex.Message);
            }
        }

        private void OnDestroy()
        {
            Config.SettingChanged -= OnSettingChanged;
            _harmony?.UnpatchSelf();
            if (Instance == this)
                Instance = null;
        }
    }
}
