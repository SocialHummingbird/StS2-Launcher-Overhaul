using System;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    private const string RandomBytesBase64BridgeMethod = "randomBytesBase64";

    public static byte[] GetRandomBytes(int count)
    {
        if (!OperatingSystem.IsAndroid())
            return System.Security.Cryptography.RandomNumberGenerator.GetBytes(count);

        return CallRandomBytesBridge(count);
    }

    public static void FillRandom(Span<byte> destination)
    {
        if (!OperatingSystem.IsAndroid())
        {
            System.Security.Cryptography.RandomNumberGenerator.Fill(destination);
            return;
        }

        GetRandomBytes(destination.Length).CopyTo(destination);
    }

    private static byte[] CallRandomBytesBridge(int count)
        => AndroidBridgeDispatcher.Run(
            () =>
            {
                if (
                    !AndroidGodotAppBridge.TryGetInstance(
                        out var app,
                        "[Auth] Java crypto bridge unavailable"
                    )
                )
                {
                    throw new InvalidOperationException(
                        "GodotApp Java bridge is unavailable for random bytes"
                    );
                }

                var encoded = (string)app.Call(RandomBytesBase64BridgeMethod, count);
                if (string.IsNullOrEmpty(encoded))
                    throw new InvalidOperationException(
                        "Android Java random byte bridge returned an empty response"
                    );

                return Convert.FromBase64String(encoded);
            }
        );
}
