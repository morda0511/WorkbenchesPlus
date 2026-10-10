using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Filter symbols. Per key, the first of these wins:
    /// 1. a PNG next to the DLL:  plugins/WorkbenchesPlus/icons/&lt;Key&gt;.png  (quick testing, no rebuild)
    /// 2. a PNG compiled into the DLL: Assets/Icons/&lt;Key&gt;.png
    /// 3. our drawn symbol (GlyphPainter), white with soft edges
    /// </summary>
    internal static class GlyphIcons
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> Custom = new HashSet<string>();

        /// <summary>True when the symbol comes from a PNG (shown in its own colors, not gold-tinted).</summary>
        public static bool IsCustom(string key)
        {
            Get(key);
            return Custom.Contains(key);
        }

        public static Sprite Get(string key)
        {
            Sprite s;
            if (Cache.TryGetValue(key, out s) && s != null)
                return s;

            s = LoadPng(key);
            if (s != null)
                Custom.Add(key);
            else
                s = Draw(key);

            Cache[key] = s;
            return s;
        }

        private static Sprite LoadPng(string key)
        {
            try
            {
                byte[] bytes = null;

                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string file = dir != null ? Path.Combine(Path.Combine(dir, "icons"), key + ".png") : null;
                if (file != null && File.Exists(file))
                    bytes = File.ReadAllBytes(file);

                if (bytes == null)
                {
                    using (Stream st = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                        "WorkbenchesPlus.icons." + key + ".png"))
                    {
                        if (st != null)
                        {
                            bytes = new byte[st.Length];
                            int read = 0;
                            while (read < bytes.Length)
                            {
                                int n = st.Read(bytes, read, bytes.Length - read);
                                if (n <= 0)
                                    break;
                                read += n;
                            }
                        }
                    }
                }

                if (bytes == null || bytes.Length == 0)
                    return null;

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!LoadImage(tex, bytes))
                {
                    Object.Destroy(tex);
                    return null;
                }
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                tex.hideFlags = HideFlags.HideAndDontSave;
                Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                s.hideFlags = HideFlags.HideAndDontSave;
                return s;
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("Icon '" + key + "': " + ex.Message);
                return null;
            }
        }

        // ImageConversion lives in a module we do not reference (it would pull in netstandard 2.1),
        // so call it by reflection.
        private static readonly MethodInfo LoadImageMethod = FindLoadImage();

        private static MethodInfo FindLoadImage()
        {
            System.Type t = System.Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
            return t != null ? t.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) }) : null;
        }

        private static bool LoadImage(Texture2D tex, byte[] bytes)
        {
            return LoadImageMethod != null && (bool)LoadImageMethod.Invoke(null, new object[] { tex, bytes });
        }

        private static Sprite Draw(string key)
        {
            int size = GlyphPainter.Size;
            float[] alpha = GlyphPainter.Render(key);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.HideAndDontSave;
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++)
                px[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha[i] * 255f));
            tex.SetPixels32(px);
            tex.Apply(false, true);

            Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }
    }
}
