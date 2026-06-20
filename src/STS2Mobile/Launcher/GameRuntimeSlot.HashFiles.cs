using System;
using System.IO;
using System.Security.Cryptography;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    private static string Sha256OrMissing(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return "<missing>";

            byte[] hash;
            if (OperatingSystem.IsAndroid())
            {
                hash = AndroidJavaCrypto.Sha256FileHashData(path);
            }
            else
            {
                using var stream = File.OpenRead(path);
                hash = SHA256.HashData(stream);
            }
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            return $"<unavailable:{ex.GetType().Name}>";
        }
    }

    private static bool HasUsableHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64)
            return false;

        foreach (var c in value)
        {
            if (!Uri.IsHexDigit(c))
                return false;
        }

        return true;
    }
}
