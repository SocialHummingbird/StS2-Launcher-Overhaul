using System;
using System.IO;
using System.Text.Json;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherRuntimeSlotEvidence
{
    internal const string MarkerFileName = "current_runtime_slot.json";

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
            PatchHelper.Log($"[Launcher] Failed to clear runtime slot evidence marker: {ex.Message}");
        }
    }

    internal static void Write(string dataDir, string branch, bool filesReady, string readinessProblem)
    {
        try
        {
            branch = SteamGameBranch.Normalize(branch);
            var slot = GameRuntimeSlot.Inspect(dataDir, branch);
            Write(dataDir, slot, filesReady, readinessProblem);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write runtime slot evidence marker: {ex.Message}");
        }
    }

    internal static void Write(string dataDir, GameRuntimeSlot slot, bool filesReady, string readinessProblem)
    {
        try
        {
            var branch = SteamGameBranch.Normalize(slot.Branch);
            var payload = new
            {
                utc = DateTime.UtcNow.ToString("O"),
                branch,
                selectedVersion = SteamGameBranch.DisplayName(branch),
                selectedVersionSlotKind = SteamGameInstallPaths.VersionSlotKind(branch),
                selectedVersionSlotDirectory = SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch),
                runtimeSlotId = slot.RuntimeSlotId,
                runtimeSlotIdentity = slot.RuntimeSlotIdentity,
                filesReady,
                readinessProblem = string.IsNullOrWhiteSpace(readinessProblem) ? string.Empty : readinessProblem,
                playable = slot.Playable,
                runtimeCompatible = slot.RuntimeCompatible,
                patchCompatible = slot.PatchCompatible,
                branchMatchedAndroidRuntimePrepared = slot.BranchMatchedAndroidRuntimePrepared,
                requiresUsableRuntimePack = slot.RequiresRuntimePackOrPreparedCache,
                requiresRuntimePackOrPreparedCache = slot.RequiresRuntimePackOrPreparedCache,
                runtimePairingStatus = slot.RuntimePairingStatus,
                pckPath = slot.PckPath,
                pckSha256 = slot.PckSha256,
                sourceAssemblyPath = slot.SourceAssemblyPath,
                sourceAssemblySha256 = slot.SourceAssemblySha256,
                activeAndroidAssemblyPath = slot.ActiveAndroidAssemblyPath,
                activeAndroidAssemblySha256 = slot.ActiveAndroidAssemblySha256,
                releaseVersion = slot.Metadata.ReleaseVersion,
                releaseCommit = slot.Metadata.ReleaseCommit,
                releaseBuildId = slot.Metadata.ReleaseBuildId,
                depotManifestCount = slot.Metadata.DepotManifestCount,
                depotManifestFingerprint = slot.Metadata.DepotManifestFingerprint,
                runtimePackManifestPath = slot.RuntimePackManifestPath,
                runtimePackManifestPresent = slot.RuntimePackManifestExists,
                runtimePackId = slot.RuntimePack.PackId,
                runtimePackStatus = slot.RuntimePack.Status,
                runtimePackUsabilityStatus = slot.RuntimePackUsabilityStatus,
                runtimePackUsable = slot.RuntimePackUsable,
                runtimePackSourceRuntimeSlotId = slot.RuntimePack.SourceRuntimeSlotId,
                runtimePackSourceRuntimeSlotIdMatchesSelected = slot.RuntimePackSlotIdMatches,
                patchCompatibilitySource = slot.PatchCompatibility.Source,
                patchCompatibilityStatus = slot.PatchCompatibility.Status,
                patchCompatibilityDetail = slot.PatchCompatibility.Detail,
                patchCompatibilityMarkerPath = slot.PatchCompatibility.MarkerPath,
                patchCompatibilityValidationMode = slot.PatchCompatibility.ValidationMode,
                patchCompatibilityValidationSurfaceVersion = slot.PatchCompatibility.ValidationSurfaceVersion,
                patchCompatibilityMissingSymbolCount = slot.PatchCompatibility.MissingSymbolCount
            };

            File.WriteAllText(
                MarkerPath(dataDir),
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })
            );
            PatchHelper.Log($"[Launcher] Runtime slot evidence marker written: branch={branch} slot={slot.RuntimeSlotId} playable={slot.Playable} path={MarkerPath(dataDir)}");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write runtime slot evidence marker: {ex.Message}");
        }
    }

    internal static string RuntimeSlotId(string dataDir)
        => ReadString(dataDir, "runtimeSlotId");

    internal static string Branch(string dataDir)
        => ReadString(dataDir, "branch");

    internal static string FilesReady(string dataDir)
        => ReadString(dataDir, "filesReady");

    internal static string ReadinessProblem(string dataDir)
        => ReadString(dataDir, "readinessProblem");

    internal static string RuntimePackUsabilityStatus(string dataDir)
        => ReadString(dataDir, "runtimePackUsabilityStatus");

    internal static string PatchCompatibilityStatus(string dataDir)
        => ReadString(dataDir, "patchCompatibilityStatus");

    internal static bool BranchMatchesSelectedRuntime(string dataDir, string branch)
    {
        var markerBranch = Branch(dataDir);
        if (IsMissing(markerBranch))
            return false;

        return string.Equals(
            SteamGameBranch.Normalize(markerBranch),
            SteamGameBranch.Normalize(branch),
            StringComparison.OrdinalIgnoreCase
        );
    }

    internal static bool RuntimeSlotIdMatchesSelectedRuntime(string dataDir, string branch)
    {
        var markerRuntimeSlotId = RuntimeSlotId(dataDir);
        if (IsMissing(markerRuntimeSlotId))
            return false;

        var selectedRuntime = GameRuntimeSlot.Inspect(dataDir, branch);
        return string.Equals(
            markerRuntimeSlotId,
            selectedRuntime.RuntimeSlotId,
            StringComparison.OrdinalIgnoreCase
        );
    }

    internal static bool PckMatchesSelectedRuntime(string dataDir, string branch)
    {
        var markerPck = ReadString(dataDir, "pckSha256");
        if (IsMissing(markerPck))
            return false;

        var selectedRuntime = GameRuntimeSlot.Inspect(dataDir, branch);
        return string.Equals(
            markerPck,
            selectedRuntime.PckSha256,
            StringComparison.OrdinalIgnoreCase
        );
    }

    internal static bool SourceAssemblyMatchesSelectedRuntime(string dataDir, string branch)
    {
        var markerAssembly = ReadString(dataDir, "sourceAssemblySha256");
        if (IsMissing(markerAssembly))
            return false;

        var selectedRuntime = GameRuntimeSlot.Inspect(dataDir, branch);
        return string.Equals(
            markerAssembly,
            selectedRuntime.SourceAssemblySha256,
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static bool IsMissing(string value)
        => string.IsNullOrWhiteSpace(value) || value.StartsWith("<", StringComparison.Ordinal);

    private static string ReadString(string dataDir, string property)
    {
        try
        {
            if (!File.Exists(MarkerPath(dataDir)))
                return "<none>";

            using var document = JsonDocument.Parse(File.ReadAllText(MarkerPath(dataDir)));
            if (!document.RootElement.TryGetProperty(property, out var value))
                return "<missing>";

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? "<missing>",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => value.ToString(),
                _ => "<unsupported>"
            };
        }
        catch
        {
            return "<read failed>";
        }
    }
}
