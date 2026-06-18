using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private readonly struct CloudUploadPayload
    {
        private CloudUploadPayload(byte[] data, bool compressed)
        {
            Data = data;
            Compressed = compressed;
        }

        private byte[] Data { get; }
        private bool Compressed { get; }

        internal static CloudUploadPayload FromRaw(byte[] raw)
        {
            var zipped = CreateSingleEntryZip(raw);
            if (zipped.Length >= raw.Length)
                return Raw(raw);

            return CompressedData(zipped);
        }

        private static CloudUploadPayload Raw(byte[] data)
            => new(data, compressed: false);

        private static CloudUploadPayload CompressedData(byte[] data)
            => new(data, compressed: true);

        internal void LogUploadStart(string path, uint rawSize)
        {
            PatchHelper.Log(
                Compressed
                    ? SteamKit2CloudSaveStore.Compressed(path, rawSize, UploadSize())
                    : UploadingUncompressed(path, rawSize)
            );
        }

        internal void LogUploadComplete(string path, int rawBytes)
            => PatchHelper.Log(Wrote(path, rawBytes, Compressed));

        internal Task SendBlocksAsync(Func<byte[], Task> sendBlocks)
            => sendBlocks(Data);

        internal int UploadSize()
            => Data.Length;
    }
}
