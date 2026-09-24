using HarmonyLib;

namespace WorkbenchesPlus
{
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetStationBuildRange))]
    internal static class BuildRangeGetStationPatch
    {
        private static void Postfix(CraftingStation __instance, ref float __result)
        {
            try
            {
                __result = BuildRangeBonus.AddExtra(__instance, __result);
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("GetStationBuildRange: " + ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(CraftingStation), "GetExtensions")]
    internal static class BuildRangeExtensionsVisualPatch
    {
        private static void Postfix(CraftingStation __instance)
        {
            try
            {
                if (!BuildRangeBonus.Ready() || !BuildRangeBonus.HasBuildRange(__instance))
                    return;
                if (BuildRangeBonus.GetExtra(__instance) <= 0)
                    return;
                BuildRangeBonus.ApplyVisuals(__instance);
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
    internal static class BuildRangeInteractPatch
    {
        private static bool Prefix(CraftingStation __instance, Humanoid user, bool repeat, bool alt, ref bool __result)
        {
            try
            {
                if (repeat || !alt)
                    return true;

                Player player = user as Player;
                if (player == null || player != Player.m_localPlayer)
                    return true;
                if (!BuildRangeBonus.Ready() || !BuildRangeBonus.HasBuildRange(__instance))
                    return true;
                if (!__instance.InUseDistance(user))
                    return true;

                BuildRangeBonus.TryCycle(__instance, player);
                __result = false;
                return false;
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("Build range cycle: " + ex.Message);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetHoverText))]
    internal static class BuildRangeHoverPatch
    {
        private static void Postfix(CraftingStation __instance, ref string __result)
        {
            try
            {
                if (string.IsNullOrEmpty(__result) || __instance == null)
                    return;
                string hint = BuildRangeBonus.HoverHint(__instance);
                if (string.IsNullOrEmpty(hint))
                    return;
                __result = __result + hint;
            }
            catch
            {
            }
        }
    }
}
