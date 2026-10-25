using Aimer.Core;
using Aimer.Core.Packs;

namespace Aimer.Core.Tests;

public class CrosshairRendererTests
{
    private static readonly CrosshairStyle Style = new() { Length = 6, Gap = 3, Thickness = 2, Outline = 1 };

    [Theory]
    [InlineData(32)]
    [InlineData(48)]
    [InlineData(128)]
    public void HotspotIsCentred(int size)
    {
        var image = CrosshairRenderer.Render(Style, size);
        Assert.Equal(size / 2, image.HotspotX);
        Assert.Equal(size / 2, image.HotspotY);
    }

    [Fact]
    public void DrawsArmsWithGapAndOutline()
    {
        var image = CrosshairRenderer.Render(Style, 32);

        // The 2px centre block spans x = 15..16; the left arm runs from 15 - gap - length to 15 - gap.
        const int lo = 15;
        Assert.Equal(0, image.GetPixel(16, 16).A); // centre is empty without a dot
        Assert.Equal(0, image.GetPixel(lo - 1, 16).A); // inside the gap
        Assert.Equal(Style.Color, image.GetPixel(lo - 3 - 1, 16)); // left arm, just outside the gap
        Assert.Equal(Style.Color, image.GetPixel(lo - 3 - 6, 16)); // left arm's far end
        Assert.Equal(Style.OutlineColor, image.GetPixel(lo - 3 - 6 - 1, 16)); // outline past the arm's end
        Assert.Equal(0, image.GetPixel(0, 0).A);
    }

    [Fact]
    public void CenterDotFillsCentre()
    {
        var image = CrosshairRenderer.Render(Style with { CenterDot = true }, 32);
        Assert.Equal(Style.Color, image.GetPixel(16, 16));
    }

    [Fact]
    public void ThemeCoversArrowAndCrosshairAtEverySize()
    {
        var theme = CrosshairRenderer.RenderTheme(Style, "Test", [32, 64]);
        Assert.Equal([CursorRole.Arrow, CursorRole.Crosshair], theme.Cursors.Keys.Order());
        Assert.All(theme.Cursors.Values, images => Assert.Equal([32, 64], images.Select(i => i.Width)));
    }
}
