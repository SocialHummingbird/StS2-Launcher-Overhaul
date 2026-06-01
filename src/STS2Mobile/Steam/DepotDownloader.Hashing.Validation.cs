using System.IO;
using System.Linq;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static void ValidateChunkBounds(
        string fileName,
        ulong fileSize,
        DepotManifest.ChunkData chunk
    )
    {
        if (chunk.UncompressedLength == 0)
        {
            throw new IOException(
                $"Depot chunk has zero length for {fileName} at offset {chunk.Offset}"
            );
        }

        if (chunk.Offset > fileSize || chunk.UncompressedLength > fileSize - chunk.Offset)
        {
            throw new IOException(
                $"Depot chunk is outside file bounds for {fileName}: "
                    + $"offset={chunk.Offset}, length={chunk.UncompressedLength}, fileSize={fileSize}"
            );
        }
    }

    private static void ValidateFileChunks(string fileName, DepotManifest.FileData file)
    {
        if (file.TotalSize == 0)
        {
            return;
        }

        if (file.Chunks == null || !file.Chunks.Any())
        {
            throw new IOException($"Depot file has no chunks: {fileName}");
        }
    }
}
