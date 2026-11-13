using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.CDN;
using STS2Mobile.Launcher;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private string _validationOutput;
    private string _validationPhase;
    private Server _validationLastChunkServer;
    private string _validationLastChunkAuth;
    private readonly object _validationTraceLock = new();

    internal void ConfigureValidationTrace(string output, string phase)
    {
        _validationOutput = output;
        _validationPhase = phase;
    }

    private async Task ValidationBeforePckPreparationAsync()
    {
        if (_validationOutput == null) return;
        if (_retainedPreparedPck)
        {
            DownloadValidationRunner.Write(Path.Combine(_validationOutput, "pck-before-preparation.json"), new { retainedPreparedPck = true });
            return;
        }
        var verified = await Task.Run(() =>
        {
            foreach (var path in Directory.EnumerateFiles(SteamGameInstallPaths.DownloadStateDirectoryPath(_dataDir, _branch), "*.manifest"))
            {
                using var input = File.OpenRead(path);
                var manifest = DepotManifest.Deserialize(input);
                var pck = manifest.Files?.FirstOrDefault(file => NormalizeManifestPath(file.FileName) == "SlayTheSpire2.pck");
                if (pck != null) return VerifyFileHash(Path.Combine(_gameDir, "SlayTheSpire2.pck"), pck);
            }
            return false;
        }).ConfigureAwait(false);
        DownloadValidationRunner.Write(Path.Combine(_validationOutput, "pck-before-preparation.json"), new { retainedPreparedPck = false, rawSteamFileVerified = verified });
        if (!verified) throw new IOException("Validation observed an invalid raw PCK before Android preparation.");
    }

    private Task<T> ValidationCdnRequest<T>(Server server, int attempt, string kind, uint depot, string identity, Func<Task<T>> run, string auth = null)
    {
        lock (_validationTraceLock)
        {
            if (kind == "chunk") { _validationLastChunkServer = server; _validationLastChunkAuth = auth; }
            if (_validationOutput != null)
                File.AppendAllText(Path.Combine(_validationOutput, "sdk-requests.jsonl"), JsonSerializer.Serialize(new
                {
                    phase = _validationPhase,
                    kind,
                    depot,
                    identity,
                    attempt = attempt + 1,
                    protocol = server.Protocol.ToString(),
                    server.Host,
                    server.VHost,
                    server.Port,
                    requestTimeoutMs = Client.RequestTimeout.TotalMilliseconds,
                    responseBodyTimeoutMs = Client.ResponseBodyTimeout.TotalMilliseconds,
                    androidChunkWorkers = 3,
                }) + "\n");
        }
        return run();
    }

    internal async Task<Dictionary<string, object>> RunCdnInvestigationAsync(string originalData, string branch, string output, CancellationToken ct)
    {
        _validationOutput = output;
        var result = new Dictionary<string, object>
        {
            ["branch"] = branch,
            ["gatePassed"] = false,
            ["productionSettingsChanged"] = false,
            ["defaultRequestTimeoutMs"] = Client.RequestTimeout.TotalMilliseconds,
            ["defaultBodyTimeoutMs"] = Client.ResponseBodyTimeout.TotalMilliseconds,
            ["dnsTimingLimitation"] = "Actual DNS/TLS are inside HttpURLConnection connection/header time. Separate DNS probes run after requests, without warming the measured path.",
        };
        void Save() => DownloadValidationRunner.Write(Path.Combine(output, "diagnosis.json"), result);
        var verifiedChunks = new List<string>();
        DepotManifest cached = null;
        DepotManifest target = null;
        DepotManifest.ChunkData selectedChunk = null;
        byte[] depotKey = null;
        uint depotId = 0;
        ulong targetId = 0;
        foreach (var path in Directory.EnumerateFiles(SteamGameInstallPaths.DownloadStateDirectoryPath(originalData, branch), "*.manifest"))
        {
            using var input = File.OpenRead(path);
            cached = DepotManifest.Deserialize(input);
            if (cached.Files?.Any(file => NormalizeManifestPath(file.FileName) == "data_sts2_windows_x86_64/System.Private.CoreLib.dll") == true) break;
        }
        if (cached == null) throw new IOException("Captured manifest unavailable for independent chunk probe.");
        result["capturedManifest"] = cached.ManifestGID;
        result["capturedDepot"] = cached.DepotID;
        Save();
        try
        {
            _validationPhase = "app-info-and-ownership";
            var depots = await PrepareAndGetMainAppDepotsAsync(requireAny: true).ConfigureAwait(false);
            var depot = depots.FirstOrDefault(item => item.DepotId == cached.DepotID);
            if (depot.DepotId == 0) depot = depots[0];
            depotId = depot.DepotId; targetId = depot.ManifestId;
            result["authenticatedTargets"] = depots.Select(item => new { item.DepotId, item.ManifestId, item.Branch }).ToArray();
            _validationPhase = "default-server-discovery";
            _servers = await LoadCdnServersAsync(ct).ConfigureAwait(false);
            result["defaultServers"] = _servers.Select(server => new { protocol = server.Protocol.ToString(), server.Host, server.VHost, server.Port }).ToArray();
            Save();
            _validationPhase = "default-manifest";
            try
            {
                var loaded = await LoadDepotManifestAsync(depot, ct).ConfigureAwait(false);
                target = loaded.Manifest; depotKey = loaded.DepotKey;
                if (target.DepotID != depotId || target.ManifestGID != targetId || target.FilenamesEncrypted)
                    throw new IOException("Downloaded target manifest identity/decryption mismatch.");
                result["defaultManifestVerified"] = true;
                result["manifestFiles"] = target.Files.Count;
            }
            catch (Exception error) { result["defaultManifestVerified"] = false; result["manifestFailure"] = Chain(error); }
            result["javaAfterManifest"] = JavaState();
            Save();
            depotKey ??= await _connection.GetDepotDecryptionKeyAsync(depotId).ConfigureAwait(false);
            var probeManifest = target ?? (cached.DepotID == depotId ? cached : throw new IOException("Captured depot differs from authenticated target."));
            var file = probeManifest.Files.FirstOrDefault(item => NormalizeManifestPath(item.FileName) == "data_sts2_windows_x86_64/System.Private.CoreLib.dll")
                ?? probeManifest.Files.First(item => item.Chunks.Count >= 3);
            var probes = file.Chunks.Where(chunk => chunk.ChunkID?.Length == 20 && chunk.UncompressedLength > 0 && chunk.UncompressedLength <= 64 * 1024 * 1024)
                .GroupBy(chunk => Convert.ToHexString(chunk.ChunkID)).Select(group => group.First()).Take(3).ToArray();
            selectedChunk = probes[0];
            result["chunkSource"] = target == null ? "captured manifest; cannot satisfy live gate" : "authenticated target manifest";
            result["probeChunks"] = probes.Select(chunk => new { sha1 = Convert.ToHexString(chunk.ChunkID), chunk.UncompressedLength, chunk.CompressedLength }).ToArray();
            _validationPhase = "default-chunk";
            foreach (var chunk in probes)
            {
                selectedChunk = chunk; // Java replays the last attempted chunk, including a failed second/third probe.
                var buffer = ArrayPool<byte>.Shared.Rent(checked((int)chunk.UncompressedLength));
                try
                {
                    var count = await ValidationFetchAsync(depotId, chunk, ct,
                        () => DownloadChunkWithRetriesAsync(depotId, chunk, buffer, depotKey, "CDN investigation", ct)).ConfigureAwait(false);
                    if (count != chunk.UncompressedLength || !ManagedSha1.Hash(buffer.AsSpan(0, count)).SequenceEqual(chunk.ChunkID))
                        throw new IOException("Default chunk failed final SHA-1.");
                    verifiedChunks.Add(Convert.ToHexString(chunk.ChunkID));
                    if (target == null) break; // One independent captured-resource operation is sufficient on manifest failure.
                }
                catch (Exception error) { result["chunkFailure"] = Chain(error); break; }
                finally { ArrayPool<byte>.Shared.Return(buffer); }
            }
            result["defaultVerifiedChunkIds"] = verifiedChunks;
            result["javaAfterSdkOperations"] = JavaState();
            result["gatePassed"] = target != null && verifiedChunks.Distinct().Count() == 3;
            Save();
            // Observe outstanding Java work rather than treating managed cancellation as a socket disconnect.
            result["workersDrainedBeforeComparison"] = await WaitForWorkersAsync(output, TimeSpan.FromSeconds(150), ct).ConfigureAwait(false);
            Save();
            if (!(bool)result["gatePassed"])
            {
                result["directManifest"] = await ReplayAsync("manifest", null, depotKey, depotId, targetId, output, ct).ConfigureAwait(false);
                var directChunk = await ReplayAsync("chunk", selectedChunk, depotKey, depotId, targetId, output, ct).ConfigureAwait(false);
                result["directChunk"] = directChunk;
                Save();
                if (directChunk.TryGetValue("verified", out var valid) && valid is true
                    && directChunk.TryGetValue("javaElapsedMs", out var elapsed) && Convert.ToDouble(elapsed) > Client.RequestTimeout.TotalMilliseconds)
                {
                    var previous = Client.RequestTimeout;
                    var buffer = ArrayPool<byte>.Shared.Rent(checked((int)selectedChunk.UncompressedLength));
                    try
                    {
                        _validationPhase = "diagnostic-only-single-request-60s";
                        Client.RequestTimeout = TimeSpan.FromSeconds(60);
                        var begin = Environment.TickCount64;
                        var count = await new CdnServerAttempt(_validationLastChunkServer, 0).DownloadChunkAsync(this, depotId, selectedChunk, buffer, depotKey, _validationLastChunkAuth).ConfigureAwait(false);
                        result["deadlineExperiment"] = new
                        {
                            diagnosticOnly = true,
                            countsForAcceptance = false,
                            requestTimeoutMs = 60000,
                            elapsedMs = Environment.TickCount64 - begin,
                            verified = count == selectedChunk.UncompressedLength && ManagedSha1.Hash(buffer.AsSpan(0, count)).SequenceEqual(selectedChunk.ChunkID)
                        };
                    }
                    catch (Exception error) { result["deadlineExperiment"] = new { diagnosticOnly = true, countsForAcceptance = false, error = Chain(error) }; }
                    finally { Client.RequestTimeout = previous; ArrayPool<byte>.Shared.Return(buffer); }
                }
                else result["deadlineExperiment"] = "Not run: no verified direct Java content after the default deadline.";
            }
        }
        catch (Exception error) { result["diagnosticFailure"] = Chain(error); }
        finally
        {
            result["finalDefaultRequestTimeoutMs"] = Client.RequestTimeout.TotalMilliseconds;
            result["javaWorkersAtClose"] = JavaState();
            foreach (var kind in new[] { "manifest", "chunk" })
            {
                try
                {
                    using var state = JsonDocument.Parse(ValidationJava.Call("getDownloadValidationHttpState"));
                    var request = state.RootElement.GetProperty("requests").EnumerateArray().FirstOrDefault(item => item.GetProperty("source").GetString() == "steamkit" && item.GetProperty("category").GetString() == kind);
                    if (request.ValueKind != JsonValueKind.Undefined)
                        result[kind + "DnsAfter"] = JsonDocument.Parse(ValidationJava.Call("getDownloadValidationDnsAfter", request.GetProperty("requestId").GetString())).RootElement.Clone();
                }
                catch (Exception error) { result[kind + "DnsAfterFailure"] = Chain(error); }
            }
            Save();
        }
        return result;
    }

    private async Task<Dictionary<string, object>> ReplayAsync(string kind, DepotManifest.ChunkData chunk, byte[] key, uint depot, ulong manifestId, string output, CancellationToken ct)
    {
        var result = new Dictionary<string, object> { ["diagnosticOnly"] = true, ["countsForAcceptance"] = false };
        try
        {
            var id = ValidationJava.Call("replayDownloadValidationResource", kind);
            result["requestId"] = id;
            var end = Environment.TickCount64 + 150000;
            string raw;
            while (string.IsNullOrEmpty(raw = ValidationJava.Call("takeDownloadValidationReplay", id)))
            {
                ct.ThrowIfCancellationRequested();
                if (Environment.TickCount64 > end) throw new TimeoutException("Direct Java comparison exceeded its bounded observation window.");
                await Task.Delay(500, ct).ConfigureAwait(false);
            }
            using var response = JsonDocument.Parse(raw);
            var root = response.RootElement;
            if (root.TryGetProperty("error", out var failure)) throw new IOException(DownloadValidationRunner.SafeText(failure.GetString()));
            result["status"] = root.GetProperty("status").GetInt32();
            if ((int)result["status"] != 200) return result;
            var bytes = root.TryGetProperty("bodyFile", out var path) ? File.ReadAllBytes(path.GetString()) : Convert.FromBase64String(root.GetProperty("body").GetString());
            result["receivedBytes"] = bytes.Length;
            if (chunk != null)
            {
                var destination = ArrayPool<byte>.Shared.Rent(checked((int)chunk.UncompressedLength));
                try
                {
                    var count = DepotChunk.Process(chunk, bytes, destination, key);
                    result["verified"] = count == chunk.UncompressedLength && ManagedSha1.Hash(destination.AsSpan(0, count)).SequenceEqual(chunk.ChunkID);
                    result["sha1"] = Convert.ToHexString(ManagedSha1.Hash(destination.AsSpan(0, count)));
                }
                finally { ArrayPool<byte>.Shared.Return(destination); }
            }
            else
            {
                using var zip = new ZipArchive(new MemoryStream(bytes));
                if (zip.Entries.Count != 1) throw new IOException("Unexpected direct manifest archive.");
                using var stream = zip.Entries[0].Open();
                var manifest = DepotManifest.Deserialize(stream);
                manifest.DecryptFilenames(key);
                result["verified"] = manifest.DepotID == depot && manifest.ManifestGID == manifestId && !manifest.FilenamesEncrypted;
            }
            using var state = JsonDocument.Parse(ValidationJava.Call("getDownloadValidationHttpState"));
            var request = state.RootElement.GetProperty("requests").EnumerateArray().Single(item => item.GetProperty("requestId").GetString() == id);
            result["javaElapsedMs"] = request.GetProperty("elapsedMs").GetDouble();
        }
        catch (Exception error) { result["failure"] = Chain(error); }
        return result;
    }

    private async Task<bool> WaitForWorkersAsync(string output, TimeSpan timeout, CancellationToken ct)
    {
        var end = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
        while (true)
        {
            var state = JavaState();
            DownloadValidationRunner.Write(Path.Combine(output, "java-worker-state.json"), state);
            if (state.GetProperty("activeJavaWorkers").GetInt32() == 0) return true;
            if (Environment.TickCount64 > end) return false;
            await Task.Delay(1000, ct).ConfigureAwait(false);
        }
    }
    private static JsonElement JavaState()
    {
        using var state = JsonDocument.Parse(ValidationJava.Call("getDownloadValidationHttpState"));
        return state.RootElement.Clone();
    }
    private static object[] Chain(Exception error)
    {
        var entries = new List<object>();
        for (var count = 0; error != null && count < 8; count++, error = error.InnerException)
            entries.Add(new { type = error.GetType().FullName, message = DownloadValidationRunner.SafeText(error.Message) });
        return entries.ToArray();
    }
}
