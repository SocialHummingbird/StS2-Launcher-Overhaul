using System;
using Godot;

namespace STS2Mobile.Steam;

internal static partial class AndroidJavaCrypto
{
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
        params string[] arguments
    )
        => AndroidBridgeDispatcher.Run(
            () => CallBase64BridgeOnMainThread(
                operationName,
                methodName,
                emptyResponseMessage,
                arguments
            )
        );

    private static byte[] CallBase64BridgeOnMainThread(
        string operationName,
        string methodName,
        string emptyResponseMessage,
        string[] arguments
    )
    {
        if (!TryGetGodotApp(out var app))
            throw new InvalidOperationException($"GodotApp Java bridge is unavailable for {operationName}");

        var encoded = (string)app.Call(methodName, CreateVariantArguments(arguments));
        if (string.IsNullOrEmpty(encoded))
            throw new InvalidOperationException(emptyResponseMessage);

        return Convert.FromBase64String(encoded);
    }

    private static Variant[] CreateVariantArguments(string[] arguments)
    {
        var variants = new Variant[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
            variants[i] = arguments[i];

        return variants;
    }

    private static bool TryGetGodotApp(out Godot.GodotObject godotApp)
    {
        try
        {
            return AndroidGodotAppBridge.TryGetInstance(out godotApp);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Auth] Java crypto bridge unavailable: {ex.Message}");
            godotApp = null;
            return false;
        }
    }
}
