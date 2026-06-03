using System.IO;
using System.Text.Json;
using Godot;

namespace STS2Mobile.Steam;

internal static class AndroidEncryptedJsonFile
{
    private const string DecryptStringBridgeMethod = "decryptString";
    private const string EncryptStringBridgeMethod = "encryptString";

    internal static T Load<T>(string path)
        where T : class
    {
        if (!File.Exists(path))
            return null;

        var encrypted = File.ReadAllText(path);
        var json = Decrypt(encrypted);
        return json == null ? null : JsonSerializer.Deserialize<T>(json);
    }

    internal static bool Save<T>(string path, T value)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(value);
        var encrypted = Encrypt(json);
        if (encrypted == null)
            return false;

        File.WriteAllText(path, encrypted);
        return true;
    }

    internal static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }

    private static string Encrypt(string plainText)
        => AndroidBridgeDispatcher.Run(
            () =>
            {
                var godotApp = GetGodotApp();
                return (string)godotApp?.Call(EncryptStringBridgeMethod, plainText);
            }
        );

    private static string Decrypt(string encryptedText)
        => AndroidBridgeDispatcher.Run(
            () =>
            {
                var godotApp = GetGodotApp();
                return (string)godotApp?.Call(DecryptStringBridgeMethod, encryptedText);
            }
        );

    private static GodotObject GetGodotApp()
    {
        return AndroidGodotAppBridge.TryGetInstance(out var godotApp) ? godotApp : null;
    }
}
