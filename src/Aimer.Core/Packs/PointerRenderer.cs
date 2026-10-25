namespace Aimer.Core.Packs;

public static class PointerRenderer
{
    private const double BaseSize = 32;
    private const int Samples = 4; // per axis, for anti-aliasing the diagonal edges

    public static readonly CursorRole[] Roles = CrosshairRenderer.Roles;

    /// <summary>
    /// One piece of a shape. <paramref name="Distance"/> is a signed distance in base-size pixels
    /// (negative inside); <paramref name="Fill"/> picks its color from the style color.
    /// Glow draws a soft halo of that radius behind everything.
    /// </summary>
    private sealed record Part(
        Func<double, double, double> Distance,
        Func<CursorColor, CursorColor> Fill,
        bool Outlined = true,
        double Glow = 0);

    private static readonly CursorColor DarkGrey = new(70, 70, 75);
    private static readonly CursorColor LightGrey = new(175, 175, 180);
    private static readonly CursorColor Flame = new(255, 140, 0);
    private static readonly CursorColor FlameCore = new(255, 230, 90);

    // Shapes are laid out along the diagonal from the tip: u runs toward the bottom-right,
    // v across it. Later parts are drawn on top of earlier ones.
    private static readonly Dictionary<PointerShape, Part[]> Shapes = new()
    {
        [PointerShape.Arrow] =
        [
            // The classic pointer, given in plain x/y.
            new(Polygon(Xy((0, 0), (0, 17), (4.2, 13.2), (7.2, 20), (10, 18.8), (7.1, 12.2), (12.5, 12.2))), c => c),
        ],
        [PointerShape.Sword] =
        [
            new(Polygon((0, 0), (4.4, 2), (25, 2), (25, -2), (4.4, -2)), c => c),
            new(Box(25, 28, 6.5), c => Shade(c, 0.6)),
            new(Box(28, 36, 1.4), c => Shade(c, 0.45)),
            new(Circle(38, 2.4), c => Shade(c, 0.6)),
        ],
        [PointerShape.Dagger] =
        [
            new(Polygon((0, 0), (3.5, 1.6), (16, 1.6), (16, -1.6), (3.5, -1.6)), c => c),
            new(Box(16, 18, 4.5), c => Shade(c, 0.6)),
            new(Box(18, 24, 1.2), c => Shade(c, 0.45)),
            new(Circle(25.5, 1.8), c => Shade(c, 0.6)),
        ],
        [PointerShape.Lightsaber] =
        [
            new(Capsule(1.6, 24, 1.6), c => Tint(c, 0.75), Outlined: false, Glow: 3.5),
            new(Box(24, 34, 2), _ => DarkGrey),
            new(Box(24, 25.6, 2.5), _ => LightGrey),
            new(Box(28, 29, 2.2), _ => LightGrey),
            new(Box(30.5, 31.5, 2.2), _ => LightGrey),
            new(Box(33, 34, 2.3), _ => LightGrey),
        ],
        [PointerShape.Rocket] =
        [
            new(Polygon((21, 1.8), (28.5, 0), (21, -1.8)), _ => Flame, Outlined: false),
            new(Polygon((21, 1), (25.5, 0), (21, -1)), _ => FlameCore, Outlined: false),
            new(Polygon((13, 3), (19.5, 7), (21, 7), (19, 3)), c => Shade(c, 0.6)),
            new(Polygon((13, -3), (19.5, -7), (21, -7), (19, -3)), c => Shade(c, 0.6)),
            new(Box(19, 21, 2), _ => DarkGrey),
            new(Polygon((0, 0), (3, 1.9), (6.5, 3.2), (19, 3.2), (19, -3.2), (6.5, -3.2), (3, -1.9)), c => c),
            new(Polygon((0, 0), (3, 1.9), (5, 2.8), (5, -2.8), (3, -1.9)), c => Shade(c, 0.6)),
            new(Circle(9.5, 1.7), _ => DarkGrey),
            new(Circle(9.5, 1.1), c => Tint(c, 0.7)),
        ],
    };

    public static CursorTheme RenderTheme(PointerStyle style, string name, IReadOnlyList<int> sizes)
    {
        IReadOnlyList<CursorImage> images = sizes.Select(size => Render(style, size)).ToArray();
        return new CursorTheme(name, Roles.ToDictionary(role => role, _ => images));
    }

    public static CursorImage Render(PointerStyle style, int size)
    {
        var parts = Shapes[style.Shape];
        var scale = size / BaseSize;
        var outline = style.Outline > 0 ? Math.Max(1, Math.Round(style.Outline * scale)) / scale : 0;
        var fills = parts.Select(p => p.Fill(style.Color)).ToArray();

        // The tip sits just inside the top-left corner, leaving room for the outline,
        // and further in for shapes with a glow so the halo is not cut off.
        var tip = Math.Max(outline, 0.5) + parts.Max(p => p.Glow) / Math.Sqrt(2);
        var hotspot = Math.Clamp((int)(tip * scale), 0, size - 1);
        var image = new CursorImage(size, size, hotspot, hotspot);

        var glowCover = new double[parts.Length];
        var partCover = new double[parts.Length];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                double outlineCover = 0;
                Array.Clear(glowCover);
                Array.Clear(partCover);

                for (var sy = 0; sy < Samples; sy++)
                {
                    for (var sx = 0; sx < Samples; sx++)
                    {
                        var px = (x + (sx + 0.5) / Samples) / scale - tip;
                        var py = (y + (sy + 0.5) / Samples) / scale - tip;
                        var u = (px + py) / Math.Sqrt(2);
                        var v = (px - py) / Math.Sqrt(2);

                        var inOutline = false;
                        var top = -1; // only the topmost part covering a sample counts
                        for (var i = 0; i < parts.Length; i++)
                        {
                            var d = parts[i].Distance(u, v);
                            if (d <= 0)
                                top = i;
                            if (parts[i].Outlined && outline > 0 && d <= outline)
                                inOutline = true;
                            if (parts[i].Glow > 0 && d > 0 && d < parts[i].Glow)
                                glowCover[i] += Math.Pow(1 - d / parts[i].Glow, 2);
                        }
                        if (inOutline)
                            outlineCover++;
                        if (top >= 0)
                            partCover[top]++;
                    }
                }

                const double total = Samples * Samples;
                var pixel = new CursorColor(0, 0, 0, 0);
                for (var i = 0; i < parts.Length; i++)
                    pixel = Over(style.Color, glowCover[i] / total * 0.8, pixel);
                pixel = Over(style.OutlineColor, outlineCover / total, pixel);
                for (var i = 0; i < parts.Length; i++)
                    pixel = Over(fills[i], partCover[i] / total, pixel);
                image.SetPixel(x, y, pixel);
            }
        }

        return image;
    }

    private static (double U, double V)[] Xy(params (double X, double Y)[] points) =>
        points.Select(p => ((p.X + p.Y) / Math.Sqrt(2), (p.X - p.Y) / Math.Sqrt(2))).ToArray();

    private static Func<double, double, double> Box(double u0, double u1, double halfWidth) =>
        Polygon((u0, -halfWidth), (u1, -halfWidth), (u1, halfWidth), (u0, halfWidth));

    private static Func<double, double, double> Circle(double u, double radius) =>
        (pu, pv) => Math.Sqrt((pu - u) * (pu - u) + pv * pv) - radius;

    /// <summary>A rounded bar along the axis from u0 to u1.</summary>
    private static Func<double, double, double> Capsule(double u0, double u1, double radius) =>
        (pu, pv) =>
        {
            var du = pu - Math.Clamp(pu, u0, u1);
            return Math.Sqrt(du * du + pv * pv) - radius;
        };

    /// <summary>Exact signed distance to a simple polygon.</summary>
    private static Func<double, double, double> Polygon(params (double U, double V)[] points) =>
        (pu, pv) =>
        {
            var best = double.MaxValue;
            var sign = 1.0;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                var (au, av) = points[i];
                var (eu, ev) = (points[j].U - au, points[j].V - av);
                var (wu, wv) = (pu - au, pv - av);
                var t = Math.Clamp((wu * eu + wv * ev) / (eu * eu + ev * ev), 0, 1);
                var (bu, bv) = (wu - eu * t, wv - ev * t);
                best = Math.Min(best, bu * bu + bv * bv);

                var c1 = pv >= av;
                var c2 = pv < points[j].V;
                var c3 = eu * wv > ev * wu;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3))
                    sign = -sign;
            }
            return sign * Math.Sqrt(best);
        };

    private static CursorColor Shade(CursorColor color, double factor) => new(
        (byte)Math.Round(color.R * factor), (byte)Math.Round(color.G * factor), (byte)Math.Round(color.B * factor), color.A);

    /// <summary>Blends the color toward white, for glowing or glassy parts.</summary>
    private static CursorColor Tint(CursorColor color, double amount)
    {
        byte Lift(byte c) => (byte)Math.Round(c + (255 - c) * amount);
        return new(Lift(color.R), Lift(color.G), Lift(color.B), color.A);
    }

    /// <summary>Paints <paramref name="color"/> at the given coverage over a straight-alpha pixel.</summary>
    private static CursorColor Over(CursorColor color, double coverage, CursorColor dest)
    {
        if (coverage <= 0)
            return dest;

        var srcA = coverage * color.A / 255.0;
        var dstA = dest.A / 255.0;
        var outA = srcA + dstA * (1 - srcA);
        byte Mix(byte s, byte d) => (byte)Math.Round((s * srcA + d * dstA * (1 - srcA)) / outA);
        return new CursorColor(Mix(color.R, dest.R), Mix(color.G, dest.G), Mix(color.B, dest.B), (byte)Math.Round(outA * 255));
    }
}
