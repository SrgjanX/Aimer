namespace Aimer.Core.Packs;

public static class CrosshairRenderer
{
    private const double BaseSize = 32;

    /// <summary>The scope preset: thin arms reaching well past a ring, stopping short of a dot.</summary>
    public static readonly CrosshairStyle Scope = new()
    {
        Length = 9,
        Gap = 4,
        Thickness = 1,
        CenterDot = true,
        DotSize = 1,
        RingRadius = 7.5,
        RingThickness = 1,
    };

    /// <summary>The roles a crosshair theme replaces; links, text and resize cursors stay normal.</summary>
    public static readonly CursorRole[] Roles = [CursorRole.Arrow, CursorRole.Crosshair];

    public static CursorTheme RenderTheme(CrosshairStyle style, string name, IReadOnlyList<int> sizes)
    {
        IReadOnlyList<CursorImage> images = sizes.Select(size => Render(style, size)).ToArray();
        return new CursorTheme(name, Roles.ToDictionary(role => role, _ => images));
    }

    public static CursorImage Render(CrosshairStyle style, int size)
    {
        var scale = size / BaseSize;
        int Scaled(double value, int min) => Math.Max(min, (int)Math.Round(value * scale));

        var thickness = Scaled(style.Thickness, 1);
        var length = Scaled(style.Length, 1);
        var gap = Scaled(style.Gap, 0);
        var outline = style.Outline > 0 ? Scaled(style.Outline, 1) : 0;

        // The centre block [lo, hi) is the crosshair's core; arms extend out from it.
        var lo = size / 2 - thickness / 2;
        var hi = lo + thickness;

        (int X0, int Y0, int X1, int Y1)[] rects =
        [
            (lo - gap - length, lo, lo - gap, hi), // left
            (hi + gap, lo, hi + gap + length, hi), // right
            (lo, lo - gap - length, hi, lo - gap), // top
            (lo, hi + gap, hi, hi + gap + length), // bottom
        ];
        // The dot is centered on the core, as nearly as whole pixels allow when its size and the thickness differ in parity.
        var center = lo + thickness / 2.0;
        if (style.CenterDot)
        {
            var dot = Scaled(style.DotSize, 1);
            var dlo = (int)Math.Floor(center - dot / 2.0 + 0.5);
            rects = [.. rects, (dlo, dlo, dlo + dot, dlo + dot)];
        }

        var hotspot = Math.Clamp(size / 2, 0, size - 1);
        var image = new CursorImage(size, size, hotspot, hotspot);

        var ringRadius = style.RingRadius * scale;
        var ringHalf = Scaled(style.RingThickness, 1) / 2.0;

        if (outline > 0)
        {
            foreach (var r in rects)
                image.FillRect(r.X0 - outline, r.Y0 - outline, r.X1 + outline, r.Y1 + outline, style.OutlineColor);
            if (ringRadius > 0)
                DrawRing(image, center, ringRadius, ringHalf + outline, style.OutlineColor);
        }
        foreach (var r in rects)
            image.FillRect(r.X0, r.Y0, r.X1, r.Y1, style.Color);
        if (ringRadius > 0)
            DrawRing(image, center, ringRadius, ringHalf, style.Color);

        return image;
    }

    /// <summary>Paints the band within <paramref name="halfWidth"/> of a circle, anti-aliased by supersampling.</summary>
    private static void DrawRing(CursorImage image, double center, double radius, double halfWidth, CursorColor color)
    {
        const int samples = 4; // per axis
        var reach = (int)Math.Ceiling(radius + halfWidth) + 1;
        var x0 = Math.Max(0, (int)center - reach);
        var x1 = Math.Min(image.Width, (int)center + reach + 1);
        var y0 = Math.Max(0, (int)center - reach);
        var y1 = Math.Min(image.Height, (int)center + reach + 1);

        for (var y = y0; y < y1; y++)
        {
            for (var x = x0; x < x1; x++)
            {
                var hits = 0;
                for (var sy = 0; sy < samples; sy++)
                {
                    for (var sx = 0; sx < samples; sx++)
                    {
                        var dx = x + (sx + 0.5) / samples - center;
                        var dy = y + (sy + 0.5) / samples - center;
                        if (Math.Abs(Math.Sqrt(dx * dx + dy * dy) - radius) <= halfWidth)
                            hits++;
                    }
                }
                image.Blend(x, y, color, hits / (double)(samples * samples));
            }
        }
    }
}
