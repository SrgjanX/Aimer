using System.Buffers.Binary;

namespace Aimer.Core.Formats;

/// <summary>
/// Writes Windows .cur files: an ICONDIR header followed by one 32-bit DIB per size.
/// </summary>
public static class CurWriter
{
    private const int DirHeaderSize = 6;
    private const int DirEntrySize = 16;
    private const int BitmapInfoHeaderSize = 40;

    public static void Write(Stream stream, IReadOnlyList<CursorImage> images)
    {
        if (images.Count is 0 or > ushort.MaxValue)
            throw new ArgumentException("A .cur file needs between 1 and 65535 images.", nameof(images));

        var dibs = images.Select(EncodeDib).ToArray();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        writer.Write((ushort)0); // reserved
        writer.Write((ushort)2); // type: cursor
        writer.Write((ushort)images.Count);

        var offset = DirHeaderSize + DirEntrySize * images.Count;
        for (var i = 0; i < images.Count; i++)
        {
            var image = images[i];
            writer.Write((byte)(image.Width >= 256 ? 0 : image.Width)); // 0 means 256
            writer.Write((byte)(image.Height >= 256 ? 0 : image.Height));
            writer.Write((byte)0); // palette size
            writer.Write((byte)0); // reserved
            writer.Write((ushort)image.HotspotX);
            writer.Write((ushort)image.HotspotY);
            writer.Write(dibs[i].Length);
            writer.Write(offset);
            offset += dibs[i].Length;
        }

        foreach (var dib in dibs)
            writer.Write(dib);
    }

    private static byte[] EncodeDib(CursorImage image)
    {
        int width = image.Width, height = image.Height;
        var xorSize = width * height * 4;
        var maskStride = (width + 31) / 32 * 4;
        var maskSize = maskStride * height;
        var dib = new byte[BitmapInfoHeaderSize + xorSize + maskSize];
        var span = dib.AsSpan();

        BinaryPrimitives.WriteInt32LittleEndian(span[0..], BitmapInfoHeaderSize);
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], width);
        BinaryPrimitives.WriteInt32LittleEndian(span[8..], height * 2); // XOR + AND mask
        BinaryPrimitives.WriteUInt16LittleEndian(span[12..], 1); // planes
        BinaryPrimitives.WriteUInt16LittleEndian(span[14..], 32); // bits per pixel
        BinaryPrimitives.WriteInt32LittleEndian(span[20..], xorSize + maskSize);

        // DIB rows are stored bottom-up.
        var rowBytes = width * 4;
        var maskStart = BitmapInfoHeaderSize + xorSize;
        for (var y = 0; y < height; y++)
        {
            var srcRow = height - 1 - y;
            image.Pixels.AsSpan(srcRow * rowBytes, rowBytes).CopyTo(span[(BitmapInfoHeaderSize + y * rowBytes)..]);

            // AND mask bit 1 = transparent; kept in sync with alpha for older renderers.
            for (var x = 0; x < width; x++)
            {
                if (image.Pixels[(srcRow * width + x) * 4 + 3] == 0)
                    dib[maskStart + y * maskStride + x / 8] |= (byte)(0x80 >> (x % 8));
            }
        }

        return dib;
    }
}
