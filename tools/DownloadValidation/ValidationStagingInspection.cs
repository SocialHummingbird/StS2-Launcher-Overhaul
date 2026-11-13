using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SteamKit2;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed record ValidationStagedChunk(uint DepotId, ulong ManifestId, string File, ulong Offset, string Sha1, uint Bytes);
internal sealed record ValidationStagingSnapshot(DateTimeOffset Utc, bool InstallationReady, int PartialFiles, int InvalidRanges,
    long VerifiedLogicalBytes, IReadOnlyList<ValidationStagedChunk> VerifiedChunks);

// Test-only, read-only evidence. Fetch completion alone does not prove that a range reached staging before process death.
internal static class ValidationStagingInspection
{
    internal static ValidationStagingSnapshot Read(string dataDir, string branch)
    {
        var verified = new List<ValidationStagedChunk>();
        var files = 0;
        var invalid = 0;
        var state = SteamGameInstallPaths.DownloadStateDirectoryPath(dataDir, branch);
        foreach (var partial in DepotDownloadStaging.ReadPartials(state))
            foreach (var file in partial.Manifest.Files!)
            {
                if (file.Flags.HasFlag(EDepotFileFlag.Directory)) continue;
                var path = DepotDownloadStaging.Resolve(partial.Root, file.FileName) + ".downloading";
                if (!System.IO.File.Exists(path)) continue;
                files++;
                using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                foreach (var chunk in file.Chunks)
                {
                    if (chunk.ChunkID?.Length != 20 || chunk.UncompressedLength == 0 || chunk.UncompressedLength > 64 * 1024 * 1024
                        || chunk.Offset > (ulong)input.Length || chunk.UncompressedLength > (ulong)input.Length - chunk.Offset)
                    { invalid++; continue; }
                    var length = checked((int)chunk.UncompressedLength);
                    var buffer = ArrayPool<byte>.Shared.Rent(length);
                    try
                    {
                        input.Position = checked((long)chunk.Offset);
                        input.ReadExactly(buffer.AsSpan(0, length));
                        if (!ManagedSha1.Hash(buffer.AsSpan(0, length)).SequenceEqual(chunk.ChunkID)) { invalid++; continue; }
                        verified.Add(new(partial.Manifest.DepotID, partial.Manifest.ManifestGID,
                            DepotDownloader.NormalizeManifestPath(file.FileName), chunk.Offset, Convert.ToHexString(chunk.ChunkID), chunk.UncompressedLength));
                    }
                    finally { ArrayPool<byte>.Shared.Return(buffer); }
                }
            }
        var ready = System.IO.File.Exists(BranchInstallStateStore.PathFor(dataDir, branch))
            && BranchInstallStateStore.Current.Read(dataDir, branch).IsReady;
        return new(DateTimeOffset.UtcNow, ready, files, invalid, verified.Sum(chunk => (long)chunk.Bytes), verified);
    }
}
