using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotChunkTransfer
{
    private Dictionary<ChunkKey, List<Source>> BuildSources()
    {
        var result = new Dictionary<ChunkKey, List<Source>>();
        // The caller supplies the selected installation first and other completed
        // versions last. Superseded partials are inserted between those groups.
        var donors = _donors.Where(donor => donor.Selected)
            .Append(new DepotChunkDonor(_installRoot, _manifest))
            .Concat(DepotDownloadStaging.ReadPartials(_stateRoot, _depotId, _manifestId))
            .Concat(_donors.Where(donor => !donor.Selected));
        foreach (var donor in donors)
        {
            if (donor.Manifest.DepotID != _depotId || donor.Manifest.Files == null) continue;
            foreach (var file in donor.Manifest.Files)
            {
                if (file.Flags.HasFlag(EDepotFileFlag.Directory)) continue;
                string path;
                try { path = DepotDownloadStaging.Resolve(donor.Root, file.FileName) + (donor.Partial ? ".downloading" : ""); }
                catch (IOException) { continue; }
                if (!File.Exists(path)) continue;
                foreach (var chunk in file.Chunks)
                {
                    if (chunk.ChunkID?.Length != 20 || chunk.UncompressedLength == 0 || chunk.UncompressedLength > 64L * 1024 * 1024) continue;
                    var key = Key(chunk);
                    if (!result.TryGetValue(key, out var entries)) result.Add(key, entries = new());
                    entries.Add(new Source(path, chunk));
                }
            }
        }
        // Legacy crash leftovers have no trusted descriptor: try target offsets,
        // and accept nothing until its bytes match the new manifest SHA-1.
        foreach (var file in _manifest.Files!)
        {
            if (file.Flags.HasFlag(EDepotFileFlag.Directory)) continue;
            var path = DepotDownloadStaging.Resolve(_installRoot, file.FileName) + ".downloading";
            if (!File.Exists(path)) continue;
            foreach (var chunk in file.Chunks)
            {
                if (chunk.ChunkID?.Length != 20 || chunk.UncompressedLength == 0) continue;
                var key = Key(chunk);
                if (!result.TryGetValue(key, out var entries)) result.Add(key, entries = new());
                entries.Add(new Source(path, chunk));
            }
        }
        return result;
    }

    private List<ChunkWork> ReuseLocalContent(List<ChunkWork> work, Dictionary<ChunkKey, List<Source>> sources, CancellationToken ct)
    {
        var pending = new List<ChunkWork>();
        using var reader = new VerifiedChunkReader();
        foreach (var item in work)
        {
            ct.ThrowIfCancellationRequested();
            if (item.Destinations.All(destination => destination.Valid)) continue;
            var buffer = ArrayPool<byte>.Shared.Rent(checked((int)item.Chunk.UncompressedLength));
            try
            {
                var resumed = item.StagedSource != null && reader.TryRead(item.StagedSource.Path, item.StagedSource.Chunk, buffer);
                var found = resumed;
                if (!found && sources.TryGetValue(Key(item.Chunk), out var candidates))
                    found = candidates.Any(source => reader.TryRead(source.Path, source.Chunk, buffer));
                if (!found) { pending.Add(item); continue; }
                var bytes = item.Destinations.Where(destination => !destination.Valid).Sum(destination => (long)destination.Chunk.UncompressedLength);
                WriteDestinations(item, buffer).GetAwaiter().GetResult();
                if (resumed) _resumed += bytes;
                else _reused += bytes;
                Report();
            }
            finally { ArrayPool<byte>.Shared.Return(buffer); }
        }
        return pending;
    }

    private sealed class VerifiedChunkReader : IDisposable
    {
        private string _path;
        private FileStream _stream;
        internal bool TryRead(string path, DepotManifest.ChunkData chunk, byte[] buffer)
        {
            try
            {
                if (_path != path)
                {
                    _stream?.Dispose();
                    _stream = null;
                    _path = path;
                    if (!File.Exists(path)) return false;
                    _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                }
                if (_stream == null || chunk.Offset > (ulong)_stream.Length || chunk.UncompressedLength > (ulong)_stream.Length - chunk.Offset) return false;
                _stream.Position = checked((long)chunk.Offset);
                var count = checked((int)chunk.UncompressedLength);
                _stream.ReadExactly(buffer.AsSpan(0, count));
                return ManagedSha1.Hash(buffer.AsSpan(0, count)).SequenceEqual(chunk.ChunkID!);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OverflowException)
            {
                _stream?.Dispose();
                _stream = null;
                return false;
            }
        }
        public void Dispose() => _stream?.Dispose();
    }

    private void RetireSupersededPartials()
    {
        var ownedRoot = Path.GetFullPath(Path.Combine(_stateRoot, DepotDownloadStaging.DirectoryName)) + Path.DirectorySeparatorChar;
        foreach (var donor in DepotDownloadStaging.ReadPartials(_stateRoot, _depotId, _manifestId))
        {
            var path = Path.GetFullPath(Path.GetDirectoryName(donor.Root)!);
            if (!path.StartsWith(ownedRoot, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
                || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Pending download cleanup escaped its owned staging directory.");
            Directory.Delete(path, recursive: true);
        }
    }
}
