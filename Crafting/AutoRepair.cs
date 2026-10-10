using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Repairs worn gear while the player stands inside the build range of a matching,
    /// usable station. One item per tick (RepairSeconds), so there is never a burst.
    /// Mirrors vanilla InventoryGui.CanRepair / RepairOneItem (station name, station level,
    /// roof + fire via CheckUsable, crafting skill XP, repair effect).
    /// </summary>
    internal static class AutoRepair
    {
        // Resolved once; if Valheim renames the field the feature switches itself off (no log spam).
        private static readonly List<CraftingStation> AllStations = ResolveStations();

        private static List<CraftingStation> ResolveStations()
        {
            try
            {
                return AccessTools.StaticFieldRefAccess<List<CraftingStation>>(typeof(CraftingStation), "m_allStations");
            }
            catch (System.Exception)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("CraftingStation.m_allStations not found - auto repair disabled.");
                return null;
            }
        }

        private static readonly List<ItemDrop.ItemData> Worn = new List<ItemDrop.ItemData>();
        private const float ScanSeconds = 5f;      // idle check
        private const float RepairSeconds = 1.5f;   // between two repairs
        private const float BelowPercent = 0.9f;    // repair at or below this durability

        private static float _nextTick;

        public static void Tick()
        {
            ModConfig cfg = Plugin.Settings;
            if (cfg == null || !cfg.EnableMod.Value || !cfg.EnableAutoRepair.Value)
                return;
            if (Time.unscaledTime < _nextTick)
                return;
            // Idle: look again only every ScanSeconds. After a repair the shorter
            // RepairSeconds applies, so a pile of worn items is still done one by one.
            _nextTick = Time.unscaledTime + ScanSeconds;

            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead() || player.IsTeleporting() || player.InCutscene())
                return;
            if (ObjectDB.instance == null || AllStations == null || AllStations.Count == 0)
                return;

            Inventory inv = player.GetInventory();
            if (inv == null)
                return;

            Worn.Clear();
            inv.GetWornItems(Worn);
            if (Worn.Count == 0)
                return;

            float limit = BelowPercent;
            for (int i = 0; i < Worn.Count; i++)
            {
                ItemDrop.ItemData item = Worn[i];
                if (item?.m_shared == null || !item.m_shared.m_canBeReparied)
                    continue;

                float max = item.GetMaxDurability();
                if (max <= 0.01f || item.m_durability / max > limit)
                    continue;

                CraftingStation station = FindStation(player, item);
                if (station == null)
                    continue;

                Repair(player, station, item, max);
                _nextTick = Time.unscaledTime + RepairSeconds;
                return; // one item per tick
            }
        }

        /// <summary>First station in its own build range that may repair this item.</summary>
        private static CraftingStation FindStation(Player player, ItemDrop.ItemData item)
        {
            Recipe recipe = ObjectDB.instance.GetRecipe(item);
            if (recipe == null || (recipe.m_craftingStation == null && recipe.m_repairStation == null))
                return null;

            Vector3 pos = player.transform.position;
            for (int i = 0; i < AllStations.Count; i++)
            {
                CraftingStation s = AllStations[i];
                if (s == null)
                    continue;

                bool matches =
                    (recipe.m_repairStation != null && recipe.m_repairStation.m_name == s.m_name)
                    || (recipe.m_craftingStation != null && recipe.m_craftingStation.m_name == s.m_name)
                    || item.m_worldLevel < Game.m_worldLevel;
                if (!matches)
                    continue;
                if (Mathf.Min(s.GetLevel(), 4) < recipe.m_minStationLevel)
                    continue;

                // Same flat distance check as CraftingStation.HaveBuildStationInRange (includes the override).
                Vector3 p = pos;
                p.y = s.transform.position.y;
                if (Vector3.Distance(s.transform.position, p) >= s.GetStationBuildRange())
                    continue;

                if (!s.CheckUsable(player, false))
                    continue;
                return s;
            }
            return null;
        }

        private static void Repair(Player player, CraftingStation station, ItemDrop.ItemData item, float max)
        {
            player.RaiseSkill(Skills.SkillType.Crafting, 1f - item.m_durability / max);
            item.m_durability = max;
            if (station.m_repairItemDoneEffects != null)
                station.m_repairItemDoneEffects.Create(station.transform.position, Quaternion.identity);
            player.Message(MessageHud.MessageType.TopLeft,
                Localization.instance.Localize("$msg_repaired", item.m_shared.m_name));
        }
    }
}
