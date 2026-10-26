using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using STS2Mobile.Steam;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.Launcher;

internal enum LauncherModDiscoveryErrorCode
{
    SelectionNotDiscovered,
    SelectionAmbiguous,
    RootMissing,
    ManifestNotFound,
    SourceUnavailable,
    DependencyMissing,
}

internal sealed record LauncherModDiscoveryError(
    LauncherModDiscoveryErrorCode Code,
    string SelectionKey,
    string Message
);

internal sealed record LauncherModSource(
    string SelectionKey,
    string RootPath,
    string ManifestPath,
    ulong? WorkshopId,
    string InstalledGeneration,
    string DisplayTitle,
    string ContentSha256 = ""
);

internal sealed class LauncherModLaunchPlan
{
    private LauncherModLaunchPlan(
        LauncherModPlayMode mode,
        ImmutableArray<LauncherModSource> sources
    )
    {
        Mode = mode;
        Sources = sources;
        Roots = sources.Select(source => source.RootPath)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToImmutableArray();
        Fingerprint = ComputeFingerprint(mode, sources);
    }

    internal LauncherModPlayMode Mode { get; }
    internal ImmutableArray<LauncherModSource> Sources { get; }
    internal ImmutableArray<string> Roots { get; }
    internal string Fingerprint { get; }

    internal static LauncherModLaunchPlan VanillaPlan()
        => new(LauncherModPlayMode.Vanilla, ImmutableArray<LauncherModSource>.Empty);

    internal static LauncherModLaunchPlanResolution Resolve(LauncherModSelectionDocument document)
        => LauncherModSelectionState.IsModdedModeFor(document)
            ? Resolve(document, LauncherModSelectionState.KnownMods(document))
            : LauncherModLaunchPlanResolution.Succeeded(VanillaPlan());

    internal static LauncherModLaunchPlanResolution Resolve(
        LauncherModSelectionDocument document,
        IReadOnlyList<LauncherKnownMod> knownMods
    )
    {
        if (!LauncherModSelectionState.IsModdedModeFor(document))
            return LauncherModLaunchPlanResolution.Succeeded(VanillaPlan());

        var selectedKeys = (document.EnabledMods ?? new Dictionary<string, bool>())
            .Where(pair => pair.Value)
            .Select(pair => pair.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        var byKey = (knownMods ?? Array.Empty<LauncherKnownMod>())
            .Where(mod => mod != null && !string.IsNullOrWhiteSpace(mod.Key))
            .GroupBy(mod => mod.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var selected = new Dictionary<string, LauncherKnownMod>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<LauncherKnownMod>();
        foreach (var key in selectedKeys)
        {
            if (!byKey.TryGetValue(key, out var mod))
                return Failed(LauncherModDiscoveryErrorCode.SelectionNotDiscovered, key,
                    $"Selected source '{key}' was not found in Workshop or manual staging.");
            if (mod.SelectionError != null)
                return LauncherModLaunchPlanResolution.Failed(mod.SelectionError);
            if (selected.TryAdd(key, mod))
                pending.Enqueue(mod);
        }

        while (pending.TryDequeue(out var parent))
        {
            foreach (var dependencyId in parent.RequiredWorkshopIds)
            {
                var key = $"workshop:{dependencyId}";
                if (!byKey.TryGetValue(key, out var dependency))
                    return Failed(LauncherModDiscoveryErrorCode.DependencyMissing, parent.Key,
                        $"Selected Workshop source '{parent.Key}' requires unavailable Workshop item '{dependencyId}'.");
                if (selected.TryAdd(key, dependency))
                    pending.Enqueue(dependency);
            }
        }

        // This orders selected sources only. Native discovery owns manifest ordering,
        // dependency semantics, defaults, and validation within those sources.
        var sources = ImmutableArray.CreateBuilder<LauncherModSource>(selected.Count);
        foreach (var candidate in selected.Values.OrderBy(mod => mod.Key, StringComparer.Ordinal))
        {
            try
            {
                if (string.IsNullOrWhiteSpace(candidate.Path) || !Directory.Exists(candidate.Path))
                    return Failed(LauncherModDiscoveryErrorCode.RootMissing, candidate.Key,
                        $"Selected source root was not found: {candidate.Path}");

                var sourceRoot = Path.GetFullPath(candidate.Path);
                string manifestPath = null;
                if (!candidate.WorkshopId.HasValue)
                {
                    if (string.IsNullOrWhiteSpace(candidate.ManifestPath) || !File.Exists(candidate.ManifestPath))
                        return Failed(LauncherModDiscoveryErrorCode.ManifestNotFound, candidate.Key,
                            $"Selected manual manifest was not found: {candidate.ManifestPath}");
                    manifestPath = Path.GetFullPath(candidate.ManifestPath);
                }
                else if (candidate.Installed == null)
                {
                    return Failed(LauncherModDiscoveryErrorCode.SourceUnavailable, candidate.Key,
                        $"Selected Workshop source '{candidate.Key}' has no committed installation.");
                }

                var files = SteamWorkshopPackageInventory.Capture(sourceRoot);
                var digest = SteamWorkshopPackageInventory.ContentSha256(files);
                var installed = candidate.Installed;
                if (installed != null && !installed.LegacyUnverified && string.IsNullOrWhiteSpace(installed.Generation))
                    return Failed(LauncherModDiscoveryErrorCode.SourceUnavailable, candidate.Key,
                        $"Selected Workshop source '{candidate.Key}' has no committed generation identity.");

                var generation = installed == null
                    ? $"manual:{digest}"
                    : installed.LegacyUnverified
                        ? $"legacy:{digest}"
                        : installed.Generation;
                sources.Add(new LauncherModSource(candidate.Key, sourceRoot, manifestPath,
                    candidate.WorkshopId, generation, candidate.Title, digest));
            }
            catch (Exception ex)
            {
                return Failed(LauncherModDiscoveryErrorCode.SourceUnavailable, candidate.Key,
                    $"Selected source '{candidate.Key}' could not be prepared: {ex.Message}");
            }
        }

        return LauncherModLaunchPlanResolution.Succeeded(
            new LauncherModLaunchPlan(LauncherModPlayMode.Modded, sources.ToImmutable()));
    }

    private static string ComputeFingerprint(LauncherModPlayMode mode, ImmutableArray<LauncherModSource> sources)
    {
        var canonical = new StringBuilder();
        Append(mode == LauncherModPlayMode.Vanilla ? "vanilla" : "modded");
        foreach (var source in sources)
        {
            Append(source.SelectionKey.ToLowerInvariant());
            Append(source.RootPath);
            Append(source.ManifestPath);
            Append(source.WorkshopId?.ToString());
            Append(source.InstalledGeneration);
            Append(source.ContentSha256);
        }
        return Convert.ToHexString(AndroidJavaCrypto.Sha256HashData(
            Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();

        void Append(string value)
        {
            value ??= string.Empty;
            canonical.Append(value.Length).Append(':').Append(value);
        }
    }

    private static LauncherModLaunchPlanResolution Failed(
        LauncherModDiscoveryErrorCode code, string key, string message
    ) => LauncherModLaunchPlanResolution.Failed(new LauncherModDiscoveryError(code, key, message));
}

internal sealed record LauncherModLaunchPlanResolution(
    LauncherModLaunchPlan Plan,
    LauncherModDiscoveryError Error
)
{
    internal bool Success => Plan != null && Error == null;
    internal static LauncherModLaunchPlanResolution Succeeded(LauncherModLaunchPlan plan) => new(plan, null);
    internal static LauncherModLaunchPlanResolution Failed(LauncherModDiscoveryError error) => new(null, error);
}
