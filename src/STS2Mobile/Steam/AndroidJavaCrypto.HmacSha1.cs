using System;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    public static byte[] HmacSha1HashData(byte[] key, byte[] source)
    {
        if (!OperatingSystem.IsAndroid())
            return HMACSHA1.HashData(key, source);

        return HmacSha1HashDataManaged(key, source);
    }

    public static byte[] HmacSha1HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source)
    {
        if (!OperatingSystem.IsAndroid())
            return HMACSHA1.HashData(key, source);

        return HmacSha1HashDataManaged(key, source);
    }

    public static int HmacSha1HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (!OperatingSystem.IsAndroid())
            return HMACSHA1.HashData(key, source, destination);

        var hash = HmacSha1HashDataManaged(key, source);
        return CopyToDestination(hash, destination, "Destination is too short for HMAC-SHA1 output");
    }

    private static byte[] HmacSha1HashDataManaged(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source)
    {
        const int BlockSize = 64;
        if (key.Length > BlockSize)
            key = ManagedSha1.Hash(key);

        var inner = new byte[BlockSize + source.Length];
        var outer = new byte[BlockSize + 20];
        for (var i = 0; i < BlockSize; i++)
        {
            var value = i < key.Length ? key[i] : (byte)0;
            inner[i] = (byte)(value ^ 0x36);
            outer[i] = (byte)(value ^ 0x5c);
        }

        source.CopyTo(inner.AsSpan(BlockSize));
        var innerHash = ManagedSha1.Hash(inner);
        innerHash.CopyTo(outer.AsSpan(BlockSize));
        return ManagedSha1.Hash(outer);
    }
}
