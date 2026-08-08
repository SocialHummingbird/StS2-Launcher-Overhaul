using System;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    public static byte[] RsaEncrypt(RSA rsa, byte[] data, RSAEncryptionPadding padding)
    {
        if (!OperatingSystem.IsAndroid())
            return rsa.Encrypt(data, padding);

        var paddingName = RsaPaddingName(padding);

        if (!RsaPublicKeys.TryGetValue(rsa, out var key))
            throw new InvalidOperationException("RSA public key was not imported before encryption");

        var keyArguments = key.ToBridgeArguments();
        return CallBase64Bridge(
            "RSA encryption",
            RsaEncryptBase64BridgeMethod,
            "Android Java RSA bridge returned an empty response",
            keyArguments.SubjectPublicKeyInfo,
            keyArguments.Modulus,
            keyArguments.Exponent,
            Convert.ToBase64String(data),
            paddingName
        );
    }

    private static string RsaPaddingName(RSAEncryptionPadding padding)
    {
        if (padding == RSAEncryptionPadding.Pkcs1)
            return "PKCS1";

        if (padding == RSAEncryptionPadding.OaepSHA1)
            return "OAEP-SHA1";

        throw new NotSupportedException($"Unsupported RSA padding: {padding}");
    }
}
