using System;

namespace STS2Mobile.Steam;

internal static class ValidationJava
{
    internal static string Call(string method, string argument = null)
        => AndroidBridgeDispatcher.Run(() =>
        {
            if (!AndroidGodotAppBridge.TryGetInstance(out var app, "[DownloadValidation] Java bridge unavailable"))
                throw new InvalidOperationException("Java diagnostic bridge unavailable.");
            return (string)(argument == null ? app.Call(method) : app.Call(method, argument));
        });
}
