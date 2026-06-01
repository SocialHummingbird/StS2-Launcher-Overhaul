using System.IO;
using System.IO.Compression;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private const string CloudZipDataEntryName = "data";

    private static (byte[] Data, bool Compressed) CompressCloudFile(byte[] raw)
    {
        var zipped = CreateSingleEntryCloudZip(raw);
        if (zipped.Length >= raw.Length)
            return (Data: raw, Compressed: false);

        return (Data: zipped, Compressed: true);
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
