using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    private const string Sha1Base64BridgeMethod = "sha1Base64";
    private const string Sha1FileBase64BridgeMethod = "sha1FileBase64";
    private static int Sha1TryHashDataBridgeLogged;

    public static byte[] Sha1HashData(byte[] source)
    {
        if (!OperatingSystem.IsAndroid())
            return SHA1.HashData(source);

        return CallBase64Bridge(
            "SHA-1",
            Sha1Base64BridgeMethod,
            "Android Java SHA-1 bridge returned an empty response",
            Convert.ToBase64String(source));
    }

    public static bool Sha1TryHashData(
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        out int bytesWritten
    )
    {
        if (!OperatingSystem.IsAndroid())
            return SHA1.TryHashData(source, destination, out bytesWritten);

        if (Interlocked.Exchange(ref Sha1TryHashDataBridgeLogged, 1) == 0)
            STS2Mobile.BootstrapTrace.Log("[Auth] Android Java SHA-1 TryHashData bridge active");

        byte[] hash;
        try
        {
            hash = Sha1HashData(source.ToArray());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Android Java SHA-1 TryHashData bridge failed", ex);
        }

        if (destination.Length < hash.Length)
        {
            bytesWritten = 0;
            return false;
        }

        hash.AsSpan().CopyTo(destination);
        bytesWritten = hash.Length;
        return true;
    }

    internal static byte[] Sha1FileHashData(string path)
    {
        if (!OperatingSystem.IsAndroid())
        {
            using var fs = File.OpenRead(path);
            return SHA1.HashData(fs);
        }

        return CallBase64Bridge(
            "file SHA-1",
            Sha1FileBase64BridgeMethod,
            "Android Java file SHA-1 bridge returned an empty response",
            path);
    }
}
