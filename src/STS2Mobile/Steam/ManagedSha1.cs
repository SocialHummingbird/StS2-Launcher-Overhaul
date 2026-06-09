using System;

namespace STS2Mobile.Steam;

internal static class ManagedSha1
{
    internal static byte[] Hash(ReadOnlySpan<byte> data)
    {
        uint h0 = 0x67452301;
        uint h1 = 0xEFCDAB89;
        uint h2 = 0x98BADCFE;
        uint h3 = 0x10325476;
        uint h4 = 0xC3D2E1F0;
        Span<uint> w = stackalloc uint[80];
        var fullBlocks = data.Length / 64;

        for (var block = 0; block < fullBlocks; block++)
            ProcessBlock(data.Slice(block * 64, 64), w, ref h0, ref h1, ref h2, ref h3, ref h4);

        Span<byte> tail = stackalloc byte[128];
        var tailLength = data.Length - fullBlocks * 64;
        data.Slice(fullBlocks * 64).CopyTo(tail);
        tail[tailLength] = 0x80;

        var finalLength = tailLength + 1;
        var paddedLength = finalLength <= 56 ? 64 : 128;
        var bitLength = (ulong)data.Length * 8;
        WriteUInt64BigEndian(tail.Slice(paddedLength - 8, 8), bitLength);

        ProcessBlock(tail.Slice(0, 64), w, ref h0, ref h1, ref h2, ref h3, ref h4);
        if (paddedLength == 128)
            ProcessBlock(tail.Slice(64, 64), w, ref h0, ref h1, ref h2, ref h3, ref h4);

        var hash = new byte[20];
        WriteUInt32BigEndian(hash.AsSpan(0, 4), h0);
        WriteUInt32BigEndian(hash.AsSpan(4, 4), h1);
        WriteUInt32BigEndian(hash.AsSpan(8, 4), h2);
        WriteUInt32BigEndian(hash.AsSpan(12, 4), h3);
        WriteUInt32BigEndian(hash.AsSpan(16, 4), h4);
        return hash;
    }

    private static void ProcessBlock(
        ReadOnlySpan<byte> block,
        Span<uint> w,
        ref uint h0,
        ref uint h1,
        ref uint h2,
        ref uint h3,
        ref uint h4
    )
    {
        for (var i = 0; i < 16; i++)
            w[i] = ReadUInt32BigEndian(block.Slice(i * 4, 4));

        for (var i = 16; i < 80; i++)
            w[i] = RotateLeft(w[i - 3] ^ w[i - 8] ^ w[i - 14] ^ w[i - 16], 1);

        var a = h0;
        var b = h1;
        var c = h2;
        var d = h3;
        var e = h4;

        for (var i = 0; i < 80; i++)
        {
            uint f;
            uint k;
            if (i < 20)
            {
                f = (b & c) | (~b & d);
                k = 0x5A827999;
            }
            else if (i < 40)
            {
                f = b ^ c ^ d;
                k = 0x6ED9EBA1;
            }
            else if (i < 60)
            {
                f = (b & c) | (b & d) | (c & d);
                k = 0x8F1BBCDC;
            }
            else
            {
                f = b ^ c ^ d;
                k = 0xCA62C1D6;
            }

            var temp = RotateLeft(a, 5) + f + e + k + w[i];
            e = d;
            d = c;
            c = RotateLeft(b, 30);
            b = a;
            a = temp;
        }

        h0 += a;
        h1 += b;
        h2 += c;
        h3 += d;
        h4 += e;
    }

    private static uint RotateLeft(uint value, int bits)
        => (value << bits) | (value >> (32 - bits));

    private static uint ReadUInt32BigEndian(ReadOnlySpan<byte> bytes)
        => ((uint)bytes[0] << 24)
            | ((uint)bytes[1] << 16)
            | ((uint)bytes[2] << 8)
            | bytes[3];

    private static void WriteUInt32BigEndian(Span<byte> destination, uint value)
    {
        destination[0] = (byte)(value >> 24);
        destination[1] = (byte)(value >> 16);
        destination[2] = (byte)(value >> 8);
        destination[3] = (byte)value;
    }

    private static void WriteUInt64BigEndian(Span<byte> destination, ulong value)
    {
        destination[0] = (byte)(value >> 56);
        destination[1] = (byte)(value >> 48);
        destination[2] = (byte)(value >> 40);
        destination[3] = (byte)(value >> 32);
        destination[4] = (byte)(value >> 24);
        destination[5] = (byte)(value >> 16);
        destination[6] = (byte)(value >> 8);
        destination[7] = (byte)value;
    }
}
