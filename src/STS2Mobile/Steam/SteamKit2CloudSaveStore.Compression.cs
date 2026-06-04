using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly struct CloudUploadPayload
    {
        private const string ZipDataEntryName = "data";

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

        private static byte[] CreateSingleEntryZip(byte[] raw)
        {
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry(ZipDataEntryName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                entryStream.Write(raw, 0, raw.Length);
            }

            return ms.ToArray();
        }

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

    private static bool ShouldDecompressDownloadedFile(
        CCloud_ClientFileDownload_Response result,
        byte[] data
    )
        => result.raw_file_size > 0
            && result.raw_file_size != result.file_size
            && HasZipMagic(data);

    private static bool HasZipMagic(byte[] data)
        => data.Length >= 4
            && data[0] == 0x50
            && data[1] == 0x4B
            && data[2] == 0x03
            && data[3] == 0x04;
}
