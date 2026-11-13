using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Reflection;
using SteamKit2;
using STS2Mobile.Launcher;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    internal string ValidationProvider { get; set; } = "default";
    private Task<IReadOnlyList<SteamKit2.CDN.Server>> LoadValidationCdnServersAsync(CancellationToken ct)
    {
        if (ValidationProvider == "akamai")
        {
            ct.ThrowIfCancellationRequested();
            Log("TEST-ONLY explicit provider selected: skip optional CDN-directory WebAPI; normal Steam ownership/app-info/key/request-code checks remain.");
            ApplyValidationProvider();
            return Task.FromResult(_servers);
        }
        return LoadCdnServersAsync(ct);
    }
    private void ApplyValidationProvider()
    {
        if (ValidationProvider == "akamai")
        {
            SteamKit2.CDN.Server server = new DnsEndPoint("steampipe.akamaized.net", 443);
            typeof(SteamKit2.CDN.Server).GetProperty("Protocol", BindingFlags.Instance | BindingFlags.Public)!
                .SetValue(server, SteamKit2.CDN.Server.ConnectionProtocol.HTTPS);
            _servers = new[] { server };
            Log("TEST-ONLY CDN provider override: discovered SteamPipe Akamai; shipping provider discovery/rotation is unchanged.");
        }
        else if (ValidationProvider != "default") throw new InvalidDataException("Unsupported explicit validation provider.");
        foreach (var server in _servers) Log($"Validation CDN endpoint: Protocol={server.Protocol} Port={server.Port} Host={server.Host} VHost={server.VHost}");
    }

    internal async Task<object> RunValidationFixtureAsync(string realDataDir, string branch, string run,
        Action<DepotChunkTransferProgress> report, CancellationToken ct)
    {
        var state = SteamGameInstallPaths.DownloadStateDirectoryPath(realDataDir, branch);
        var realGame = SteamGameInstallPaths.GameDirectory(realDataDir, branch);
        DepotManifest captured = null;
        DepotManifest.FileData sourceFile = null;
        foreach (var manifestPath in Directory.EnumerateFiles(state, "*.manifest"))
        {
            using var input = File.OpenRead(manifestPath);
            var manifest = DepotManifest.Deserialize(input);
            var file = manifest.Files!.Where(file => !file.Flags.HasFlag(EDepotFileFlag.Directory)
                && file.TotalSize >= 3 * 1024 * 1024 && file.TotalSize <= 64 * 1024 * 1024 && file.Chunks.Count >= 3)
                .OrderByDescending(file => file.TotalSize)
                .FirstOrDefault(file => VerifyFileHash(DepotDownloadStaging.Resolve(realGame, file.FileName), file));
            if (file == null) continue;
            captured = manifest;
            sourceFile = file;
            break;
        }
        if (captured == null || sourceFile == null) throw new IOException("No captured multi-chunk installed file is available for the fixture.");
        Log($"Validation fixture source: depot={captured.DepotID} manifest={captured.ManifestGID} file={sourceFile.FileName} chunks={sourceFile.Chunks.Count} bytes={sourceFile.TotalSize}");
        var donors = Path.Combine(run, "fixture-donor");
        var source = DepotDownloadStaging.Resolve(realGame, sourceFile.FileName);
        var donor = DepotDownloadStaging.Resolve(donors, sourceFile.FileName);
        var missing = sourceFile.Chunks.First(chunk => sourceFile.Chunks.Count(other => other.ChunkID!.SequenceEqual(chunk.ChunkID!)) == 1);
        if (!File.Exists(donor))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(donor)!);
            File.Copy(source, donor);
            using var corrupt = new FileStream(donor, FileMode.Open, FileAccess.ReadWrite);
            corrupt.Position = checked((long)missing.Offset);
            var original = corrupt.ReadByte();
            corrupt.Position = checked((long)missing.Offset);
            corrupt.WriteByte((byte)(original ^ 255));
            corrupt.Flush(flushToDisk: true);
        }
        var raw = File.ReadAllBytes(source);
        var targetBytes = new byte[raw.Length];
        var targetFile = new DepotManifest.FileData { FileName = "renamed/rotated-real-content.bin", TotalSize = sourceFile.TotalSize };
        ulong offset = 0;
        foreach (var chunk in sourceFile.Chunks.Skip(1).Concat(sourceFile.Chunks.Take(1)))
        {
            Buffer.BlockCopy(raw, checked((int)chunk.Offset), targetBytes, checked((int)offset), checked((int)chunk.UncompressedLength));
            targetFile.Chunks.Add(new DepotManifest.ChunkData(chunk.ChunkID!, chunk.Checksum, offset,
                comp_length: chunk.CompressedLength, uncomp_length: chunk.UncompressedLength));
            offset += chunk.UncompressedLength;
        }
        targetFile.FileHash = ManagedSha1.Hash(targetBytes);
        var target = new DepotManifest
        {
            DepotID = captured.DepotID,
            ManifestGID = captured.ManifestGID ^ 0x4000000000000000UL,
            CreationTime = captured.CreationTime,
            Files = new List<DepotManifest.FileData> { targetFile }
        };
        var key = await _connection.GetDepotDecryptionKeyAsync(captured.DepotID).ConfigureAwait(false);
        Log("Validation fixture target construction complete; loading CDN servers.");
        _servers = await LoadValidationCdnServersAsync(ct).ConfigureAwait(false);
        ApplyValidationProvider();
        var install = Path.Combine(run, "fixture-target");
        var fixtureState = Path.Combine(run, "fixture-state");
        DepotChunkTransferProgress last = default;
        var requests = 0;
        var active = 0;
        var peak = 0;
        var transfer = new DepotChunkTransfer(install, fixtureState, captured.DepotID, target.ManifestGID,
            target, new[] { new DepotChunkDonor(donors, captured) }, 3,
            async (chunk, buffer, token) =>
            {
                Interlocked.Increment(ref requests);
                var concurrency = Interlocked.Increment(ref active);
                int before;
                do { before = Volatile.Read(ref peak); } while (concurrency > before && Interlocked.CompareExchange(ref peak, concurrency, before) != before);
                try { return await ValidationFetchAsync(captured.DepotID, chunk, token, () => DownloadChunkWithRetriesAsync(captured.DepotID, chunk, buffer, key, "validation fixture", token)).ConfigureAwait(false); }
                finally { Interlocked.Decrement(ref active); }
            }, progress => { last = progress; report(progress); });
        Log("Validation fixture starting raw transfer.");
        await transfer.RunAsync(ct).ConfigureAwait(false);
        if (!VerifyFileHash(DepotDownloadStaging.Resolve(install, targetFile.FileName), targetFile)) throw new IOException("Fixture final file hash mismatch.");
        if (requests != 1 || last.ReusedBytes <= 0 || last.DownloadedBytes != missing.CompressedLength)
            throw new IOException("Fixture did not reuse intact chunks and fetch exactly one corrupt range.");
        return new
        {
            capturedDepot = captured.DepotID,
            capturedManifest = captured.ManifestGID,
            syntheticTargetManifest = target.ManifestGID,
            sourceFile = sourceFile.FileName,
            targetFile = targetFile.FileName,
            rawFileSha1 = Convert.ToHexString(targetFile.FileHash),
            verified = true,
            requests,
            peakConcurrency = peak,
            progress = last
        };
    }
}
