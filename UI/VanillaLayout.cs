using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Moves / resizes vanilla elements of the inventory window by the edge offsets saved with
    /// tools/LayoutEditor.html ("vanilla" list in the layout file). Runs right before the canvases
    /// render, after the game's own layout groups. Each frame it takes its own last offset back out
    /// first, so the offsets never add up; if the game changed the element in between, the new value
    /// becomes the starting point.
    /// </summary>
    internal static class VanillaLayout
    {
        private sealed class State
        {
            public RectTransform Rt;
            public float NextTry;
            public bool Has;
            public Vector2 SetMin, SetMax, DMin, DMax;
            public LayoutElement Ignore;
            public bool IgnoreAdded, IgnorePrev;
            public ContentSizeFitter Fitter;
        }

        private static readonly Dictionary<string, State> States = new Dictionary<string, State>();

        /// <summary>Entries in force right now: the normal ones; while a sub-chip panel is shown, its own entries replace those of the same path.</summary>
        internal static Dictionary<string, LayoutOverrides.VEntry> Effective()
        {
            var map = new Dictionary<string, LayoutOverrides.VEntry>();
            bool sub = CategoryBar.SubPanelVisible;
            foreach (LayoutOverrides.VEntry e in LayoutOverrides.Vanilla)
            {
                if (!e.Sub)
                    map[e.Path] = e;
            }
            if (sub)
            {
                foreach (LayoutOverrides.VEntry e in LayoutOverrides.Vanilla)
                {
                    if (e.Sub)
                        map[e.Path] = e;
                }
            }
            return map;
        }

        public static void Tick()
        {
            LayoutOverrides.Refresh();
            InventoryGui gui = InventoryGui.instance;
            Dictionary<string, LayoutOverrides.VEntry> wanted = Effective();

            // no longer wanted (file changed, or the sub-chip panel closed): put it back
            var drop = new List<string>();
            foreach (var kv in States)
            {
                if (!wanted.ContainsKey(kv.Key))
                    drop.Add(kv.Key);
            }
            foreach (string k in drop)
            {
                Restore(States[k]);
                States.Remove(k);
            }

            if (gui == null || wanted.Count == 0)
                return;

            foreach (var kv in wanted)
            {
                LayoutOverrides.VEntry e = kv.Value;
                State s;
                if (!States.TryGetValue(e.Path, out s))
                {
                    s = new State();
                    States[e.Path] = s;
                }
                if (s.Rt == null)
                {
                    if (Time.unscaledTime < s.NextTry)
                        continue;
                    s.NextTry = Time.unscaledTime + 1f;
                    s.Rt = Resolve(gui.transform, e.Path);
                    s.Has = false;
                    if (s.Rt == null)
                        continue;
                    TakeOutOfLayout(s);
                }
                Apply(s, e);
            }
        }

        private static void Apply(State s, LayoutOverrides.VEntry e)
        {
            RectTransform rt = s.Rt;
            // canvas pixels -> this element's parent space
            float k = 1f;
            Canvas cv = rt.GetComponentInParent<Canvas>();
            Transform parent = rt.parent;
            if (cv != null && parent != null)
            {
                float cs = cv.rootCanvas.transform.lossyScale.x;
                float ps = parent.lossyScale.x;
                if (cs > 1e-5f && ps > 1e-5f)
                    k = ps / cs;
            }
            Vector2 dMin = new Vector2(e.L, -e.B) / k;
            Vector2 dMax = new Vector2(e.R, -e.T) / k;

            Vector2 baseMin = rt.offsetMin, baseMax = rt.offsetMax;
            if (s.Has && Near(baseMin, s.SetMin) && Near(baseMax, s.SetMax))
            {
                baseMin -= s.DMin;
                baseMax -= s.DMax;
            }

            Vector2 newMin = baseMin + dMin, newMax = baseMax + dMax;
            if (!(Near(rt.offsetMin, newMin) && Near(rt.offsetMax, newMax)))
            {
                rt.offsetMin = newMin;
                rt.offsetMax = newMax;
            }
            s.SetMin = rt.offsetMin;
            s.SetMax = rt.offsetMax;
            s.DMin = dMin;
            s.DMax = dMax;
            s.Has = true;
        }

        private static void Restore(State s)
        {
            if (s.Rt == null)
                return;
            if (s.Has && Near(s.Rt.offsetMin, s.SetMin) && Near(s.Rt.offsetMax, s.SetMax))
            {
                s.Rt.offsetMin -= s.DMin;
                s.Rt.offsetMax -= s.DMax;
            }
            if (s.Ignore != null)
            {
                if (s.IgnoreAdded)
                    Object.Destroy(s.Ignore);
                else
                    s.Ignore.ignoreLayout = s.IgnorePrev;
            }
            if (s.Fitter != null)
                s.Fitter.enabled = true;
        }

        /// <summary>A parent layout group or a size fitter on the element would undo the change.</summary>
        private static void TakeOutOfLayout(State s)
        {
            RectTransform rt = s.Rt;
            if (rt.parent != null && rt.parent.GetComponent<LayoutGroup>() != null)
            {
                LayoutElement le = rt.GetComponent<LayoutElement>();
                if (le == null)
                {
                    le = rt.gameObject.AddComponent<LayoutElement>();
                    s.IgnoreAdded = true;
                }
                s.Ignore = le;
                s.IgnorePrev = le.ignoreLayout;
                le.ignoreLayout = true;
            }
            ContentSizeFitter f = rt.GetComponent<ContentSizeFitter>();
            if (f != null && f.enabled)
            {
                s.Fitter = f;
                f.enabled = false;
            }
        }

        /// <summary>Path = "InventoryGui/Child/Other|2/..." (|n = n-th child with that name).</summary>
        private static RectTransform Resolve(Transform root, string path)
        {
            string[] seg = path.Split('/');
            Transform t = root;
            for (int i = 1; i < seg.Length; i++)
            {
                string name = seg[i];
                int nth = 0;
                int bar = name.LastIndexOf('|');
                if (bar > 0 && int.TryParse(name.Substring(bar + 1), out nth))
                    name = name.Substring(0, bar);
                else
                    nth = 0;

                Transform found = null;
                int seen = 0;
                for (int c = 0; c < t.childCount; c++)
                {
                    Transform ch = t.GetChild(c);
                    if (ch.name != name)
                        continue;
                    if (seen == nth)
                    {
                        found = ch;
                        break;
                    }
                    seen++;
                }
                if (found == null)
                    return null;
                t = found;
            }
            return t as RectTransform;
        }

        private static bool Near(Vector2 a, Vector2 b)
        {
            return (a - b).sqrMagnitude < 1e-4f;
        }
    }
}
