using System;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

internal static partial class AndroidJavaCrypto
{
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
