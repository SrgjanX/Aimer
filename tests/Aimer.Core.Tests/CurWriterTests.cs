using System.Buffers.Binary;
using Aimer.Core;
using Aimer.Core.Formats;

namespace Aimer.Core.Tests;

public class CurWriterTests
{
    [Fact]
    public void WritesHeaderEntriesAndHotspots()
    {
        var images = new[] { new CursorImage(32, 32, 5, 7), new CursorImage(256, 256, 128, 128) };
        var bytes = Write(images);

        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(0)));
        Assert.Equal(2, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(2)));
        Assert.Equal(2, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(4)));

        var first = bytes.AsSpan(6, 16);
        Assert.Equal(32, first[0]);
        Assert.Equal(5, BinaryPrimitives.ReadUInt16LittleEndian(first[4..]));
        Assert.Equal(7, BinaryPrimitives.ReadUInt16LittleEndian(first[6..]));

        var second = bytes.AsSpan(22, 16);
        Assert.Equal(0, second[0]); // 256 is stored as 0

        // The last image ends exactly at the end of the file.
        var size = BinaryPrimitives.ReadInt32LittleEndian(second[8..]);
        var offset = BinaryPrimitives.ReadInt32LittleEndian(second[12..]);
        Assert.Equal(bytes.Length, offset + size);
    }

    [Fact]
    public void StoresRowsBottomUpWithTransparencyMask()
    {
        var image = new CursorImage(32, 2, 0, 0);
        image.SetPixel(0, 0, new CursorColor(255, 0, 0)); // top-left red, rest transparent
        var bytes = Write([image]);

        var dib = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(6 + 12));
        Assert.Equal(4, BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(dib + 8))); // height * 2

        // Top row is stored second: BGRA of red.
        var topRow = dib + 40 + 32 * 4;
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, bytes[topRow..(topRow + 4)]);

        // AND mask, top row second: first pixel opaque (bit 0), the rest transparent.
        var maskTopRow = dib + 40 + 32 * 2 * 4 + 4;
        Assert.Equal(0x7F, bytes[maskTopRow]);
    }

    private static byte[] Write(CursorImage[] images)
    {
        using var stream = new MemoryStream();
        CurWriter.Write(stream, images);
        return stream.ToArray();
    }
}
