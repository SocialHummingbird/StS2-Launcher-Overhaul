using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed record DepotChunkDonor(string Root, DepotManifest Manifest, bool Partial = false, bool Selected = false);

// Pending manifests describe possible chunk sources, never installed readiness.
internal static class DepotDownloadStaging
{
    internal const string DirectoryName = "staging-v1";
    internal static string GenerationRoot(string stateRoot, uint depot, ulong manifest)
        => Path.Combine(stateRoot, DirectoryName, depot.ToString(System.Globalization.CultureInfo.InvariantCulture), manifest.ToString(System.Globalization.CultureInfo.InvariantCulture));

    internal static string Resolve(string root, string relative)
    {
        var normalized = DepotDownloader.NormalizeManifestPath(relative);
        if (string.IsNullOrWhiteSpace(normalized) || Path.IsPathRooted(normalized))
            throw new IOException("Invalid depot file path.");
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, normalized));
        if (!fullPath.StartsWith(fullRoot, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new IOException("Depot file path escapes its owned directory.");
        return fullPath;
    }

    internal static async Task SaveManifestAsync(string generationRoot, DepotManifest manifest, CancellationToken ct)
    {
        using var bytes = new MemoryStream();
        manifest.Serialize(bytes);
        await CancellableAtomicFile.WriteAllBytesAsync(Path.Combine(generationRoot, "manifest.bin"), bytes.ToArray(), true, ct).ConfigureAwait(false);
    }

    internal static IReadOnlyList<DepotChunkDonor> ReadPartials(string stateRoot, uint? depot = null, ulong? exceptManifest = null)
    {
        var result = new List<DepotChunkDonor>();
        var root = Path.Combine(stateRoot, DirectoryName);
        if (!Directory.Exists(root)) return result;
        foreach (var depotRoot in Directory.EnumerateDirectories(root).OrderBy(path => path, StringComparer.Ordinal))
        {
            if (!uint.TryParse(Path.GetFileName(depotRoot), out var depotId) || depot.HasValue && depot.Value != depotId) continue;
            foreach (var generation in Directory.EnumerateDirectories(depotRoot).OrderBy(path => path, StringComparer.Ordinal))
            {
                if (!ulong.TryParse(Path.GetFileName(generation), out var id) || exceptManifest == id) continue;
                try
                {
                    using var stream = File.OpenRead(Path.Combine(generation, "manifest.bin"));
                    var manifest = DepotManifest.Deserialize(stream);
                    if (manifest.DepotID != depotId || manifest.ManifestGID != id || manifest.Files == null || manifest.FilenamesEncrypted) continue;
                    result.Add(new DepotChunkDonor(Path.Combine(generation, "files"), manifest, Partial: true));
                }
                catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or ProtoBuf.ProtoException)
                {
                    PatchHelper.Log($"[Depot] Ignoring unreadable pending manifest: {ex.GetType().Name}");
                }
            }
        }
        return result;
    }

    internal static bool HasPending(string stateRoot) => ReadPartials(stateRoot).Count != 0;
}
