using System;
using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherRuntimeCacheEvidence
{
    internal const string MarkerFileName = "current_runtime_cache.txt";

    internal static string MarkerPath(string dataDir)
        => Path.Combine(dataDir, MarkerFileName);

    internal static bool MarkerPresent(string dataDir)
        => File.Exists(MarkerPath(dataDir));

    internal static void Clear(string dataDir)
    {
        try
        {
            var path = MarkerPath(dataDir);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to clear runtime cache evidence marker: {ex.Message}");
        }
    }

    internal static string UtcMillis(string dataDir)
        => ReadMarkerValue(dataDir, "UTC millis:");

    internal static string Package(string dataDir)
        => ReadMarkerValue(dataDir, "Package:");

    internal static string VersionName(string dataDir)
        => ReadMarkerValue(dataDir, "Version name:");

    internal static string VersionCode(string dataDir)
        => ReadMarkerValue(dataDir, "Version code:");

    internal static string AssemblyCacheSchema(string dataDir)
        => ReadMarkerValue(dataDir, "Assembly cache schema:");

    internal static string SelectedBranch(string dataDir)
        => ReadMarkerValue(dataDir, "Selected branch:");

    internal static string SelectedBranchRequiresRuntimePack(string dataDir)
        => ReadMarkerValue(dataDir, "Selected branch requires runtime pack:");

    internal static string RuntimeId(string dataDir)
        => ReadMarkerValue(dataDir, "Runtime ID:");

    internal static string RuntimeSource(string dataDir)
        => ReadMarkerValue(dataDir, "Runtime source:");

    internal static string RuntimePackDirectory(string dataDir)
        => ReadMarkerValue(dataDir, "Runtime pack directory:");

    internal static string RuntimePackGameAssembly(string dataDir)
        => ReadMarkerValue(dataDir, "Runtime pack game assembly:");

    internal static string GameDirectory(string dataDir)
        => ReadMarkerValue(dataDir, "Game directory:");

    internal static string SelectedPckPath(string dataDir)
        => ReadMarkerValue(dataDir, "Selected PCK path:");

    internal static string SelectedPckIdentity(string dataDir)
        => ReadMarkerValue(dataDir, "Selected PCK identity:");

    internal static string SelectedPckSha256(string dataDir)
        => ReadMarkerValue(dataDir, "Selected PCK SHA256:");

    internal static string SelectedSourceAssembly(string dataDir)
        => ReadMarkerValue(dataDir, "Selected source sts2.dll:");

    internal static string SelectedSourceAssemblySha256(string dataDir)
        => ReadMarkerValue(dataDir, "Selected source sts2.dll SHA256:");

    internal static string ActiveSourceAssembly(string dataDir)
        => ReadMarkerValue(dataDir, "Active source sts2.dll:");

    internal static string ActiveSourceAssemblySha256(string dataDir)
        => ReadMarkerValue(dataDir, "Active source sts2.dll SHA256:");

    internal static string PublishCacheDirectory(string dataDir)
        => ReadMarkerValue(dataDir, "Publish cache directory:");

    internal static string PublishCacheActiveAssemblySha256(string dataDir)
        => ReadMarkerValue(dataDir, "Publish cache active sts2.dll SHA256:");

    internal static bool MatchesSelectedBranch(string dataDir, string selectedBranch)
    {
        var markerBranch = SelectedBranch(dataDir);
        return HasValue(markerBranch)
            && string.Equals(
                SteamGameBranch.Normalize(markerBranch),
                SteamGameBranch.Normalize(selectedBranch),
                StringComparison.OrdinalIgnoreCase
            );
    }

    internal static bool PckMatchesSelectedRuntime(string dataDir, string selectedBranch)
    {
        var markerHash = SelectedPckSha256(dataDir);
        if (!HasValue(markerHash))
            return false;

        var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
        if (HasValue(slot.PckSha256)
            && string.Equals(markerHash, slot.PckSha256, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return slot.RuntimePackUsable
            && slot.RuntimePack.SourcePckMatchesSelectedPck(markerHash, slot.PckPath);
    }

    internal static bool SourceAssemblyMatchesSelectedRuntime(string dataDir, string selectedBranch)
    {
        var markerHash = SelectedSourceAssemblySha256(dataDir);
        if (!HasValue(markerHash))
            return false;

        var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
        return HasValue(slot.SourceAssemblySha256)
            && string.Equals(markerHash, slot.SourceAssemblySha256, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool PublishCacheMatchesSelectedRuntime(string dataDir, string selectedBranch)
    {
        var markerHash = PublishCacheActiveAssemblySha256(dataDir);
        if (!HasValue(markerHash))
            return false;

        var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
        if (slot.RuntimePackUsable)
            return HasValue(slot.RuntimePack.ActualAndroidAssemblySha256)
                && string.Equals(markerHash, slot.RuntimePack.ActualAndroidAssemblySha256, StringComparison.OrdinalIgnoreCase);

        if (slot.RequiresRuntimePackOrPreparedCache)
            return false;

        if (slot.SourceAssemblyExists)
            return string.Equals(markerHash, slot.SourceAssemblySha256, StringComparison.OrdinalIgnoreCase);

        return slot.UsesLegacyPackagedPublicRuntime
            && HasValue(slot.ActiveAndroidAssemblySha256)
            && string.Equals(markerHash, slot.ActiveAndroidAssemblySha256, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool CachePreparedForSelectedRuntime(string dataDir, string selectedBranch)
        => MarkerPresent(dataDir)
            && MatchesSelectedBranch(dataDir, selectedBranch)
            && PckMatchesSelectedRuntime(dataDir, selectedBranch)
            && SourceAssemblyMatchesSelectedRuntime(dataDir, selectedBranch)
            && PublishCacheMatchesSelectedRuntime(dataDir, selectedBranch);

    private static string ReadMarkerValue(string dataDir, string prefix)
    {
        try
        {
            var path = MarkerPath(dataDir);
            if (!File.Exists(path))
                return "<none>";

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return line.Substring(prefix.Length).Trim();
            }
        }
        catch
        {
            return "<read failed>";
        }

        return "<missing>";
    }

    private static bool HasValue(string value)
        => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("<", StringComparison.Ordinal);
}
