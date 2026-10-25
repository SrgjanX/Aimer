namespace Aimer.Core.Packs;

public static class CrosshairRenderer
{
    private const double BaseSize = 32;

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
        if (style.CenterDot)
            rects = [.. rects, (lo, lo, hi, hi)];

        var hotspot = Math.Clamp(size / 2, 0, size - 1);
        var image = new CursorImage(size, size, hotspot, hotspot);

        if (outline > 0)
        {
            foreach (var r in rects)
                image.FillRect(r.X0 - outline, r.Y0 - outline, r.X1 + outline, r.Y1 + outline, style.OutlineColor);
        }
        foreach (var r in rects)
            image.FillRect(r.X0, r.Y0, r.X1, r.Y1, style.Color);

        return image;
    }
}
