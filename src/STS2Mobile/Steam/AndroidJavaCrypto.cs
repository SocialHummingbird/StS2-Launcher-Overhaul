using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Godot;

namespace STS2Mobile.Steam;

internal static class AndroidJavaCrypto
{
    private static readonly object AppLock = new();
    private static GodotObject _godotApp;
    private static readonly ConditionalWeakTable<RSA, RsaPublicKey> RsaPublicKeys = new();
    private const string AesCryptBase64BridgeMethod = "aesCryptBase64";
    private const string HmacSha1Base64BridgeMethod = "hmacSha1Base64";
    private const string RandomBytesBase64BridgeMethod = "randomBytesBase64";
    private const string RsaEncryptBase64BridgeMethod = "rsaEncryptBase64";
    private const string Sha1Base64BridgeMethod = "sha1Base64";
    private const string Sha1FileBase64BridgeMethod = "sha1FileBase64";

    internal static byte[] GetRandomBytes(int count)
    {
        if (!OperatingSystem.IsAndroid())
            return System.Security.Cryptography.RandomNumberGenerator.GetBytes(count);

        return CallBase64Bridge(
            "random bytes",
            RandomBytesBase64BridgeMethod,
            "Android Java random byte bridge returned an empty response",
            count);
    }

    internal static void FillRandom(Span<byte> destination)
    {
        if (!OperatingSystem.IsAndroid())
        {
            System.Security.Cryptography.RandomNumberGenerator.Fill(destination);
            return;
        }

        GetRandomBytes(destination.Length).CopyTo(destination);
    }

    internal static void ImportSubjectPublicKeyInfo(
        AsymmetricAlgorithm algorithm,
        ReadOnlySpan<byte> source,
        out int bytesRead
    )
    {
        if (!OperatingSystem.IsAndroid())
        {
            algorithm.ImportSubjectPublicKeyInfo(source, out bytesRead);
            return;
        }

        var rsa = (RSA)algorithm;
        RsaPublicKeys.Remove(rsa);
        RsaPublicKeys.Add(rsa, new RsaPublicKey(source.ToArray()));
        if (rsa is AndroidRsa androidRsa)
            androidRsa.SetPublicKeySize(EstimateSubjectPublicKeyInfoSize(source));
        bytesRead = source.Length;
    }

    internal static void ImportParameters(RSA rsa, RSAParameters parameters)
    {
        if (!OperatingSystem.IsAndroid())
        {
            rsa.ImportParameters(parameters);
            return;
        }

        if (parameters.Modulus == null || parameters.Exponent == null)
            throw new ArgumentException("RSA public parameters require modulus and exponent");

        RsaPublicKeys.Remove(rsa);
        RsaPublicKeys.Add(rsa, new RsaPublicKey(parameters.Modulus, parameters.Exponent));
        if (rsa is AndroidRsa androidRsa)
            androidRsa.SetPublicKeySize(parameters.Modulus.Length * 8);
    }

    internal static RSA CreateRsa()
    {
        if (!OperatingSystem.IsAndroid())
            return RSA.Create();

        return new AndroidRsa();
    }

    internal static byte[] RsaEncrypt(RSA rsa, byte[] data, RSAEncryptionPadding padding)
    {
        if (!OperatingSystem.IsAndroid())
            return rsa.Encrypt(data, padding);

        var paddingName = RsaPaddingName(padding);

        if (!RsaPublicKeys.TryGetValue(rsa, out var key))
            throw new InvalidOperationException("RSA public key was not imported before encryption");

        return CallBase64Bridge(
            "RSA encryption",
            RsaEncryptBase64BridgeMethod,
            "Android Java RSA bridge returned an empty response",
            key.SubjectPublicKeyInfo == null
                ? string.Empty
                : Convert.ToBase64String(key.SubjectPublicKeyInfo),
            key.Modulus == null ? string.Empty : Convert.ToBase64String(key.Modulus),
            key.Exponent == null ? string.Empty : Convert.ToBase64String(key.Exponent),
            Convert.ToBase64String(data),
            paddingName
        );
    }

    internal static byte[] HmacSha1HashData(byte[] key, byte[] source)
    {
        if (!OperatingSystem.IsAndroid())
            return HMACSHA1.HashData(key, source);

        return HmacSha1HashDataAndroid(key, source);
    }

    internal static byte[] HmacSha1HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source)
    {
        if (!OperatingSystem.IsAndroid())
            return HMACSHA1.HashData(key, source);

        return HmacSha1HashDataAndroid(key.ToArray(), source.ToArray());
    }

    internal static int HmacSha1HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (!OperatingSystem.IsAndroid())
            return HMACSHA1.HashData(key, source, destination);

        var hash = HmacSha1HashDataAndroid(key.ToArray(), source.ToArray());
        return CopyToDestination(hash, destination, "Destination is too short for HMAC-SHA1 output");
    }

    internal static byte[] Sha1HashData(byte[] source)
    {
        if (!OperatingSystem.IsAndroid())
            return SHA1.HashData(source);

        return CallBase64Bridge(
            "SHA-1",
            Sha1Base64BridgeMethod,
            "Android Java SHA-1 bridge returned an empty response",
            Convert.ToBase64String(source));
    }

    internal static byte[] Sha1FileHashData(string path)
    {
        if (!OperatingSystem.IsAndroid())
        {
            using var fs = File.OpenRead(path);
            return SHA1.HashData(fs);
        }

        return CallBase64Bridge(
            "file SHA-1",
            Sha1FileBase64BridgeMethod,
            "Android Java file SHA-1 bridge returned an empty response",
            path);
    }

    internal static Aes CreateAes()
    {
        if (!OperatingSystem.IsAndroid())
            return Aes.Create();

        return new AndroidAes();
    }

    private static byte[] HmacSha1HashDataAndroid(byte[] key, byte[] source)
    {
        return CallBase64Bridge(
            "HMAC-SHA1",
            HmacSha1Base64BridgeMethod,
            "Android Java HMAC-SHA1 bridge returned an empty response",
            Convert.ToBase64String(key),
            Convert.ToBase64String(source)
        );
    }

    private static int EstimateSubjectPublicKeyInfoSize(ReadOnlySpan<byte> source)
    {
        // SteamKit imports RSA public keys through SPKI. The ASN.1 parser is intentionally
        // minimal: this value is only metadata for callers that inspect RSA.KeySize, while
        // the actual encryption path uses the original SPKI bytes in Android Java.
        for (var i = 0; i < source.Length - 4; i++)
        {
            if (source[i] == 0x02)
            {
                var lengthByte = source[i + 1];
                var length = 0;
                var offset = i + 2;

                if ((lengthByte & 0x80) == 0)
                {
                    length = lengthByte;
                }
                else
                {
                    var lengthBytes = lengthByte & 0x7f;
                    if (lengthBytes == 0 || lengthBytes > 4 || offset + lengthBytes > source.Length)
                        continue;

                    for (var j = 0; j < lengthBytes; j++)
                        length = (length << 8) | source[offset + j];
                    offset += lengthBytes;
                }

                if (length <= 0 || offset + length > source.Length)
                    continue;

                if (source[offset] == 0x00 && length > 1)
                    length--;

                return length * 8;
            }
        }

        return 2048;
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

    private static int CopyToDestination(byte[] source, Span<byte> destination, string tooShortMessage)
    {
        if (destination.Length < source.Length)
            throw new ArgumentException(tooShortMessage, nameof(destination));

        source.CopyTo(destination);
        return source.Length;
    }

    private static string RsaPaddingName(RSAEncryptionPadding padding)
    {
        if (padding == RSAEncryptionPadding.Pkcs1)
            return "PKCS1";

        if (padding == RSAEncryptionPadding.OaepSHA1)
            return "OAEP-SHA1";

        throw new NotSupportedException($"Unsupported RSA padding: {padding}");
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

    private sealed class RsaPublicKey
    {
        private RsaPublicKey(byte[] subjectPublicKeyInfo)
        {
            SubjectPublicKeyInfo = subjectPublicKeyInfo;
        }

        private RsaPublicKey(byte[] modulus, byte[] exponent)
        {
            Modulus = modulus;
            Exponent = exponent;
        }

        private byte[] SubjectPublicKeyInfo { get; }
        private byte[] Modulus { get; }
        private byte[] Exponent { get; }
    }

    private sealed class AndroidRsa : RSA
    {
        private AndroidRsa()
        {
            LegalKeySizesValue = new[] { new KeySizes(384, 16384, 8) };
            KeySizeValue = 2048;
        }

        private void SetPublicKeySize(int bits)
        {
            KeySizeValue = bits;
        }

        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
        {
            throw new NotSupportedException("Android RSA decrypt is not required for SteamKit login");
        }

        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
        {
            return RsaEncrypt(this, data, padding);
        }

        public override bool TryEncrypt(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            RSAEncryptionPadding padding,
            out int bytesWritten
        )
        {
            var encrypted = RsaEncrypt(this, data.ToArray(), padding);
            if (destination.Length < encrypted.Length)
            {
                bytesWritten = 0;
                return false;
            }

            encrypted.CopyTo(destination);
            bytesWritten = encrypted.Length;
            return true;
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            throw new NotSupportedException("Android RSA export is not required for SteamKit login");
        }

        public override void ImportParameters(RSAParameters parameters)
        {
            AndroidJavaCrypto.ImportParameters(this, parameters);
        }

        public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            throw new NotSupportedException("Android RSA signing is not required for SteamKit login");
        }

        public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            throw new NotSupportedException("Android RSA verification is not required for SteamKit login");
        }
    }

    private sealed class AndroidAes : Aes
    {
        private AndroidAes()
        {
            LegalBlockSizesValue = new[] { new KeySizes(128, 128, 0) };
            LegalKeySizesValue = new[] { new KeySizes(128, 256, 64) };
            BlockSizeValue = 128;
            KeySizeValue = 256;
            FeedbackSizeValue = 8;
            ModeValue = CipherMode.CBC;
            PaddingValue = PaddingMode.PKCS7;
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            throw new NotSupportedException("Android AES transforms are routed through the Java bridge");
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            throw new NotSupportedException("Android AES transforms are routed through the Java bridge");
        }

        public override void GenerateIV()
        {
            IVValue = GetRandomBytes(BlockSize / 8);
        }

        public override void GenerateKey()
        {
            KeyValue = GetRandomBytes(KeySize / 8);
        }
    }
}
