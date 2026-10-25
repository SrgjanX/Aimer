namespace Aimer.Core.Packs;

public static class SwordRenderer
{
    private const double BaseSize = 32;
    private const int Samples = 4; // per axis, for anti-aliasing the diagonal edges

    // Sword geometry in base-size pixels, measured along the blade from the tip (u)
    // and across it (v).
    private const double BladeEnd = 25;
    private const double BladeHalfWidth = 2;
    private const double TipTaper = 0.45; // half-width gained per pixel near the tip
    private const double GuardEnd = 28;
    private const double GuardHalfWidth = 6.5;
    private const double GripEnd = 36;
    private const double GripHalfWidth = 1.4;
    private const double PommelCenter = 38;
    private const double PommelRadius = 2.4;

    public static readonly CursorRole[] Roles = CrosshairRenderer.Roles;

    public static CursorTheme RenderTheme(SwordStyle style, string name, IReadOnlyList<int> sizes)
    {
        IReadOnlyList<CursorImage> images = sizes.Select(size => Render(style, size)).ToArray();
        return new CursorTheme(name, Roles.ToDictionary(role => role, _ => images));
    }

    public static CursorImage Render(SwordStyle style, int size)
    {
        var scale = size / BaseSize;
        var outline = style.Outline > 0 ? Math.Max(1, Math.Round(style.Outline * scale)) / scale : 0;
        var hiltColor = Darken(style.Color, 0.6);

        // The tip sits just inside the top-left corner, leaving room for the outline.
        var tip = Math.Max(outline, 0.5);
        var hotspot = Math.Clamp((int)(tip * scale), 0, size - 1);
        var image = new CursorImage(size, size, hotspot, hotspot);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                double outlineCover = 0, hiltCover = 0, bladeCover = 0;
                for (var sy = 0; sy < Samples; sy++)
                {
                    for (var sx = 0; sx < Samples; sx++)
                    {
                        var px = (x + (sx + 0.5) / Samples) / scale - tip;
                        var py = (y + (sy + 0.5) / Samples) / scale - tip;
                        var u = (px + py) / Math.Sqrt(2);
                        var v = (px - py) / Math.Sqrt(2);

                        if (outline > 0 && (InBlade(u, v, outline) || InHilt(u, v, outline)))
                            outlineCover++;
                        if (InHilt(u, v, 0))
                            hiltCover++;
                        else if (InBlade(u, v, 0))
                            bladeCover++;
                    }
                }

                const double total = Samples * Samples;
                var pixel = new CursorColor(0, 0, 0, 0);
                pixel = Over(style.OutlineColor, outlineCover / total, pixel);
                pixel = Over(hiltColor, hiltCover / total, pixel);
                pixel = Over(style.Color, bladeCover / total, pixel);
                image.SetPixel(x, y, pixel);
            }
        }

        return image;
    }

    private static bool InBlade(double u, double v, double grow) =>
        u >= -grow && u <= BladeEnd + grow
        && Math.Abs(v) <= Math.Min(BladeHalfWidth, Math.Max(0, u) * TipTaper) + grow;

    private static bool InHilt(double u, double v, double grow)
    {
        if (u >= BladeEnd - grow && u <= GuardEnd + grow && Math.Abs(v) <= GuardHalfWidth + grow)
            return true;
        if (u >= GuardEnd - grow && u <= GripEnd + grow && Math.Abs(v) <= GripHalfWidth + grow)
            return true;
        var du = u - PommelCenter;
        return du * du + v * v <= (PommelRadius + grow) * (PommelRadius + grow);
    }

    private static CursorColor Darken(CursorColor color, double factor) => new(
        (byte)Math.Round(color.R * factor), (byte)Math.Round(color.G * factor), (byte)Math.Round(color.B * factor), color.A);

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
