using System;
using System.IO;
using System.Security.Cryptography;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    private sealed class AndroidAes : Aes
    {
        internal AndroidAes()
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
            => new AndroidAesTransform("decrypt", Mode, Padding, rgbKey, rgbIV);

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
            => new AndroidAesTransform("encrypt", Mode, Padding, rgbKey, rgbIV);

        public override void GenerateIV()
        {
            IVValue = GetRandomBytes(BlockSize / 8);
        }

        public override void GenerateKey()
        {
            KeyValue = GetRandomBytes(KeySize / 8);
        }
    }

    private sealed class AndroidAesTransform : ICryptoTransform
    {
        private const int AesBlockSizeBytes = 16;

        private readonly string _operation;
        private readonly string _mode;
        private readonly PaddingMode _padding;
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private readonly MemoryStream _buffer = new();
        private bool _disposed;

        internal AndroidAesTransform(
            string operation,
            CipherMode mode,
            PaddingMode padding,
            byte[] key,
            byte[] iv
        )
        {
            _operation = operation;
            _mode = AesModeName(mode);
            _padding = padding;
            _key = CopyRequired(key, nameof(key));
            _iv = CopyOptional(iv);
        }

        public bool CanReuseTransform => false;
        public bool CanTransformMultipleBlocks => true;
        public int InputBlockSize => AesBlockSizeBytes;
        public int OutputBlockSize => AesBlockSizeBytes;

        public void Dispose()
        {
            _disposed = true;
            _buffer.Dispose();
        }

        public int TransformBlock(
            byte[] inputBuffer,
            int inputOffset,
            int inputCount,
            byte[] outputBuffer,
            int outputOffset
        )
        {
            ThrowIfDisposed();
            ValidateInput(inputBuffer, inputOffset, inputCount);
            ValidateOutput(outputBuffer, outputOffset);

            if (inputCount > 0)
                _buffer.Write(inputBuffer, inputOffset, inputCount);

            // Java Cipher is used as a one-shot bridge. Buffering intermediate
            // blocks keeps CryptoStream write paths on the safe Java side too.
            return 0;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            ThrowIfDisposed();
            ValidateInput(inputBuffer, inputOffset, inputCount);

            if (inputCount > 0)
                _buffer.Write(inputBuffer, inputOffset, inputCount);

            var data = _buffer.ToArray();
            _buffer.SetLength(0);
            return AesCryptAndroid(_operation, _mode, _padding, _key, _iv, data);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AndroidAesTransform));
        }

        private static void ValidateInput(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            ArgumentNullException.ThrowIfNull(inputBuffer);
            if (inputOffset < 0 || inputCount < 0 || inputOffset > inputBuffer.Length - inputCount)
                throw new ArgumentOutOfRangeException(nameof(inputOffset));
        }

        private static void ValidateOutput(byte[] outputBuffer, int outputOffset)
        {
            ArgumentNullException.ThrowIfNull(outputBuffer);
            if (outputOffset < 0 || outputOffset > outputBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(outputOffset));
        }

        private static byte[] CopyRequired(byte[] value, string paramName)
        {
            ArgumentNullException.ThrowIfNull(value, paramName);
            return (byte[])value.Clone();
        }

        private static byte[] CopyOptional(byte[] value)
            => value == null ? Array.Empty<byte>() : (byte[])value.Clone();

        private static string AesModeName(CipherMode mode)
        {
            return mode switch
            {
                CipherMode.CBC => "CBC",
                CipherMode.ECB => "ECB",
                _ => throw new NotSupportedException($"Unsupported AES mode: {mode}"),
            };
        }
    }
}
