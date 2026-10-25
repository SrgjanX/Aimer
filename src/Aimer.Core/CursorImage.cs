namespace Aimer.Core;

/// <summary>
/// One rendered cursor bitmap: 32-bit BGRA pixels (straight alpha, top-down rows)
/// plus the hotspot, the pixel that is the actual click point.
/// </summary>
public sealed class CursorImage
{
    public CursorImage(int width, int height, int hotspotX, int hotspotY, byte[]? pixels = null)
    {
        if (width is < 1 or > 256 || height is < 1 or > 256)
            throw new ArgumentOutOfRangeException(nameof(width), "Cursor images must be 1-256 pixels per side.");
        if (hotspotX < 0 || hotspotX >= width || hotspotY < 0 || hotspotY >= height)
            throw new ArgumentOutOfRangeException(nameof(hotspotX), "Hotspot must lie inside the image.");

        pixels ??= new byte[width * height * 4];
        if (pixels.Length != width * height * 4)
            throw new ArgumentException("Pixel buffer must be width * height * 4 bytes.", nameof(pixels));

        Width = width;
        Height = height;
        HotspotX = hotspotX;
        HotspotY = hotspotY;
        Pixels = pixels;
    }

    public int Width { get; }
    public int Height { get; }
    public int HotspotX { get; }
    public int HotspotY { get; }
    public byte[] Pixels { get; }

    public CursorColor GetPixel(int x, int y)
    {
        var i = (y * Width + x) * 4;
        return new CursorColor(Pixels[i + 2], Pixels[i + 1], Pixels[i], Pixels[i + 3]);
    }

    public void SetPixel(int x, int y, CursorColor color)
    {
        var i = (y * Width + x) * 4;
        Pixels[i] = color.B;
        Pixels[i + 1] = color.G;
        Pixels[i + 2] = color.R;
        Pixels[i + 3] = color.A;
    }

    /// <summary>Fills the rectangle [x0, x1) x [y0, y1), clipped to the image.</summary>
    public void FillRect(int x0, int y0, int x1, int y1, CursorColor color)
    {
        x0 = Math.Max(x0, 0);
        y0 = Math.Max(y0, 0);
        x1 = Math.Min(x1, Width);
        y1 = Math.Min(y1, Height);
        for (var y = y0; y < y1; y++)
            for (var x = x0; x < x1; x++)
                SetPixel(x, y, color);
    }
}

public readonly record struct CursorColor(byte R, byte G, byte B, byte A = 255);
