using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Optional absolute hammer/build radius override on top of vanilla
    /// GetStationBuildRange (m_rangeBuild + extensions). Stored per station on the ZDO.
    /// Cycle: default (vanilla) → 50 → 100 → 150 → 200 → default.
    /// </summary>
    internal static class BuildRangeBonus
    {
        public const string ZdoKey = "WBP_buildExtra";
        public const int Step = 50;
        public const int MaxOverride = 200;

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

        /// <summary>0 = vanilla; otherwise absolute metres (50 / 100 / 150 / 200).</summary>
        public static int GetExtra(CraftingStation station)
        {
            if (station == null)
                return 0;

            ZNetView nv = station.GetComponent<ZNetView>();
            ZDO zdo = nv != null && nv.IsValid() ? nv.GetZDO() : null;
            if (zdo == null)
                return 0;

            return Snap(zdo.GetInt(ZdoKey, 0));
        }

        public static bool TryCycle(CraftingStation station, Player player)
        {
            if (!Ready() || !HasBuildRange(station) || player == null)
                return false;

            ZNetView nv = station.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid() || nv.GetZDO() == null)
                return false;

            int current = GetExtra(station);
            int next = current >= MaxOverride ? 0 : current + Step;

            if (!nv.IsOwner())
                nv.ClaimOwnership();
            nv.GetZDO().Set(ZdoKey, next);

            RefreshVisuals(station);

            float total = station.GetStationBuildRange();
            if (next <= 0)
                player.Message(MessageHud.MessageType.Center, "Build range: default (" + FormatMeters(total) + ")");
            else
                player.Message(MessageHud.MessageType.Center, "Build range: " + FormatMeters(total));

            return true;
        }

        /// <summary>
        /// Postfix for GetStationBuildRange: keep vanilla, or replace with absolute override.
        /// </summary>
        public static float AddExtra(CraftingStation station, float vanillaRange)
        {
            if (!Ready() || !HasBuildRange(station))
                return vanillaRange;

            int overrideMeters = GetExtra(station);
            if (overrideMeters <= 0)
                return vanillaRange;

            return overrideMeters;
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

            int overrideMeters = GetExtra(station);
            float total = station.GetStationBuildRange();
            string current = overrideMeters <= 0
                ? "default (" + FormatMeters(total) + ")"
                : FormatMeters(total);

            return "\n[<color=yellow><b>Shift+E</b></color>] Build range  " + current;
        }

        private static int Snap(int value)
        {
            if (value < 0)
                return 0;
            if (value > MaxOverride)
                return MaxOverride;
            return (value / Step) * Step;
        }

        private static string FormatMeters(float meters)
        {
            return Mathf.RoundToInt(meters) + " m";
        }
    }
}
