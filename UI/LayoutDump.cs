using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;
using UnityEngine.UI;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Writes where the vanilla crafting window and our own elements really are
    /// (BepInEx/config/WorkbenchesPlus_layout_dump.json). tools/LayoutEditor.html loads it, so the
    /// layout is drawn on the real window instead of a guess. Once per game start, only with DebugLogging.
    /// </summary>
    internal static class LayoutDump
    {
        private static bool _done;
        private static float _visibleSince = -1f;

        public static void Tick()
        {
            // Developer tool for tools/LayoutEditor.html: only with DebugLogging on.
            if (_done || Plugin.Settings == null || !Plugin.Settings.DebugLogging.Value)
                return;
            InventoryGui gui = InventoryGui.instance;
            RectTransform panel = gui != null ? AccessToolsExt.CraftingPanel(gui) : null;
            if (panel == null || !panel.gameObject.activeInHierarchy)
            {
                _visibleSince = -1f;
                return;
            }
            if (_visibleSince < 0f)
                _visibleSince = Time.unscaledTime;
            if (Time.unscaledTime - _visibleSince < 2f)
                return;

            _done = true;
            try
            {
                Write(gui, panel);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning("LayoutDump: " + ex.Message);
            }
        }

        private static void Write(InventoryGui gui, RectTransform panel)
        {
            Canvas.ForceUpdateCanvases();
            var sb = new StringBuilder();
            RectTransform craftPanel = panel;
            Canvas cv = panel.GetComponentInParent<Canvas>();
            if (cv != null && cv.rootCanvas.transform is RectTransform)
                panel = (RectTransform)cv.rootCanvas.transform;
            Rect pr = panel.rect;
            sb.Append("{\n  \"version\": 1,\n");
            sb.Append("  \"panel\": { \"w\": ").Append(F(pr.width)).Append(", \"h\": ").Append(F(pr.height)).Append(" },\n");
            sb.Append("  \"elements\": {\n");

            var items = new List<KeyValuePair<string, RectTransform>>();
            items.Add(new KeyValuePair<string, RectTransform>("recipeList", AccessToolsExt.RecipeListRoot(gui)));
            items.Add(new KeyValuePair<string, RectTransform>("repair", AccessToolsExt.RepairButton(gui)));
            items.Add(new KeyValuePair<string, RectTransform>("craft", AccessToolsExt.CraftButton(gui)));
            Button tc = AccessToolsExt.TabCraft(gui), tu = AccessToolsExt.TabUpgrade(gui);
            items.Add(new KeyValuePair<string, RectTransform>("tabCraft", tc != null ? tc.transform as RectTransform : null));
            items.Add(new KeyValuePair<string, RectTransform>("tabUpgrade", tu != null ? tu.transform as RectTransform : null));
            items.Add(new KeyValuePair<string, RectTransform>("craftingPanel", craftPanel));
            items.Add(new KeyValuePair<string, RectTransform>("inventory", gui.m_player));
            items.Add(new KeyValuePair<string, RectTransform>("container", gui.m_container));
            items.Add(new KeyValuePair<string, RectTransform>("info", gui.m_info));
            AddNamed(panel, items, "categories", "WBP_Categories");
            AddNamed(panel, items, "search", "WBP_Search");
            AddNamed(panel, items, "multiplier", "WBP_CraftMultiplier");
            AddNamed(panel, items, "dismantle", "WBP_TabDismantle");

            bool first = true;
            foreach (var kv in items)
            {
                if (kv.Value == null)
                    continue;
                // corners in the panel's own space, origin top-left, y down (like a normal image)
                var c = new Vector3[4];
                kv.Value.GetWorldCorners(c);
                Vector3 bl = panel.InverseTransformPoint(c[0]);
                Vector3 tr = panel.InverseTransformPoint(c[2]);
                float x = bl.x - pr.xMin;
                float y = pr.yMax - tr.y;
                // report the DEFAULT place: take a saved offset back out (see LayoutOverrides.Nudge)
                x -= LayoutOverrides.Get(kv.Key + ".dx", 0f);
                y += LayoutOverrides.Get(kv.Key + ".dy", 0f);
                if (!first)
                    sb.Append(",\n");
                first = false;
                sb.Append("    \"").Append(kv.Key).Append("\": { \"x\": ").Append(F(x))
                  .Append(", \"y\": ").Append(F(y))
                  .Append(", \"w\": ").Append(F(tr.x - bl.x))
                  .Append(", \"h\": ").Append(F(tr.y - bl.y)).Append(" }");
            }
            sb.Append("\n  },\n  \"extras\": [");
            bool firstX = true;
            _count = 0;
            AppendChildren(sb, panel, pr, gui.transform as RectTransform, gui.name, 1, ref firstX);
            foreach (RectTransform root in new RectTransform[0])
                AppendChildren(sb, panel, pr, root, "", 1, ref firstX);
            sb.Append("\n  ]\n}\n");

            string path = Path.Combine(Paths.ConfigPath, "WorkbenchesPlus_layout_dump.json");
            File.WriteAllText(path, sb.ToString());
            Plugin.Log.LogInfo("Layout dump written: " + path);
        }

        private static int _count;

        /// <summary>Every active rectangle under the inventory GUI with its path, text and sprite: only a picture for orientation in the editor.</summary>
        private static void AppendChildren(StringBuilder sb, RectTransform frame, Rect fr, RectTransform parent, string path, int depth, ref bool first)
        {
            if (parent == null || depth > 14 || _count > 8000)
                return;
            for (int i = 0; i < parent.childCount; i++)
            {
                RectTransform t = parent.GetChild(i) as RectTransform;
                if (t == null || !t.gameObject.activeInHierarchy)
                    continue;
                string p = path + "/" + Seg(parent, t, i);
                var c = new Vector3[4];
                t.GetWorldCorners(c);
                Vector3 bl = frame.InverseTransformPoint(c[0]);
                Vector3 tr = frame.InverseTransformPoint(c[2]);
                _count++;
                float ox = 0f, oy = 0f, ow = 0f, oh = 0f;
                LayoutOverrides.VEntry ve;
                if (VanillaLayout.Effective().TryGetValue(p, out ve))
                {
                    ox = ve.L; oy = ve.T; ow = ve.R - ve.L; oh = ve.B - ve.T;
                }
                string text = null;
                var tmp = t.GetComponent<TMPro.TMP_Text>();
                if (tmp != null)
                    text = tmp.text;
                else
                {
                    var ui = t.GetComponent<Text>();
                    if (ui != null)
                        text = ui.text;
                }
                var img = t.GetComponent<Image>();
                string sprite = img != null && img.sprite != null ? img.sprite.name : null;

                sb.Append(first ? "\n" : ",\n");
                first = false;
                sb.Append("    { \"name\": \"").Append(J(t.name)).Append("\", \"path\": \"").Append(J(p))
                  .Append("\", \"depth\": ").Append(depth)
                  .Append(", \"x\": ").Append(F(bl.x - fr.xMin - ox)).Append(", \"y\": ").Append(F(fr.yMax - tr.y - oy))
                  .Append(", \"w\": ").Append(F(tr.x - bl.x - ow)).Append(", \"h\": ").Append(F(tr.y - bl.y - oh));
                sb.Append(", \"amin\": [").Append(F3(t.anchorMin.x)).Append(", ").Append(F3(t.anchorMin.y))
                  .Append("], \"amax\": [").Append(F3(t.anchorMax.x)).Append(", ").Append(F3(t.anchorMax.y)).Append("]");
                if (!string.IsNullOrEmpty(text))
                    sb.Append(", \"text\": \"").Append(J(text.Length > 80 ? text.Substring(0, 80) : text)).Append("\"");
                if (sprite != null)
                    sb.Append(", \"sprite\": \"").Append(J(sprite)).Append("\"");
                sb.Append(" }");
                AppendChildren(sb, frame, fr, t, p, depth + 1, ref first);
            }
        }

        /// <summary>Path segment: the name, plus "|n" when siblings share the name (n = which one).</summary>
        internal static string Seg(Transform parent, Transform t, int index)
        {
            int same = 0, nth = 0;
            for (int j = 0; j < parent.childCount; j++)
            {
                Transform o = parent.GetChild(j);
                if (o.name != t.name)
                    continue;
                if (j < index)
                    nth++;
                same++;
            }
            return same > 1 ? t.name + "|" + nth : t.name;
        }

        private static string J(string s)
        {
            return s.Replace("\\", "/").Replace("\"", "'").Replace("\r", " ").Replace("\n", " ");
        }


        private static void AddNamed(RectTransform panel, List<KeyValuePair<string, RectTransform>> items, string key, string goName)
        {
            foreach (RectTransform t in panel.GetComponentsInChildren<RectTransform>(true))
            {
                if (t.name == goName)
                {
                    items.Add(new KeyValuePair<string, RectTransform>(key, t));
                    return;
                }
            }
        }

        private static string F3(float v)
        {
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string F(float v)
        {
            return v.ToString("0.#", CultureInfo.InvariantCulture);
        }
    }
}
