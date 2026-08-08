using System;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    private const string AesCryptBase64BridgeMethod = "aesCryptBase64";

    private static byte[] AesCryptAndroid(
        string operation,
        string mode,
        PaddingMode paddingMode,
        byte[] key,
        ReadOnlySpan<byte> iv,
        ReadOnlySpan<byte> data
    )
    {
        var paddingName = AesPaddingName(paddingMode);

        return CallBase64Bridge(
            "AES",
            AesCryptBase64BridgeMethod,
            "Android Java AES bridge returned an empty response",
            operation,
            mode,
            paddingName,
            Convert.ToBase64String(key),
            iv.IsEmpty ? string.Empty : Convert.ToBase64String(iv),
            Convert.ToBase64String(data)
        );
    }

    private static string AesPaddingName(PaddingMode paddingMode)
    {
        return paddingMode switch
        {
            PaddingMode.None => "None",
            PaddingMode.PKCS7 => "PKCS7",
            _ => throw new NotSupportedException($"Unsupported AES padding: {paddingMode}"),
        };
    }
}
