using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private const string CloudZipDataEntryName = "data";

    private readonly struct CloudUploadPayload
    {
        private CloudUploadPayload(byte[] data, bool compressed)
        {
            Data = data;
            Compressed = compressed;
        }

        private byte[] Data { get; }
        private bool Compressed { get; }

        internal static CloudUploadPayload Raw(byte[] data)
            => new(data, compressed: false);

        internal static CloudUploadPayload CompressedData(byte[] data)
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

    private static CloudUploadPayload CompressCloudFile(byte[] raw)
    {
        var zipped = CreateSingleEntryCloudZip(raw);
        if (zipped.Length >= raw.Length)
            return CloudUploadPayload.Raw(raw);

        return CloudUploadPayload.CompressedData(zipped);
    }

    private static byte[] DecompressCloudFile(byte[] zipData)
    {
        using var archive = new ZipArchive(new MemoryStream(zipData), ZipArchiveMode.Read);
        if (archive.Entries.Count == 0)
            throw new InvalidDataException("Cloud ZIP archive contains no entries");

        var entry = archive.Entries[0];
        using var stream = entry.Open();
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] CreateSingleEntryCloudZip(byte[] raw)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(CloudZipDataEntryName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(raw, 0, raw.Length);
        }

        return ms.ToArray();
    }
}
