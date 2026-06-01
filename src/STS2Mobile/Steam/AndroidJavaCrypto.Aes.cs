using System;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

internal static partial class AndroidJavaCrypto
{
    internal static Aes CreateAes()
    {
        if (!OperatingSystem.IsAndroid())
            return Aes.Create();

        return new AndroidAes();
    }

    internal static int AesEncryptEcb(
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

    internal static int AesEncryptCbc(
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

    internal static int AesDecryptEcb(
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

    internal static int AesDecryptCbc(
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

    internal static byte[] AesDecryptCbc(
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
