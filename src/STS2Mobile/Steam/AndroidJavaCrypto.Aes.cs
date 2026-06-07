using System;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    public static Aes CreateAes()
    {
        if (!OperatingSystem.IsAndroid())
            return Aes.Create();

        return new AndroidAes();
    }

    public static int AesEncryptEcb(
        SymmetricAlgorithm algorithm,
        ReadOnlySpan<byte> plaintext,
        Span<byte> destination,
        PaddingMode paddingMode
    )
    {
        if (!OperatingSystem.IsAndroid())
            return algorithm.EncryptEcb(plaintext, destination, paddingMode);

        var output = AesCryptAndroid("encrypt", "ECB", paddingMode, algorithm.Key, ReadOnlySpan<byte>.Empty, plaintext);
        return CopyToDestination(output, destination, "Destination is too short for AES output");
    }

    public static int AesEncryptCbc(
        SymmetricAlgorithm algorithm,
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> iv,
        Span<byte> destination,
        PaddingMode paddingMode
    )
    {
        if (!OperatingSystem.IsAndroid())
            return algorithm.EncryptCbc(plaintext, iv, destination, paddingMode);

        var output = AesCryptAndroid("encrypt", "CBC", paddingMode, algorithm.Key, iv, plaintext);
        return CopyToDestination(output, destination, "Destination is too short for AES output");
    }

    public static int AesDecryptEcb(
        SymmetricAlgorithm algorithm,
        ReadOnlySpan<byte> ciphertext,
        Span<byte> destination,
        PaddingMode paddingMode
    )
    {
        if (!OperatingSystem.IsAndroid())
            return algorithm.DecryptEcb(ciphertext, destination, paddingMode);

        var output = AesCryptAndroid("decrypt", "ECB", paddingMode, algorithm.Key, ReadOnlySpan<byte>.Empty, ciphertext);
        return CopyToDestination(output, destination, "Destination is too short for AES output");
    }

    public static int AesDecryptCbc(
        SymmetricAlgorithm algorithm,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> iv,
        Span<byte> destination,
        PaddingMode paddingMode
    )
    {
        if (!OperatingSystem.IsAndroid())
            return algorithm.DecryptCbc(ciphertext, iv, destination, paddingMode);

        var output = AesCryptAndroid("decrypt", "CBC", paddingMode, algorithm.Key, iv, ciphertext);
        return CopyToDestination(output, destination, "Destination is too short for AES output");
    }

    public static byte[] AesDecryptCbc(
        SymmetricAlgorithm algorithm,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> iv,
        PaddingMode paddingMode
    )
    {
        if (!OperatingSystem.IsAndroid())
            return algorithm.DecryptCbc(ciphertext, iv, paddingMode);

        return AesCryptAndroid("decrypt", "CBC", paddingMode, algorithm.Key, iv, ciphertext);
    }
}
