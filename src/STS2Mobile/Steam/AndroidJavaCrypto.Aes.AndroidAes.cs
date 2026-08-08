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
        private byte[] _currentIv;
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
            _currentIv = CopyOptional(iv);
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

            var data = TakeTransformBlockData();
            if (data.Length == 0)
                return 0;

            var output = CryptWithCurrentIv(PaddingMode.None, data);
            if (outputBuffer.Length - outputOffset < output.Length)
                throw new ArgumentException("Output buffer is too short for AES output", nameof(outputBuffer));

            Array.Copy(output, 0, outputBuffer, outputOffset, output.Length);
            return output.Length;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            ThrowIfDisposed();
            ValidateInput(inputBuffer, inputOffset, inputCount);

            if (inputCount > 0)
                _buffer.Write(inputBuffer, inputOffset, inputCount);

            var data = _buffer.ToArray();
            _buffer.SetLength(0);
            if (data.Length == 0 && _padding == PaddingMode.None)
                return Array.Empty<byte>();

            return CryptWithCurrentIv(_padding, data);
        }

        private byte[] TakeTransformBlockData()
        {
            var buffered = _buffer.ToArray();
            var processLength = buffered.Length - (buffered.Length % AesBlockSizeBytes);
            if (
                _padding == PaddingMode.PKCS7 &&
                _operation == "decrypt" &&
                processLength == buffered.Length &&
                processLength > 0
            )
            {
                processLength -= AesBlockSizeBytes;
            }

            if (processLength <= 0)
                return Array.Empty<byte>();

            var data = new byte[processLength];
            Array.Copy(buffered, 0, data, 0, processLength);

            _buffer.SetLength(0);
            if (processLength < buffered.Length)
                _buffer.Write(buffered, processLength, buffered.Length - processLength);

            return data;
        }

        private byte[] CryptWithCurrentIv(PaddingMode padding, byte[] data)
        {
            var iv = _mode == "CBC" ? _currentIv : Array.Empty<byte>();
            var output = AesCryptAndroid(_operation, _mode, padding, _key, iv, data);
            UpdateCurrentIv(data, output);
            return output;
        }

        private void UpdateCurrentIv(byte[] input, byte[] output)
        {
            if (_mode != "CBC")
                return;

            if (_operation == "encrypt")
            {
                if (output.Length >= AesBlockSizeBytes)
                    _currentIv = LastBlock(output);
                return;
            }

            if (input.Length >= AesBlockSizeBytes)
                _currentIv = LastBlock(input);
        }

        private static byte[] LastBlock(byte[] data)
        {
            var block = new byte[AesBlockSizeBytes];
            Array.Copy(
                data,
                data.Length - AesBlockSizeBytes,
                block,
                0,
                AesBlockSizeBytes
            );
            return block;
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
