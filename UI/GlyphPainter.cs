using System;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Draws the filter symbols as alpha masks (no Unity types, so SortPreview can render them to a
    /// picture). Coordinates are 0..1 with y pointing up; the result is Size*Size coverage values,
    /// row 0 = bottom row.
    /// </summary>
    internal static class GlyphPainter
    {
        public const int Size = 64;

        public static float[] Render(string key)
        {
            var p = new Pic();
            Draw(p, key);
            return p.Alpha;
        }

        private static void Draw(Pic p, string key)
        {
            switch (key)
            {
                case "OneHand":
                    Sword(p, 0.27f, 0.10f);
                    p.Line(0.80f, 0.16f, 0.80f, 0.84f, 0.13f);   // 1
                    p.Line(0.80f, 0.84f, 0.62f, 0.68f, 0.12f);
                    break;

                case "TwoHand":
                    Sword(p, 0.27f, 0.10f);
                    p.Path(0.12f, 0.62f, 0.76f, 0.74f, 0.88f, 0.88f, 0.80f, 0.90f, 0.66f, 0.62f, 0.22f, 0.92f, 0.22f);   // 2
                    break;

                case "Knives":
                    p.Tilt(40f);
                    p.Poly(0.40f, 0.38f, 0.60f, 0.38f, 0.60f, 0.68f, 0.50f, 0.96f, 0.40f, 0.70f);
                    p.Line(0.30f, 0.38f, 0.70f, 0.38f, 0.08f);
                    p.Line(0.50f, 0.38f, 0.50f, 0.08f, 0.13f);
                    break;

                case "Swords":
                    p.Tilt(40f);
                    Sword(p, 0.5f, 0.11f);
                    break;

                case "Clubs":   // hammer / mace
                    p.Tilt(40f);
                    p.Line(0.50f, 0.04f, 0.50f, 0.72f, 0.12f);
                    p.Poly(0.16f, 0.62f, 0.84f, 0.62f, 0.84f, 0.96f, 0.16f, 0.96f);
                    break;

                case "Axes":
                    p.Tilt(40f);
                    p.Line(0.36f, 0.02f, 0.36f, 0.98f, 0.11f);
                    p.HalfDisc(0.36f, 0.70f, 0.27f);                       // blade
                    p.Poly(0.36f, 0.84f, 0.10f, 0.88f, 0.10f, 0.58f, 0.36f, 0.60f);   // back spike
                    break;

                case "Spears":
                    p.Tilt(40f);
                    p.Line(0.50f, 0.02f, 0.50f, 0.66f, 0.09f);
                    p.Poly(0.50f, 1.00f, 0.28f, 0.60f, 0.50f, 0.68f, 0.72f, 0.60f);
                    break;

                case "Polearms":   // halberd
                    p.Tilt(40f);
                    p.Line(0.50f, 0.02f, 0.50f, 0.80f, 0.09f);
                    p.Poly(0.50f, 1.00f, 0.38f, 0.78f, 0.62f, 0.78f);
                    p.HalfDisc(0.50f, 0.58f, 0.30f);
                    p.Poly(0.50f, 0.50f, 0.28f, 0.58f, 0.50f, 0.64f);
                    break;

                case "Bows":
                    Arc(p, 0.06f, 0.50f, 0.58f, -62f, 62f, 0.11f);
                    p.Line(0.33f, 0.98f, 0.33f, 0.02f, 0.04f);          // string
                    p.Line(0.28f, 0.50f, 0.84f, 0.50f, 0.06f);          // arrow
                    p.Poly(0.99f, 0.50f, 0.80f, 0.64f, 0.80f, 0.36f);
                    break;

                case "Crossbows":
                    p.Line(0.50f, 0.04f, 0.50f, 0.82f, 0.13f);          // stock
                    p.Line(0.06f, 0.90f, 0.50f, 0.72f, 0.11f);          // limbs
                    p.Line(0.94f, 0.90f, 0.50f, 0.72f, 0.11f);
                    p.Line(0.06f, 0.90f, 0.50f, 0.36f, 0.04f);          // string
                    p.Line(0.94f, 0.90f, 0.50f, 0.36f, 0.04f);
                    p.Line(0.50f, 0.36f, 0.50f, 0.98f, 0.07f);          // bolt
                    break;

                case "Fists":
                    p.Disc(0.52f, 0.62f, 0.34f);                                       // glove
                    p.Poly(0.30f, 0.06f, 0.74f, 0.06f, 0.74f, 0.28f, 0.30f, 0.28f);   // cuff
                    p.Disc(0.22f, 0.52f, 0.13f);                                       // thumb
                    break;

                case "Bombs":
                    p.Disc(0.44f, 0.38f, 0.32f);
                    p.Line(0.60f, 0.64f, 0.76f, 0.82f, 0.09f);          // fuse
                    p.Disc(0.80f, 0.88f, 0.09f);
                    break;

                case "Staff":
                    p.Tilt(40f);
                    p.Line(0.50f, 0.02f, 0.50f, 0.70f, 0.10f);
                    p.Disc(0.50f, 0.82f, 0.17f);
                    p.Line(0.50f, 0.54f, 0.28f, 0.64f, 0.07f);
                    p.Line(0.50f, 0.54f, 0.72f, 0.64f, 0.07f);
                    break;

                default:
                    p.Disc(0.5f, 0.5f, 0.25f);
                    break;
            }
        }

        private static void Sword(Pic p, float cx, float half)
        {
            p.Poly(cx - half, 0.34f, cx + half, 0.34f, cx + half, 0.82f, cx, 0.97f, cx - half, 0.82f);   // blade
            p.Line(cx - 0.24f, 0.33f, cx + 0.24f, 0.33f, 0.10f);                                        // guard
            p.Line(cx, 0.33f, cx, 0.13f, 0.11f);                                                        // grip
            p.Disc(cx, 0.08f, 0.075f);                                                                    // pommel
        }

        private static void Arc(Pic p, float cx, float cy, float r, float fromDeg, float toDeg, float width)
        {
            const int steps = 16;
            float px = 0f, py = 0f;
            for (int i = 0; i <= steps; i++)
            {
                double a = (fromDeg + (toDeg - fromDeg) * (i / (float)steps)) * Math.PI / 180.0;
                float x = cx + r * (float)Math.Cos(a);
                float y = cy + r * (float)Math.Sin(a);
                if (i > 0)
                    p.Line(px, py, x, y, width);
                px = x;
                py = y;
            }
        }

        // ---- tiny anti-aliased rasterizer -----------------------------------------------------

        private sealed class Pic
        {
            public readonly float[] Alpha = new float[Size * Size];
            private float _cos = 1f, _sin = 0f;

            /// <summary>Everything drawn afterwards is rotated clockwise around the centre.</summary>
            public void Tilt(float degClockwise)
            {
                double a = degClockwise * Math.PI / 180.0;
                _cos = (float)Math.Cos(a);
                _sin = (float)Math.Sin(a);
            }

            /// <summary>Half disc on the right side of x = cx (an axe blade).</summary>
            public void HalfDisc(float cx, float cy, float r)
            {
                const int steps = 14;
                var pts = new float[(steps + 1) * 2];
                for (int i = 0; i <= steps; i++)
                {
                    double a = Math.PI / 2 - Math.PI * i / steps;
                    pts[i * 2] = cx + r * (float)Math.Cos(a);
                    pts[i * 2 + 1] = cy + r * (float)Math.Sin(a);
                }
                Poly(pts);
            }

            public void Disc(float cx, float cy, float r)
            {
                Fill((x, y) => (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r);
            }

            public void Line(float x0, float y0, float x1, float y1, float width)
            {
                float half = width * 0.5f;
                Fill((x, y) => SegmentDistance(x, y, x0, y0, x1, y1) - half);
            }

            /// <summary>Polyline: width, then x,y pairs.</summary>
            public void Path(float width, params float[] pts)
            {
                for (int i = 0; i + 3 < pts.Length; i += 2)
                    Line(pts[i], pts[i + 1], pts[i + 2], pts[i + 3], width);
            }

            /// <summary>Filled polygon through x,y pairs.</summary>
            public void Poly(params float[] pts)
            {
                int n = pts.Length / 2;
                Fill((x, y) =>
                {
                    float d = float.MaxValue;
                    bool inside = false;
                    for (int i = 0, j = n - 1; i < n; j = i++)
                    {
                        float xi = pts[i * 2], yi = pts[i * 2 + 1];
                        float xj = pts[j * 2], yj = pts[j * 2 + 1];
                        d = Math.Min(d, SegmentDistance(x, y, xi, yi, xj, yj));
                        if (((yi > y) != (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi))
                            inside = !inside;
                    }
                    return inside ? -d : d;
                });
            }

            private void Fill(Func<float, float, float> signedDistance)
            {
                for (int py = 0; py < Size; py++)
                {
                    for (int px = 0; px < Size; px++)
                    {
                        float dx = (px + 0.5f) / Size - 0.5f;
                        float dy = (py + 0.5f) / Size - 0.5f;
                        // sample point mapped back into the (untilted) drawing
                        float x = _cos * dx - _sin * dy + 0.5f;
                        float y = _sin * dx + _cos * dy + 0.5f;
                        float coverage = Clamp01(0.5f - signedDistance(x, y) * Size);
                        int idx = py * Size + px;
                        if (coverage > Alpha[idx])
                            Alpha[idx] = coverage;
                    }
                }
            }

            private static float Clamp01(float v)
            {
                return v < 0f ? 0f : (v > 1f ? 1f : v);
            }

            private static float SegmentDistance(float px, float py, float ax, float ay, float bx, float by)
            {
                float dx = bx - ax, dy = by - ay;
                float len2 = dx * dx + dy * dy;
                float t = len2 > 1e-6f ? Clamp01(((px - ax) * dx + (py - ay) * dy) / len2) : 0f;
                float cx = ax + t * dx, cy = ay + t * dy;
                return (float)Math.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
            }
        }
    }
}
