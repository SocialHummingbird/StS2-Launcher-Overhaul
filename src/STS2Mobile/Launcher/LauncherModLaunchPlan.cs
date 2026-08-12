using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal enum LauncherModSaveNamespace
{
    Vanilla,
    Modded,
}

internal enum LauncherModDiscoveryErrorCode
{
    SelectionRequired,
    SelectionNotDiscovered,
    SelectionUnsupported,
    RootMissing,
    ManifestLimitExceeded,
    ManifestNotFound,
    ManifestInvalid,
    ManifestIdentityMismatch,
    ManifestAmbiguous,
    PayloadMissing,
    PayloadEmpty,
    DuplicateManifestIdentity,
    DuplicateRoot,
    DependencyMissing,
    DependencyVersionInvalid,
    DependencyVersionTooLow,
    DependencyCycle,
}

internal sealed record LauncherModDiscoveryError(
    LauncherModDiscoveryErrorCode Code,
    string SelectionKey,
    string Message
);

internal sealed record LauncherModManifestDependency(
    string Id,
    string MinimumVersion
);

internal sealed record LauncherModManifest(
    string Id,
    string Name,
    string Version,
    string Author,
    string Description,
    bool AffectsGameplay,
    bool HasDll,
    bool HasPck,
    ImmutableArray<LauncherModManifestDependency> Dependencies
);

internal enum LauncherModManifestProbeKind
{
    NotManifest,
    Valid,
    Invalid,
}

internal sealed record LauncherModManifestProbe(
    LauncherModManifestProbeKind Kind,
    string ManifestPath,
    LauncherModManifest Manifest,
    LauncherModDiscoveryErrorCode ErrorCode,
    string ErrorMessage
);

internal sealed record LauncherResolvedMod(
    string SelectionKey,
    string PortableIdentity,
    string ManifestId,
    string Name,
    string Version,
    string Author,
    string Description,
    bool AffectsGameplay,
    string Source,
    string RootPath,
    string ManifestPath,
    string DllPath,
    string PckPath,
    bool RequiresWorkshopConsent,
    ImmutableArray<LauncherModManifestDependency> Dependencies
);

internal sealed record LauncherResolvedModDependency(
    string RequiredById,
    string DependencyId,
    string MinimumVersion,
    string ResolvedVersion
);

internal sealed class LauncherModLaunchPlan
{
    private const int MaxManifestFilesPerRoot = 64;

    private LauncherModLaunchPlan(
        LauncherModPlayMode mode,
        LauncherModSaveNamespace saveNamespace,
        ImmutableArray<LauncherResolvedMod> enabledMods,
        ImmutableArray<LauncherResolvedModDependency> dependencies,
        ImmutableArray<string> roots,
        string fingerprint
    )
    {
        Mode = mode;
        SaveNamespace = saveNamespace;
        EnabledMods = enabledMods;
        Dependencies = dependencies;
        Roots = roots;
        Fingerprint = fingerprint ?? string.Empty;
    }

    internal LauncherModPlayMode Mode { get; }
    internal LauncherModSaveNamespace SaveNamespace { get; }
    internal ImmutableArray<LauncherResolvedMod> EnabledMods { get; }
    internal ImmutableArray<LauncherResolvedModDependency> Dependencies { get; }
    internal ImmutableArray<string> Roots { get; }
    internal string Fingerprint { get; }

    internal string SaveNamespaceName
        => SaveNamespace == LauncherModSaveNamespace.Vanilla
            ? LauncherModSelectionState.VanillaModeName
            : LauncherModSelectionState.ModdedModeName;

    internal static LauncherModLaunchPlanResolution Resolve(
        LauncherModSelectionDocument document
    )
    {
        document ??= new LauncherModSelectionDocument();
        if (!LauncherModSelectionState.IsModdedModeFor(document))
            return LauncherModLaunchPlanResolution.Succeeded(Vanilla());

        return Resolve(document, LauncherModSelectionState.KnownMods(document));
    }

    internal static LauncherModLaunchPlan VanillaPlan() => Vanilla();

    internal static LauncherModLaunchPlanResolution Resolve(
        LauncherModSelectionDocument document,
        IReadOnlyList<LauncherKnownMod> knownMods
    )
    {
        document ??= new LauncherModSelectionDocument();
        if (!LauncherModSelectionState.IsModdedModeFor(document))
            return LauncherModLaunchPlanResolution.Succeeded(Vanilla());

        var explicitSelections = (document.EnabledMods ?? new Dictionary<string, bool>())
            .Where(pair => pair.Value)
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToArray();
        if (explicitSelections.Length == 0)
        {
            return Failed(
                LauncherModDiscoveryErrorCode.SelectionRequired,
                "selection",
                "Modded mode requires at least one explicitly selected mod."
            );
        }
        var explicitlySelectedKeys = explicitSelections
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var discovered = (knownMods ?? Array.Empty<LauncherKnownMod>())
            .Where(mod => mod != null)
            .ToArray();
        var discoveredByKey = discovered
            .Where(mod => !string.IsNullOrWhiteSpace(mod.Key))
            .GroupBy(mod => mod.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var selection in explicitSelections)
        {
            if (!discoveredByKey.TryGetValue(selection.Key, out var discoveredMod))
            {
                return Failed(
                    LauncherModDiscoveryErrorCode.SelectionNotDiscovered,
                    selection.Key,
                    $"Selected mod '{selection.Key}' was not found in Workshop or manual staging."
                );
            }

            if (discoveredMod.IsUnsupported)
            {
                return Failed(
                    LauncherModDiscoveryErrorCode.SelectionUnsupported,
                    selection.Key,
                    $"Selected mod '{selection.Key}' is rejected by the current mod policy."
                );
            }
        }

        var selected = discovered
            .Where(mod => mod != null && !mod.IsUnsupported)
            .Where(mod => explicitlySelectedKeys.Contains(mod.Key)
                || (mod.IsRequiredDependency && mod.Enabled))
            .OrderBy(mod => mod.Key, StringComparer.Ordinal)
            .ThenBy(mod => mod.Path, StringComparer.Ordinal)
            .ToArray();

        var resolved = new List<LauncherResolvedMod>(selected.Length);
        foreach (var candidate in selected)
        {
            var result = ResolveCandidate(candidate);
            if (!result.Success)
                return LauncherModLaunchPlanResolution.Failed(result.Error);

            resolved.Add(result.Mod);
        }

        var duplicateIdentity = resolved
            .GroupBy(mod => mod.ManifestId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateIdentity != null)
        {
            var duplicate = duplicateIdentity.OrderBy(mod => mod.SelectionKey, StringComparer.Ordinal).First();
            return Failed(
                LauncherModDiscoveryErrorCode.DuplicateManifestIdentity,
                duplicate.SelectionKey,
                $"Selected mod identity '{duplicate.ManifestId}' resolves from more than one root."
            );
        }

        var duplicateRoot = resolved
            .GroupBy(mod => mod.RootPath, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateRoot != null)
        {
            var duplicate = duplicateRoot.OrderBy(mod => mod.SelectionKey, StringComparer.Ordinal).First();
            return Failed(
                LauncherModDiscoveryErrorCode.DuplicateRoot,
                duplicate.SelectionKey,
                $"Selected mod root '{duplicate.RootPath}' resolves to more than one mod."
            );
        }

        var byId = resolved.ToDictionary(
            mod => mod.ManifestId,
            StringComparer.Ordinal
        );
        foreach (var mod in resolved.OrderBy(mod => mod.ManifestId, StringComparer.Ordinal))
        {
            foreach (var dependency in mod.Dependencies.OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                if (!byId.TryGetValue(dependency.Id, out var installed))
                {
                    return Failed(
                        LauncherModDiscoveryErrorCode.DependencyMissing,
                        mod.SelectionKey,
                        $"Selected mod '{mod.ManifestId}' requires missing dependency '{dependency.Id}'."
                    );
                }

                if (string.IsNullOrWhiteSpace(dependency.MinimumVersion))
                    continue;

                if (!TryParseNumericVersion(dependency.MinimumVersion, out var minimum)
                    || !TryParseNumericVersion(installed.Version, out var actual))
                {
                    return Failed(
                        LauncherModDiscoveryErrorCode.DependencyVersionInvalid,
                        mod.SelectionKey,
                        $"Dependency version for '{dependency.Id}' could not be compared: required '{dependency.MinimumVersion}', installed '{installed.Version}'."
                    );
                }

                if (CompareVersions(actual, minimum) < 0)
                {
                    return Failed(
                        LauncherModDiscoveryErrorCode.DependencyVersionTooLow,
                        mod.SelectionKey,
                        $"Selected mod '{mod.ManifestId}' requires '{dependency.Id}' {dependency.MinimumVersion} or newer; installed version is {installed.Version}."
                    );
                }
            }
        }

        if (!TryOrderByDependencies(resolved, byId, out var ordered, out var cycleError))
            return LauncherModLaunchPlanResolution.Failed(cycleError);

        var dependencyEdges = ordered
            .SelectMany(mod => mod.Dependencies.Select(dependency =>
            {
                var installed = byId[dependency.Id];
                return new LauncherResolvedModDependency(
                    mod.ManifestId,
                    dependency.Id,
                    dependency.MinimumVersion,
                    installed.Version
                );
            }))
            .ToImmutableArray();
        var roots = ordered.Select(mod => mod.RootPath).ToImmutableArray();
        var plan = new LauncherModLaunchPlan(
            LauncherModPlayMode.Modded,
            LauncherModSaveNamespace.Modded,
            ordered,
            dependencyEdges,
            roots,
            ComputeFingerprint(LauncherModPlayMode.Modded, ordered)
        );
        return LauncherModLaunchPlanResolution.Succeeded(plan);
    }

    internal static LauncherModManifestProbe InspectManifest(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
        {
            return InvalidProbe(
                manifestPath,
                LauncherModDiscoveryErrorCode.ManifestNotFound,
                $"Mod manifest was not found: {manifestPath}"
            );
        }

        var hasCompanionPayload = HasCompanionPayload(manifestPath);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        }
        catch (JsonException ex)
        {
            if (!hasCompanionPayload)
                return NotManifestProbe(manifestPath);

            return InvalidProbe(
                manifestPath,
                LauncherModDiscoveryErrorCode.ManifestInvalid,
                $"Mod manifest JSON is invalid at '{manifestPath}': {ex.Message}"
            );
        }
        catch (Exception ex)
        {
            return InvalidProbe(
                manifestPath,
                LauncherModDiscoveryErrorCode.ManifestInvalid,
                $"Mod manifest could not be read at '{manifestPath}': {ex.Message}"
            );
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return hasCompanionPayload
                    ? InvalidProbe(
                        manifestPath,
                        LauncherModDiscoveryErrorCode.ManifestInvalid,
                        $"Mod manifest must be a JSON object: {manifestPath}"
                    )
                    : NotManifestProbe(manifestPath);
            }

            var hasDllMarker = TryGetProperty(root, "has_dll", out _)
                || TryGetProperty(root, "hasDll", out _);
            var hasPckMarker = TryGetProperty(root, "has_pck", out _)
                || TryGetProperty(root, "hasPck", out _);
            if (!hasDllMarker && !hasPckMarker && !hasCompanionPayload)
                return NotManifestProbe(manifestPath);

            if (!TryReadRequiredString(root, "id", out var id)
                || !TryReadRequiredString(root, "name", out var name)
                || !TryReadRequiredString(root, "version", out var version)
                || !TryReadRequiredBool(root, "has_dll", "hasDll", out var hasDll)
                || !TryReadRequiredBool(root, "has_pck", "hasPck", out var hasPck))
            {
                return InvalidProbe(
                    manifestPath,
                    LauncherModDiscoveryErrorCode.ManifestInvalid,
                    $"Mod manifest '{manifestPath}' must contain non-empty id, name, version and boolean has_dll/has_pck fields."
                );
            }

            if (!string.Equals(
                    Path.GetFileNameWithoutExtension(manifestPath),
                    id,
                    StringComparison.Ordinal
                ))
            {
                return InvalidProbe(
                    manifestPath,
                    LauncherModDiscoveryErrorCode.ManifestIdentityMismatch,
                    $"Manifest file '{Path.GetFileName(manifestPath)}' does not match declared id '{id}'."
                );
            }

            if (!TryReadDependencies(root, out var dependencies, out var dependencyError))
            {
                return InvalidProbe(
                    manifestPath,
                    LauncherModDiscoveryErrorCode.ManifestInvalid,
                    $"Mod manifest '{manifestPath}' has invalid dependencies: {dependencyError}"
                );
            }

            if (!hasDll && !hasPck)
            {
                return InvalidProbe(
                    manifestPath,
                    LauncherModDiscoveryErrorCode.ManifestInvalid,
                    $"Mod manifest '{manifestPath}' declares neither a DLL nor a PCK payload."
                );
            }

            return new LauncherModManifestProbe(
                LauncherModManifestProbeKind.Valid,
                Path.GetFullPath(manifestPath),
                new LauncherModManifest(
                    id,
                    name,
                    version,
                    ReadOptionalString(root, "author"),
                    ReadOptionalString(root, "description"),
                    ReadOptionalBool(root, "affects_gameplay", "affectsGameplay"),
                    hasDll,
                    hasPck,
                    dependencies
                ),
                default,
                string.Empty
            );
        }
    }

    private static LauncherModLaunchPlan Vanilla()
        => new(
            LauncherModPlayMode.Vanilla,
            LauncherModSaveNamespace.Vanilla,
            ImmutableArray<LauncherResolvedMod>.Empty,
            ImmutableArray<LauncherResolvedModDependency>.Empty,
            ImmutableArray<string>.Empty,
            ComputeFingerprint(LauncherModPlayMode.Vanilla, ImmutableArray<LauncherResolvedMod>.Empty)
        );

    private static LauncherResolvedModResult ResolveCandidate(LauncherKnownMod candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate.DiscoveryError))
        {
            return LauncherResolvedModResult.Failed(new LauncherModDiscoveryError(
                candidate.DiscoveryErrorCode,
                candidate.Key,
                candidate.DiscoveryError
            ));
        }

        if (string.IsNullOrWhiteSpace(candidate.Path) || !Directory.Exists(candidate.Path))
        {
            return CandidateFailed(
                candidate,
                LauncherModDiscoveryErrorCode.RootMissing,
                $"Selected mod root was not found: {candidate.Path}"
            );
        }

        var sourceRoot = Path.GetFullPath(candidate.Path);
        string[] jsonFiles;
        try
        {
            jsonFiles = Directory
                .EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
                .Where(path => string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                .Where(path => !Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Take(MaxManifestFilesPerRoot + 1)
                .ToArray();
        }
        catch (Exception ex)
        {
            return CandidateFailed(
                candidate,
                LauncherModDiscoveryErrorCode.ManifestInvalid,
                $"Selected mod root could not be inspected: {ex.Message}"
            );
        }

        if (jsonFiles.Length > MaxManifestFilesPerRoot)
        {
            return CandidateFailed(
                candidate,
                LauncherModDiscoveryErrorCode.ManifestLimitExceeded,
                $"Selected mod root contains more than {MaxManifestFilesPerRoot} JSON files: {sourceRoot}"
            );
        }

        var probes = jsonFiles.Select(InspectManifest).ToArray();
        var invalid = probes.FirstOrDefault(probe => probe.Kind == LauncherModManifestProbeKind.Invalid);
        if (invalid != null)
            return CandidateFailed(candidate, invalid.ErrorCode, invalid.ErrorMessage);

        var manifests = probes
            .Where(probe => probe.Kind == LauncherModManifestProbeKind.Valid)
            .ToArray();
        if (manifests.Length == 0)
        {
            return CandidateFailed(
                candidate,
                LauncherModDiscoveryErrorCode.ManifestNotFound,
                $"Selected mod root contains no valid mod manifest: {sourceRoot}"
            );
        }

        if (manifests.Length != 1)
        {
            return CandidateFailed(
                candidate,
                LauncherModDiscoveryErrorCode.ManifestAmbiguous,
                $"Selected mod root contains {manifests.Length} valid mod manifests; exactly one is required: {sourceRoot}"
            );
        }

        var probe = manifests[0];
        string expectedManifestPath = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(candidate.ManifestPath))
                expectedManifestPath = Path.GetFullPath(candidate.ManifestPath);
        }
        catch (Exception ex)
        {
            return CandidateFailed(
                candidate,
                LauncherModDiscoveryErrorCode.ManifestIdentityMismatch,
                $"Selected manifest path is invalid: {ex.Message}"
            );
        }

        if (!string.IsNullOrWhiteSpace(expectedManifestPath)
            && !string.Equals(expectedManifestPath, probe.ManifestPath, StringComparison.OrdinalIgnoreCase))
        {
            return CandidateFailed(
                candidate,
                LauncherModDiscoveryErrorCode.ManifestIdentityMismatch,
                $"Selected manifest '{candidate.ManifestPath}' did not resolve to the root's only valid manifest '{probe.ManifestPath}'."
            );
        }

        if (!string.IsNullOrWhiteSpace(candidate.ManifestIdentity)
            && !string.Equals(candidate.ManifestIdentity, probe.Manifest.Id, StringComparison.Ordinal))
        {
            return CandidateFailed(
                candidate,
                LauncherModDiscoveryErrorCode.ManifestIdentityMismatch,
                $"Selected manifest identity '{candidate.ManifestIdentity}' does not match declared id '{probe.Manifest.Id}'."
            );
        }

        var rootPath = Path.GetDirectoryName(probe.ManifestPath) ?? sourceRoot;
        var dllPath = string.Empty;
        var pckPath = string.Empty;
        if (probe.Manifest.HasDll
            && !TryResolvePayload(rootPath, probe.Manifest.Id, ".dll", out dllPath, out var dllError))
        {
            return CandidateFailed(candidate, dllError.Code, dllError.Message);
        }

        if (probe.Manifest.HasPck
            && !TryResolvePayload(rootPath, probe.Manifest.Id, ".pck", out pckPath, out var pckError))
        {
            return CandidateFailed(candidate, pckError.Code, pckError.Message);
        }

        return LauncherResolvedModResult.Succeeded(new LauncherResolvedMod(
            candidate.Key ?? string.Empty,
            candidate.PortableIdentity ?? string.Empty,
            probe.Manifest.Id,
            probe.Manifest.Name,
            probe.Manifest.Version,
            probe.Manifest.Author,
            probe.Manifest.Description,
            probe.Manifest.AffectsGameplay,
            candidate.Source ?? string.Empty,
            Path.GetFullPath(rootPath),
            probe.ManifestPath,
            dllPath,
            pckPath,
            (candidate.Source ?? string.Empty).StartsWith("Workshop", StringComparison.OrdinalIgnoreCase),
            probe.Manifest.Dependencies
        ));
    }

    private static bool TryResolvePayload(
        string rootPath,
        string manifestId,
        string extension,
        out string payloadPath,
        out (LauncherModDiscoveryErrorCode Code, string Message) error
    )
    {
        payloadPath = string.Empty;
        error = default;
        string[] matches;
        try
        {
            matches = Directory
                .EnumerateFiles(rootPath, "*", SearchOption.TopDirectoryOnly)
                .Where(path => string.Equals(
                    Path.GetFileName(path),
                    manifestId + extension,
                    StringComparison.Ordinal
                ))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception ex)
        {
            error = (
                LauncherModDiscoveryErrorCode.PayloadMissing,
                $"Declared {extension} payload for '{manifestId}' could not be inspected: {ex.Message}"
            );
            return false;
        }
        if (matches.Length != 1)
        {
            error = (
                LauncherModDiscoveryErrorCode.PayloadMissing,
                $"Manifest '{manifestId}' declares {extension} payload but exactly one '{manifestId}{extension}' was not found beside the manifest."
            );
            return false;
        }

        var info = new FileInfo(matches[0]);
        if (info.Length <= 0)
        {
            error = (
                LauncherModDiscoveryErrorCode.PayloadEmpty,
                $"Declared payload is empty: {matches[0]}"
            );
            return false;
        }

        payloadPath = info.FullName;
        return true;
    }

    private static bool HasCompanionPayload(string manifestPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(manifestPath);
            var stem = Path.GetFileNameWithoutExtension(manifestPath);
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(stem))
                return false;

            return Directory
                .EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                .Any(path =>
                {
                    var extension = Path.GetExtension(path);
                    return (string.Equals(extension, ".dll", StringComparison.Ordinal)
                            || string.Equals(extension, ".pck", StringComparison.Ordinal))
                        && string.Equals(
                            Path.GetFileNameWithoutExtension(path),
                            stem,
                            StringComparison.Ordinal
                        );
                });
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadDependencies(
        JsonElement root,
        out ImmutableArray<LauncherModManifestDependency> dependencies,
        out string error
    )
    {
        var values = ImmutableArray.CreateBuilder<LauncherModManifestDependency>();
        error = string.Empty;
        if (!TryGetProperty(root, "dependencies", out var property))
        {
            dependencies = values.ToImmutable();
            return true;
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            dependencies = default;
            error = "dependencies must be an array";
            return false;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in property.EnumerateArray())
        {
            string id;
            var minimumVersion = string.Empty;
            if (item.ValueKind == JsonValueKind.String)
            {
                id = item.GetString()?.Trim() ?? string.Empty;
            }
            else if (item.ValueKind == JsonValueKind.Object
                && TryReadRequiredString(item, "id", out id))
            {
                if (TryGetProperty(item, "min_version", out var minimumProperty))
                {
                    if (minimumProperty.ValueKind != JsonValueKind.String)
                    {
                        dependencies = default;
                        error = $"minimum version for '{id}' must be a string";
                        return false;
                    }

                    minimumVersion = minimumProperty.GetString()?.Trim() ?? string.Empty;
                }
            }
            else
            {
                dependencies = default;
                error = "each dependency must be an id string or an object with an id";
                return false;
            }

            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
            {
                dependencies = default;
                error = string.IsNullOrWhiteSpace(id)
                    ? "dependency id is empty"
                    : $"dependency '{id}' is declared more than once";
                return false;
            }

            values.Add(new LauncherModManifestDependency(id, minimumVersion));
        }

        dependencies = values
            .OrderBy(value => value.Id, StringComparer.Ordinal)
            .ToImmutableArray();
        return true;
    }

    private static bool TryOrderByDependencies(
        IReadOnlyList<LauncherResolvedMod> mods,
        IReadOnlyDictionary<string, LauncherResolvedMod> byId,
        out ImmutableArray<LauncherResolvedMod> ordered,
        out LauncherModDiscoveryError error
    )
    {
        var states = new Dictionary<string, int>(StringComparer.Ordinal);
        var output = ImmutableArray.CreateBuilder<LauncherResolvedMod>();
        LauncherModDiscoveryError detectedError = null;

        bool Visit(LauncherResolvedMod mod)
        {
            states.TryGetValue(mod.ManifestId, out var state);
            if (state == 2)
                return true;
            if (state == 1)
            {
                detectedError = new LauncherModDiscoveryError(
                    LauncherModDiscoveryErrorCode.DependencyCycle,
                    mod.SelectionKey,
                    $"Dependency cycle detected at selected mod '{mod.ManifestId}'."
                );
                return false;
            }

            states[mod.ManifestId] = 1;
            foreach (var dependency in mod.Dependencies.OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                if (!Visit(byId[dependency.Id]))
                    return false;
            }

            states[mod.ManifestId] = 2;
            output.Add(mod);
            return true;
        }

        foreach (var mod in mods.OrderBy(value => value.ManifestId, StringComparer.Ordinal))
        {
            if (!Visit(mod))
            {
                ordered = default;
                error = detectedError;
                return false;
            }
        }

        ordered = output.ToImmutable();
        error = null;
        return true;
    }

    private static string ComputeFingerprint(
        LauncherModPlayMode mode,
        ImmutableArray<LauncherResolvedMod> mods
    )
    {
        var canonical = new StringBuilder();
        canonical.AppendLine(mode == LauncherModPlayMode.Vanilla ? "vanilla" : "modded");
        foreach (var mod in mods)
        {
            canonical.Append(mod.ManifestId).Append('|')
                .Append(mod.Version).Append('|')
                .Append(mod.RootPath).Append('|')
                .Append(Path.GetFileName(mod.ManifestPath)).Append('|')
                .Append(Path.GetFileName(mod.DllPath)).Append('|')
                .Append(Path.GetFileName(mod.PckPath)).Append('|')
                .Append(string.Join(",", mod.Dependencies.Select(value => $"{value.Id}>={value.MinimumVersion}")))
                .AppendLine();
        }

        return Convert.ToHexString(
            AndroidJavaCrypto.Sha256HashData(Encoding.UTF8.GetBytes(canonical.ToString()))
        ).ToLowerInvariant();
    }

    private static bool TryReadRequiredString(JsonElement root, string name, out string value)
    {
        value = string.Empty;
        if (!TryGetProperty(root, name, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString()?.Trim() ?? string.Empty;
        return value.Length > 0;
    }

    private static string ReadOptionalString(JsonElement root, string name)
        => TryGetProperty(root, name, out var property)
            && property.ValueKind == JsonValueKind.String
                ? property.GetString()?.Trim() ?? string.Empty
                : string.Empty;

    private static bool ReadOptionalBool(
        JsonElement root,
        string snakeCaseName,
        string camelCaseName
    )
        => TryGetProperty(root, snakeCaseName, out var property)
            || TryGetProperty(root, camelCaseName, out property)
                ? property.ValueKind == JsonValueKind.True
                : false;

    private static bool TryReadRequiredBool(
        JsonElement root,
        string snakeCaseName,
        string camelCaseName,
        out bool value
    )
    {
        value = false;
        if (!TryGetProperty(root, snakeCaseName, out var property)
            && !TryGetProperty(root, camelCaseName, out property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (property.ValueKind == JsonValueKind.False)
            return true;

        return false;
    }

    private static bool TryGetProperty(JsonElement root, string name, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryParseNumericVersion(string value, out int[] components)
    {
        components = Array.Empty<int>();
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
            normalized = normalized[1..];
        var suffix = normalized.IndexOfAny(new[] { '-', '+' });
        if (suffix >= 0)
            normalized = normalized[..suffix];

        var parts = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Length > 4)
            return false;

        components = new int[parts.Length];
        for (var index = 0; index < parts.Length; index++)
        {
            if (!int.TryParse(parts[index], out components[index]) || components[index] < 0)
                return false;
        }

        return true;
    }

    private static int CompareVersions(IReadOnlyList<int> left, IReadOnlyList<int> right)
    {
        var length = Math.Max(left.Count, right.Count);
        for (var index = 0; index < length; index++)
        {
            var leftValue = index < left.Count ? left[index] : 0;
            var rightValue = index < right.Count ? right[index] : 0;
            var comparison = leftValue.CompareTo(rightValue);
            if (comparison != 0)
                return comparison;
        }

        return 0;
    }

    private static LauncherModManifestProbe NotManifestProbe(string path)
        => new(
            LauncherModManifestProbeKind.NotManifest,
            path ?? string.Empty,
            null,
            default,
            string.Empty
        );

    private static LauncherModManifestProbe InvalidProbe(
        string path,
        LauncherModDiscoveryErrorCode code,
        string message
    )
        => new(
            LauncherModManifestProbeKind.Invalid,
            path ?? string.Empty,
            null,
            code,
            message
        );

    private static LauncherResolvedModResult CandidateFailed(
        LauncherKnownMod candidate,
        LauncherModDiscoveryErrorCode code,
        string message
    )
        => LauncherResolvedModResult.Failed(new LauncherModDiscoveryError(
            code,
            candidate?.Key ?? string.Empty,
            message
        ));

    private static LauncherModLaunchPlanResolution Failed(
        LauncherModDiscoveryErrorCode code,
        string selectionKey,
        string message
    )
        => LauncherModLaunchPlanResolution.Failed(new LauncherModDiscoveryError(
            code,
            selectionKey,
            message
        ));

    private sealed record LauncherResolvedModResult(
        LauncherResolvedMod Mod,
        LauncherModDiscoveryError Error
    )
    {
        internal bool Success => Mod != null && Error == null;

        internal static LauncherResolvedModResult Succeeded(LauncherResolvedMod mod)
            => new(mod, null);

        internal static LauncherResolvedModResult Failed(LauncherModDiscoveryError error)
            => new(null, error);
    }
}

internal sealed record LauncherModLaunchPlanResolution(
    LauncherModLaunchPlan Plan,
    LauncherModDiscoveryError Error
)
{
    internal bool Success => Plan != null && Error == null;

    internal static LauncherModLaunchPlanResolution Succeeded(LauncherModLaunchPlan plan)
        => new(plan, null);

    internal static LauncherModLaunchPlanResolution Failed(LauncherModDiscoveryError error)
        => new(null, error);
}
