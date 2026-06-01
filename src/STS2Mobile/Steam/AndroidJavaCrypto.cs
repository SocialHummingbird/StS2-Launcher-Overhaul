using System;
using Godot;

namespace STS2Mobile.Steam;

internal static partial class AndroidJavaCrypto
{
    private static readonly object AppLock = new();
    private static GodotObject _godotApp;

    private static int CopyToDestination(byte[] source, Span<byte> destination, string tooShortMessage)
    {
        if (destination.Length < source.Length)
            throw new ArgumentException(tooShortMessage, nameof(destination));

        source.CopyTo(destination);
        return source.Length;
    }

    private static byte[] CallBase64Bridge(
        string operationName,
        string methodName,
        string emptyResponseMessage,
        params Variant[] arguments
    )
    {
        var app = GetGodotApp();
        if (app == null)
            throw new InvalidOperationException($"GodotApp Java bridge is unavailable for {operationName}");

        var encoded = (string)app.Call(methodName, arguments);
        if (string.IsNullOrEmpty(encoded))
            throw new InvalidOperationException(emptyResponseMessage);

        return Convert.FromBase64String(encoded);
    }

    private static GodotObject GetGodotApp()
    {
        lock (AppLock)
        {
            if (_godotApp != null)
                return _godotApp;

            try
            {
                if (!AndroidGodotAppBridge.TryGetInstance(out _godotApp))
                    return null;

                return _godotApp;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Auth] Java crypto bridge unavailable: {ex.Message}");
                return null;
            }
        }
    }
}
