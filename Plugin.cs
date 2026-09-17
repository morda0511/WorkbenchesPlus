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
        public const string ModVersion = "1.0.1";
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
            catch (System.Exception ex)
            {
                Logger.LogWarning("Config save: " + ex.Message);
            }

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo(ModName + " v" + ModVersion + " by " + ModAuthor + " loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            if (Instance == this)
                Instance = null;
        }
    }
}
