using System;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    private const string HmacSha1Base64BridgeMethod = "hmacSha1Base64";

    public static byte[] HmacSha1HashData(byte[] key, byte[] source)
    {
        if (!OperatingSystem.IsAndroid())
            return HMACSHA1.HashData(key, source);

        return HmacSha1HashDataAndroid(key, source);
    }

    public static byte[] HmacSha1HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source)
    {
        if (!OperatingSystem.IsAndroid())
            return HMACSHA1.HashData(key, source);

        return HmacSha1HashDataAndroid(key.ToArray(), source.ToArray());
    }

    public static int HmacSha1HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (!OperatingSystem.IsAndroid())
            return HMACSHA1.HashData(key, source, destination);

        var hash = HmacSha1HashDataAndroid(key.ToArray(), source.ToArray());
        return CopyToDestination(hash, destination, "Destination is too short for HMAC-SHA1 output");
    }

    private static byte[] HmacSha1HashDataAndroid(byte[] key, byte[] source)
    {
        return CallBase64Bridge(
            "HMAC-SHA1",
            HmacSha1Base64BridgeMethod,
            "Android Java HMAC-SHA1 bridge returned an empty response",
            Convert.ToBase64String(key),
            Convert.ToBase64String(source)
        );
    }
}
