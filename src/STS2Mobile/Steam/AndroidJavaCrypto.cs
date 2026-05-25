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

    public static byte[] GetRandomBytes(int count)
    {
        if (!OperatingSystem.IsAndroid())
            return System.Security.Cryptography.RandomNumberGenerator.GetBytes(count);

        var app = GetGodotApp();
        if (app == null)
            throw new InvalidOperationException("GodotApp Java bridge is unavailable for random bytes");

        var encoded = (string)app.Call("randomBytesBase64", count);
        if (string.IsNullOrEmpty(encoded))
            throw new InvalidOperationException("Android Java random byte bridge returned an empty response");

        return Convert.FromBase64String(encoded);
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

    public static void ImportSubjectPublicKeyInfo(
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

    public static void ImportParameters(RSA rsa, RSAParameters parameters)
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

    public static RSA CreateRsa()
    {
        if (!OperatingSystem.IsAndroid())
            return RSA.Create();

        return new AndroidRsa();
    }

    public static byte[] RsaEncrypt(RSA rsa, byte[] data, RSAEncryptionPadding padding)
    {
        if (!OperatingSystem.IsAndroid())
            return rsa.Encrypt(data, padding);

        var paddingName = padding == RSAEncryptionPadding.Pkcs1
            ? "PKCS1"
            : padding == RSAEncryptionPadding.OaepSHA1
                ? "OAEP-SHA1"
                : null;

        if (paddingName == null)
            throw new NotSupportedException($"Unsupported RSA padding: {padding}");

        if (!RsaPublicKeys.TryGetValue(rsa, out var key))
            throw new InvalidOperationException("RSA public key was not imported before encryption");

        var app = GetGodotApp();
        if (app == null)
            throw new InvalidOperationException("GodotApp Java bridge is unavailable for RSA encryption");

        var encoded = (string)app.Call(
            "rsaEncryptBase64",
            key.SubjectPublicKeyInfo == null
                ? string.Empty
                : Convert.ToBase64String(key.SubjectPublicKeyInfo),
            key.Modulus == null ? string.Empty : Convert.ToBase64String(key.Modulus),
            key.Exponent == null ? string.Empty : Convert.ToBase64String(key.Exponent),
            Convert.ToBase64String(data),
            paddingName
        );

        if (string.IsNullOrEmpty(encoded))
            throw new InvalidOperationException("Android Java RSA bridge returned an empty response");

        return Convert.FromBase64String(encoded);
    }

    public static byte[] HmacSha1HashData(byte[] key, byte[] source)
    {
        if (!OperatingSystem.IsAndroid())
            return HMACSHA1.HashData(key, source);

        return HmacSha1HashDataAndroid(key, source);
    }

    public static byte[] HmacSha1HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source)
    {
        if (!OperatingSystem.IsAndroid())
            return HMACSHA1.HashData(key, source);

        return HmacSha1HashDataAndroid(key.ToArray(), source.ToArray());
    }

    public static int HmacSha1HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (!OperatingSystem.IsAndroid())
            return HMACSHA1.HashData(key, source, destination);

        var hash = HmacSha1HashDataAndroid(key.ToArray(), source.ToArray());
        if (destination.Length < hash.Length)
            throw new ArgumentException("Destination is too short for HMAC-SHA1 output", nameof(destination));

        hash.CopyTo(destination);
        return hash.Length;
    }

    public static byte[] Sha1HashData(byte[] source)
    {
        if (!OperatingSystem.IsAndroid())
            return SHA1.HashData(source);

        var app = GetGodotApp();
        if (app == null)
            throw new InvalidOperationException("GodotApp Java bridge is unavailable for SHA-1");

        var encoded = (string)app.Call("sha1Base64", Convert.ToBase64String(source));
        if (string.IsNullOrEmpty(encoded))
            throw new InvalidOperationException("Android Java SHA-1 bridge returned an empty response");

        return Convert.FromBase64String(encoded);
    }

    public static byte[] Sha1FileHashData(string path)
    {
        if (!OperatingSystem.IsAndroid())
        {
            using var fs = File.OpenRead(path);
            return SHA1.HashData(fs);
        }

        var app = GetGodotApp();
        if (app == null)
            throw new InvalidOperationException("GodotApp Java bridge is unavailable for file SHA-1");

        var encoded = (string)app.Call("sha1FileBase64", path);
        if (string.IsNullOrEmpty(encoded))
            throw new InvalidOperationException("Android Java file SHA-1 bridge returned an empty response");

        return Convert.FromBase64String(encoded);
    }

    public static Aes CreateAes()
    {
        if (!OperatingSystem.IsAndroid())
            return Aes.Create();

        return new AndroidAes();
    }

    private static byte[] HmacSha1HashDataAndroid(byte[] key, byte[] source)
    {
        var app = GetGodotApp();
        if (app == null)
            throw new InvalidOperationException("GodotApp Java bridge is unavailable for HMAC-SHA1");

        var encoded = (string)app.Call(
            "hmacSha1Base64",
            Convert.ToBase64String(key),
            Convert.ToBase64String(source)
        );

        if (string.IsNullOrEmpty(encoded))
            throw new InvalidOperationException("Android Java HMAC-SHA1 bridge returned an empty response");

        return Convert.FromBase64String(encoded);
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
        if (destination.Length < output.Length)
            throw new ArgumentException("Destination is too short for AES output", nameof(destination));

        output.CopyTo(destination);
        return output.Length;
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
        if (destination.Length < output.Length)
            throw new ArgumentException("Destination is too short for AES output", nameof(destination));

        output.CopyTo(destination);
        return output.Length;
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
        if (destination.Length < output.Length)
            throw new ArgumentException("Destination is too short for AES output", nameof(destination));

        output.CopyTo(destination);
        return output.Length;
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
        if (destination.Length < output.Length)
            throw new ArgumentException("Destination is too short for AES output", nameof(destination));

        output.CopyTo(destination);
        return output.Length;
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

    private static byte[] AesCryptAndroid(
        string operation,
        string mode,
        PaddingMode paddingMode,
        byte[] key,
        ReadOnlySpan<byte> iv,
        ReadOnlySpan<byte> data
    )
    {
        var app = GetGodotApp();
        if (app == null)
            throw new InvalidOperationException("GodotApp Java bridge is unavailable for AES");

        var paddingName = paddingMode == PaddingMode.None
            ? "None"
            : paddingMode == PaddingMode.PKCS7
                ? "PKCS7"
                : null;

        if (paddingName == null)
            throw new NotSupportedException($"Unsupported AES padding: {paddingMode}");

        var encoded = (string)app.Call(
            "aesCryptBase64",
            operation,
            mode,
            paddingName,
            Convert.ToBase64String(key),
            iv.IsEmpty ? string.Empty : Convert.ToBase64String(iv),
            Convert.ToBase64String(data)
        );

        if (string.IsNullOrEmpty(encoded))
            throw new InvalidOperationException("Android Java AES bridge returned an empty response");

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
                var jcw = Engine.GetSingleton("JavaClassWrapper");
                var wrapper = (GodotObject)jcw.Call("wrap", "com.game.sts2launcher.GodotApp");
                _godotApp = (GodotObject)wrapper.Call("getInstance");
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
        public RsaPublicKey(byte[] subjectPublicKeyInfo)
        {
            SubjectPublicKeyInfo = subjectPublicKeyInfo;
        }

        public RsaPublicKey(byte[] modulus, byte[] exponent)
        {
            Modulus = modulus;
            Exponent = exponent;
        }

        public byte[] SubjectPublicKeyInfo { get; }
        public byte[] Modulus { get; }
        public byte[] Exponent { get; }
    }

    private sealed class AndroidRsa : RSA
    {
        public AndroidRsa()
        {
            LegalKeySizesValue = new[] { new KeySizes(384, 16384, 8) };
            KeySizeValue = 2048;
        }

        public void SetPublicKeySize(int bits)
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
        public AndroidAes()
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
