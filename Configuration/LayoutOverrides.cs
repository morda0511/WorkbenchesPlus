using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using UnityEngine;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Layout tweaks made with tools/LayoutEditor.html. They are offsets on top of the default
    /// placement, so a different vanilla layout (or another mod moving the buttons) still works.
    /// Source, first one wins:
    /// 1. BepInEx/config/WorkbenchesPlus_layout.json   (quick testing, re-read when the file changes)
    /// 2. Assets/layout.json compiled into the DLL     (what players get)
    /// 3. nothing: defaults.
    /// Keys are flat: "categories.dx", "search.w", "sub.columns" ... missing key = default.
    /// </summary>
    internal static class LayoutOverrides
    {
        private const string FileName = "WorkbenchesPlus_layout.json";
        private static readonly Regex Pair = new Regex("\"([A-Za-z0-9_.]+)\"\\s*:\\s*(-?[0-9]+(?:\\.[0-9]+)?)", RegexOptions.Compiled);

        private static readonly Regex VanillaObj = new Regex(@"\{[^{}]*""path""[^{}]*\}", RegexOptions.Compiled);
        private static readonly Regex PathPair = new Regex(@"""path""\s*:\s*""([^""]+)""", RegexOptions.Compiled);

        internal sealed class VEntry { public string Path; public float L, T, R, B; public bool Sub; }

        /// <summary>Edge offsets for vanilla elements (px, y down), by path under InventoryGui.</summary>
        public static readonly List<VEntry> Vanilla = new List<VEntry>();
        public static int Version;

        private static readonly Dictionary<string, float> Values = new Dictionary<string, float>();
        private static bool _loaded;
        private static float _nextCheck;
        private static System.DateTime _fileStamp;

        private static string FilePath
        {
            get { return Path.Combine(Paths.ConfigPath, FileName); }
        }

        public static float Get(string key, float fallback)
        {
            EnsureLoaded();
            float v;
            return Values.TryGetValue(key, out v) ? v : fallback;
        }

        /// <summary>Move an element by its saved dx/dy (relative to where the default code put it).</summary>
        public static void Nudge(RectTransform rt, string name)
        {
            if (rt == null)
                return;
            rt.anchoredPosition += new Vector2(Get(name + ".dx", 0f), Get(name + ".dy", 0f));
        }

        public static void Refresh() { EnsureLoaded(); }

        private static void EnsureLoaded()
        {
            if (_loaded && Time.unscaledTime < _nextCheck)
                return;
            _nextCheck = Time.unscaledTime + 1f;

            string path = FilePath;
            bool hasFile = File.Exists(path);
            System.DateTime stamp = hasFile ? File.GetLastWriteTimeUtc(path) : System.DateTime.MinValue;
            if (_loaded && stamp == _fileStamp)
                return;

            _loaded = true;
            _fileStamp = stamp;
            Values.Clear();
            Vanilla.Clear();
            Version++;

            string json = null;
            try
            {
                if (hasFile)
                {
                    json = File.ReadAllText(path);
                }
                else
                {
                    using (Stream st = Assembly.GetExecutingAssembly().GetManifestResourceStream("WorkbenchesPlus.layout.json"))
                    {
                        if (st != null)
                            using (var r = new StreamReader(st))
                                json = r.ReadToEnd();
                    }
                }
            }
            catch
            {
                json = null;
            }

            if (string.IsNullOrEmpty(json))
                return;
            foreach (Match o in VanillaObj.Matches(json))
            {
                Match pm = PathPair.Match(o.Value);
                if (!pm.Success)
                    continue;
                var e = new VEntry { Path = pm.Groups[1].Value };
                foreach (Match m in Pair.Matches(o.Value))
                {
                    float v;
                    if (!float.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                        continue;
                    switch (m.Groups[1].Value)
                    {
                        case "l": e.L = v; break;
                        case "t": e.T = v; break;
                        case "r": e.R = v; break;
                        case "b": e.B = v; break;
                        case "sub": e.Sub = v > 0.5f; break;
                    }
                }
                Vanilla.Add(e);
            }
            foreach (Match m in Pair.Matches(json))
            {
                float v;
                if (float.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    Values[m.Groups[1].Value] = v;
            }
        }
    }
}
