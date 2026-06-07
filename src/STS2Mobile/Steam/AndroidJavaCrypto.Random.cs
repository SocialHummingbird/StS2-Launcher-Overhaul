using System;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    private const string RandomBytesBase64BridgeMethod = "randomBytesBase64";

    public static byte[] GetRandomBytes(int count)
    {
        if (!OperatingSystem.IsAndroid())
            return System.Security.Cryptography.RandomNumberGenerator.GetBytes(count);

        return CallBase64Bridge(
            "random bytes",
            RandomBytesBase64BridgeMethod,
            "Android Java random byte bridge returned an empty response",
            count.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
}
