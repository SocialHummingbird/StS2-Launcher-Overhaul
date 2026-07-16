using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Mobile.Steam;

internal readonly struct ModdedSaveSeedMapping
{
    internal ModdedSaveSeedMapping(string sourcePath, string targetPath, string saveNamespace)
    {
        SourcePath = sourcePath;
        TargetPath = targetPath;
        SaveNamespace = saveNamespace;
    }

    internal string SourcePath { get; }
    internal string TargetPath { get; }
    internal string SaveNamespace { get; }
}

internal sealed class ModdedSaveSeedPlan
{
    private const string AccountNamespace = "account";
    private static readonly string[] ProfileFiles =
    {
        "progress.save",
        "current_run.save",
        "current_run_mp.save",
        "prefs.save",
    };

    private ModdedSaveSeedPlan(
        IReadOnlyList<ModdedSaveSeedMapping> seedMappings,
        IReadOnlyList<string> directCloudModdedTargets,
        IReadOnlyList<string> cloudAuthoritativeNamespaces
    )
    {
        SeedMappings = seedMappings;
        DirectCloudModdedTargets = directCloudModdedTargets;
        CloudAuthoritativeNamespaces = cloudAuthoritativeNamespaces;
    }

    internal IReadOnlyList<ModdedSaveSeedMapping> SeedMappings { get; }
    internal IReadOnlyList<string> DirectCloudModdedTargets { get; }
    internal IReadOnlyList<string> CloudAuthoritativeNamespaces { get; }

    internal IReadOnlyList<string> LocalOverwriteTargets
        => DirectCloudModdedTargets
            .Concat(SeedMappings.Select(mapping => mapping.TargetPath))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    internal static ModdedSaveSeedPlan Create(IEnumerable<string> cloudPaths)
    {
        var paths = (cloudPaths ?? Array.Empty<string>())
            .Select(Canonicalize)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var cloudModdedNamespaces = paths
            .Select(TryGetAnyModdedNamespace)
            .Where(saveNamespace => saveNamespace != null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var directTargets = paths
            .Where(path => path.StartsWith("modded/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var mappings = new List<ModdedSaveSeedMapping>();
        foreach (var path in paths)
        {
            if (!TryGetVanillaCopySetPath(path, out var relativePath, out var saveNamespace)
                || cloudModdedNamespaces.Contains(saveNamespace))
            {
                continue;
            }

            mappings.Add(new ModdedSaveSeedMapping(
                path,
                $"modded/{relativePath}",
                saveNamespace
            ));
        }

        return new ModdedSaveSeedPlan(
            mappings
                .OrderBy(mapping => mapping.TargetPath, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            directTargets,
            cloudModdedNamespaces
                .OrderBy(saveNamespace => saveNamespace, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        );
    }

    private static string Canonicalize(string path)
        => (path ?? "")
            .Replace("user://", "", StringComparison.OrdinalIgnoreCase)
            .Replace('\\', '/')
            .TrimStart('/');

    private static string TryGetAnyModdedNamespace(string path)
    {
        if (!path.StartsWith("modded/", StringComparison.OrdinalIgnoreCase))
            return null;

        var relativePath = path["modded/".Length..];
        if (relativePath.Equals("profile.save", StringComparison.OrdinalIgnoreCase))
            return AccountNamespace;

        var slash = relativePath.IndexOf('/');
        if (slash <= 0)
            return null;

        var profile = relativePath[..slash];
        return IsProfileNamespace(profile) ? profile.ToLowerInvariant() : null;
    }

    private static bool TryGetVanillaCopySetPath(
        string path,
        out string relativePath,
        out string saveNamespace
    )
    {
        relativePath = path;
        saveNamespace = null;

        if (path.StartsWith("modded/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (relativePath.Equals("profile.save", StringComparison.OrdinalIgnoreCase))
        {
            saveNamespace = AccountNamespace;
            return true;
        }

        var parts = relativePath.Split('/');
        if (parts.Length < 3
            || !IsProfileNamespace(parts[0])
            || !parts[1].Equals("saves", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        saveNamespace = parts[0].ToLowerInvariant();
        if (parts.Length == 3)
            return ProfileFiles.Contains(parts[2], StringComparer.OrdinalIgnoreCase);

        return parts.Length == 4
            && parts[2].Equals("history", StringComparison.OrdinalIgnoreCase)
            && parts[3].EndsWith(".run", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProfileNamespace(string value)
    {
        if (!value.StartsWith("profile", StringComparison.OrdinalIgnoreCase))
            return false;

        return int.TryParse(value["profile".Length..], out var profileId)
            && profileId is >= 1 and <= 3;
    }
}
