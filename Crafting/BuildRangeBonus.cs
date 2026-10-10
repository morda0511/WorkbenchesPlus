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
        public const int MaxOverrideLimit = 500;

        /// <summary>Configured highest override (BuildRangeMaxMeters), snapped to Step.</summary>
        public static int MaxOverride
        {
            get
            {
                int v = Plugin.Settings != null ? Plugin.Settings.BuildRangeMaxMeters.Value : 200;
                return System.Math.Max(Step, System.Math.Min(MaxOverrideLimit, (v / Step) * Step));
            }
        }

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

        /// <summary>
        /// Stations the hammer actually needs: m_rangeBuild &gt; 0 AND at least one placeable piece
        /// (not just a station add-on) lists it as m_craftingStation. Cauldron / prep table etc. drop out.
        /// </summary>
        public static bool HasBuildRange(CraftingStation station)
        {
            return station != null && station.m_rangeBuild > 0.01f && IsNeededForBuilding(station);
        }

        private static System.Collections.Generic.HashSet<string> _buildStations;
        private static ZNetScene _buildStationsScene;

        private static bool IsNeededForBuilding(CraftingStation station)
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null || string.IsNullOrEmpty(station.m_name))
                return false;

            if (_buildStations == null || _buildStationsScene != scene)
            {
                var set = new System.Collections.Generic.HashSet<string>();
                foreach (GameObject prefab in scene.m_prefabs)
                {
                    if (prefab == null)
                        continue;
                    Piece piece = prefab.GetComponent<Piece>();
                    if (piece == null || piece.m_craftingStation == null)
                        continue;
                    // Cauldron/prep-table add-ons are placed once next to the station - no range boost needed.
                    if (prefab.GetComponent<StationExtension>() != null)
                        continue;
                    set.Add(piece.m_craftingStation.m_name);
                }
                _buildStations = set;
                _buildStationsScene = scene;
                if (Plugin.Settings != null && Plugin.Settings.DebugLogging.Value)
                    Plugin.Log.LogInfo("Build range stations: " + string.Join(", ", set));
            }

            return _buildStations.Contains(station.m_name);
        }

        private struct CacheEntry
        {
            public int Value;
            public int Frame;
        }

        // GetStationBuildRange runs many times per frame while building: read the ZDO once per frame.
        private static readonly System.Collections.Generic.Dictionary<CraftingStation, CacheEntry> Cache =
            new System.Collections.Generic.Dictionary<CraftingStation, CacheEntry>();

        /// <summary>0 = vanilla; otherwise absolute metres (50, 100, ... up to BuildRangeMaxMeters).</summary>
        public static int GetExtra(CraftingStation station)
        {
            if (station == null)
                return 0;

            int frame = Time.frameCount;
            CacheEntry entry;
            if (Cache.TryGetValue(station, out entry) && entry.Frame == frame)
                return entry.Value;

            int value = ReadZdo(station);
            if (Cache.Count > 256)
                PruneCache();
            Cache[station] = new CacheEntry { Value = value, Frame = frame };
            return value;
        }

        private static int ReadZdo(CraftingStation station)
        {
            ZNetView nv = station.GetComponent<ZNetView>();
            ZDO zdo = nv != null && nv.IsValid() ? nv.GetZDO() : null;
            if (zdo == null)
                return 0;

            return Snap(zdo.GetInt(ZdoKey, 0));
        }

        private static void PruneCache()
        {
            var dead = new System.Collections.Generic.List<CraftingStation>();
            foreach (CraftingStation key in Cache.Keys)
            {
                if (key == null)
                    dead.Add(key);
            }
            for (int i = 0; i < dead.Count; i++)
                Cache.Remove(dead[i]);
        }

        public static bool TryCycle(CraftingStation station, Player player)
        {
            if (!Ready() || !HasBuildRange(station) || player == null)
                return false;

            ZNetView nv = station.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid() || nv.GetZDO() == null)
                return false;

            if (!PrivateArea.CheckAccess(station.transform.position, 0f, true, false))
            {
                player.Message(MessageHud.MessageType.Center, Loc.T("no_access"));
                return true;
            }

            int current = GetExtra(station);
            int next = current >= MaxOverride ? 0 : current + Step;

            if (!nv.IsOwner())
                nv.ClaimOwnership();
            nv.GetZDO().Set(ZdoKey, next);
            Cache.Remove(station);

            RefreshVisuals(station);

            float total = station.GetStationBuildRange();
            string label = Loc.T("build_range") + ": ";
            if (next <= 0)
                player.Message(MessageHud.MessageType.Center, label + Loc.T("default") + " (" + FormatMeters(total) + ")");
            else
                player.Message(MessageHud.MessageType.Center, label + FormatMeters(total));

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
                ? Loc.T("default") + " (" + FormatMeters(total) + ")"
                : FormatMeters(total);

            string key = Localization.instance != null
                ? Localization.instance.Localize("$KEY_AltPlace + $KEY_Use")
                : "Shift+E";
            return "\n[<color=yellow><b>" + key + "</b></color>] " + Loc.T("build_range") + "  " + current;
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
