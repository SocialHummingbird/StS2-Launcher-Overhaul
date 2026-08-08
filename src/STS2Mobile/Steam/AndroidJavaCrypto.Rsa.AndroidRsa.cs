using System;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    private sealed class AndroidRsa : RSA
    {
        internal AndroidRsa()
        {
            LegalKeySizesValue = new[] { new KeySizes(384, 16384, 8) };
            KeySizeValue = 2048;
        }

        internal void SetPublicKeySize(int bits)
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

        public override bool VerifyHash(
            byte[] hash,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding
        )
        {
            throw new NotSupportedException("Android RSA verification is not required for SteamKit login");
        }
    }
}
