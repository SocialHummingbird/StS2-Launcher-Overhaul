using System;
using System.IO;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    private const string Sha256Base64BridgeMethod = "sha256Base64";
    private const string Sha256FileBase64BridgeMethod = "sha256FileBase64";

    internal static byte[] Sha256HashData(ReadOnlySpan<byte> data)
    {
        if (!OperatingSystem.IsAndroid())
            return SHA256.HashData(data);

        return CallBase64Bridge(
            "SHA-256",
            Sha256Base64BridgeMethod,
            "Android Java SHA-256 bridge returned an empty response",
            Convert.ToBase64String(data));
    }

    internal static byte[] Sha256FileHashData(string path)
    {
        if (!OperatingSystem.IsAndroid())
        {
            using var fs = File.OpenRead(path);
            return SHA256.HashData(fs);
        }

        return CallBase64Bridge(
            "file SHA-256",
            Sha256FileBase64BridgeMethod,
            "Android Java file SHA-256 bridge returned an empty response",
            path);
    }
}
