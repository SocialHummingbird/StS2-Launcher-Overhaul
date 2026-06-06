using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct ChunkWriteRequest
    {
        private ChunkWriteRequest(
            FileStream stream,
            uint depotId,
            DepotManifest.ChunkData chunk,
            byte[] depotKey,
            string fileName
        )
        {
            Stream = stream;
            DepotId = depotId;
            Chunk = chunk;
            DepotKey = depotKey;
            FileName = fileName;
        }

        private FileStream Stream { get; }
        private uint DepotId { get; }
        private DepotManifest.ChunkData Chunk { get; }
        private byte[] DepotKey { get; }
        private string FileName { get; }
        private int Length => checked((int)Chunk.UncompressedLength);

        internal static ChunkWriteRequest Create(
            FileStream stream,
            uint depotId,
            DepotManifest.ChunkData chunk,
            byte[] depotKey,
            string fileName
        )
            => new(stream, depotId, chunk, depotKey, fileName);

        internal async Task RunAsync(DepotDownloader owner)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(Length);
            try
            {
                var written = await owner.DownloadChunkWithRetriesAsync(
                    DepotId,
                    Chunk,
                    buffer,
                    DepotKey,
                    FileName
                );

                Write(buffer, written);
                owner.RecordChunkWritten(written);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private void Write(byte[] buffer, int written)
        {
            Stream.Seek((long)Chunk.Offset, SeekOrigin.Begin);
            Stream.Write(buffer, 0, written);
        }
    }

    private async Task DownloadAndWriteChunkAsync(
        FileStream fs,
        uint depotId,
        DepotManifest.ChunkData chunk,
        byte[] depotKey,
        string fileName
    )
        => await ChunkWriteRequest
            .Create(fs, depotId, chunk, depotKey, fileName)
            .RunAsync(this);

    private void RecordChunkWritten(int written)
    {
        Interlocked.Add(ref _downloadedBytes, written);
        ReportProgress();
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
