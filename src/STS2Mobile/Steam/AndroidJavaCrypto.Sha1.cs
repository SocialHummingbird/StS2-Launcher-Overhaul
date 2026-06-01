using System;
using System.IO;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

internal static partial class AndroidJavaCrypto
{
    private const string Sha1Base64BridgeMethod = "sha1Base64";
    private const string Sha1FileBase64BridgeMethod = "sha1FileBase64";

    internal static byte[] Sha1HashData(byte[] source)
    {
        if (!OperatingSystem.IsAndroid())
            return SHA1.HashData(source);

        return CallBase64Bridge(
            "SHA-1",
            Sha1Base64BridgeMethod,
            "Android Java SHA-1 bridge returned an empty response",
            Convert.ToBase64String(source));
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
