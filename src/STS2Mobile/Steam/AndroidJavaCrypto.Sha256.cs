using System;
using System.IO;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    private const string Sha256FileBase64BridgeMethod = "sha256FileBase64";

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
