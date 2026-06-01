using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private async Task WriteDepotFileChunksAsync(
        DepotManifest.FileData file,
        uint depotId,
        byte[] depotKey,
        DepotFileTarget target,
        CancellationToken ct
    )
    {
        using var fs = target.CreateTempFile();
        foreach (var chunk in file.Chunks.OrderBy(c => c.Offset))
        {
            ct.ThrowIfCancellationRequested();
            if (file.TotalSize == 0 && chunk.Offset == 0 && chunk.UncompressedLength == 0)
                continue;

            target.ValidateChunk(file, chunk);
            await target.WriteChunkAsync(this, fs, depotId, chunk, depotKey);
        }
    }

    private async Task DownloadAndWriteChunkAsync(
        FileStream fs,
        uint depotId,
        DepotManifest.ChunkData chunk,
        byte[] depotKey,
        string fileName
    )
    {
        var chunkLength = checked((int)chunk.UncompressedLength);
        var buffer = ArrayPool<byte>.Shared.Rent(chunkLength);
        try
        {
            int written = await DownloadChunkWithRetriesAsync(
                depotId,
                chunk,
                buffer,
                depotKey,
                fileName
            );

            fs.Seek((long)chunk.Offset, SeekOrigin.Begin);
            fs.Write(buffer, 0, written);

            Interlocked.Add(ref _downloadedBytes, written);
            ReportProgress();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void ValidateChunkSize(string fileName, DepotManifest.ChunkData chunk)
    {
        if (chunk.UncompressedLength <= (ulong)MaxDepotChunkBytes)
            return;

        throw new IOException(
            $"Depot chunk is unexpectedly large for {fileName}: "
                + $"{chunk.UncompressedLength} bytes"
        );
    }
}
