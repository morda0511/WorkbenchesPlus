using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Extra hammer/build metres on top of vanilla GetStationBuildRange
    /// (m_rangeBuild + extensions). Stored per station on the ZDO.
    /// Cycle: 0 → 50 → 100 → 150 → 200 → 0.
    /// </summary>
    internal static class BuildRangeBonus
    {
        public const string ZdoKey = "WBP_buildExtra";
        public const int Step = 50;
        public const int MaxExtra = 200;

        private static readonly FieldInfo BuildRangeField =
            AccessTools.Field(typeof(CraftingStation), "m_buildRange");
        private static readonly FieldInfo ExtensionTimerField =
            AccessTools.Field(typeof(CraftingStation), "m_updateExtensionTimer");
        private static readonly FieldInfo AreaCircleField =
            AccessTools.Field(typeof(CraftingStation), "m_areaMarkerCircle");

        public static bool Ready()
        {
            return Plugin.Settings != null
                && Plugin.Settings.EnableMod.Value
                && Plugin.Settings.EnableBuildRangeCycle.Value;
        }

        /// <summary>Stations that actually feed the hammer via m_rangeBuild.</summary>
        public static bool HasBuildRange(CraftingStation station)
        {
            return station != null && station.m_rangeBuild > 0.01f;
        }

        public static int GetExtra(CraftingStation station)
        {
            if (station == null)
                return 0;

            ZNetView nv = station.GetComponent<ZNetView>();
            ZDO zdo = nv != null && nv.IsValid() ? nv.GetZDO() : null;
            if (zdo == null)
                return 0;

            int extra = zdo.GetInt(ZdoKey, 0);
            return Snap(extra);
        }

        public static bool TryCycle(CraftingStation station, Player player)
        {
            if (!Ready() || !HasBuildRange(station) || player == null)
                return false;

            ZNetView nv = station.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid() || nv.GetZDO() == null)
                return false;

            int extra = GetExtra(station);
            int next = extra >= MaxExtra ? 0 : extra + Step;

            if (!nv.IsOwner())
                nv.ClaimOwnership();
            nv.GetZDO().Set(ZdoKey, next);

            RefreshVisuals(station);

            float total = station.GetStationBuildRange();
            if (next <= 0)
                player.Message(MessageHud.MessageType.Center, "Build range: default (" + FormatMeters(total) + ")");
            else
                player.Message(MessageHud.MessageType.Center, "Build range: +" + next + " m (" + FormatMeters(total) + ")");

            return true;
        }

        public static float AddExtra(CraftingStation station, float vanillaRange)
        {
            if (!Ready() || !HasBuildRange(station))
                return vanillaRange;
            return vanillaRange + GetExtra(station);
        }

        public static void ApplyVisuals(CraftingStation station)
        {
            if (station == null || BuildRangeField == null)
                return;

            float vanilla;
            try
            {
                vanilla = (float)BuildRangeField.GetValue(station);
            }
            catch
            {
                return;
            }

            float display = AddExtra(station, vanilla);
            ApplyMarkerAndCollider(station, display);
        }

        public static void RefreshVisuals(CraftingStation station)
        {
            if (station == null)
                return;
            try
            {
                if (ExtensionTimerField != null)
                    ExtensionTimerField.SetValue(station, 2f);
            }
            catch
            {
            }

            station.GetStationBuildRange();
        }

        public static void ApplyMarkerAndCollider(CraftingStation station, float radius)
        {
            if (station == null)
                return;

            try
            {
                if (AreaCircleField != null)
                {
                    CircleProjector circle = AreaCircleField.GetValue(station) as CircleProjector;
                    if (circle != null)
                        circle.m_radius = radius;
                }
            }
            catch
            {
            }

            Collider collider = station.m_effectAreaCollider;
            if (collider is SphereCollider sphere)
                sphere.radius = radius;
            else if (collider is CapsuleCollider capsule)
                capsule.radius = radius;
        }

        public static string HoverHint(CraftingStation station)
        {
            if (!Ready() || !HasBuildRange(station))
                return "";
            if (Player.m_localPlayer == null || !station.InUseDistance(Player.m_localPlayer))
                return "";

            int extra = GetExtra(station);
            float total = station.GetStationBuildRange();
            string current = extra <= 0
                ? "default (" + FormatMeters(total) + ")"
                : "+" + extra + " m (" + FormatMeters(total) + ")";

            return "\n[<color=yellow><b>Shift+E</b></color>] Build range  " + current;
        }

        private static int Snap(int extra)
        {
            if (extra < 0)
                return 0;
            if (extra > MaxExtra)
                return MaxExtra;
            return (extra / Step) * Step;
        }

        private static string FormatMeters(float meters)
        {
            return Mathf.RoundToInt(meters) + " m";
        }
    }
}
