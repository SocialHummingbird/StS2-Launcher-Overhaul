using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherRuntimePatchValidationEvidence
{
    internal const string MarkerFileName = "last_runtime_patch_validation.json";
    private const string RuntimeCacheMarkerFileName = "current_runtime_cache.txt";

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
            PatchHelper.Log($"[Launcher] Failed to clear runtime patch validation evidence: {ex.Message}");
        }
    }

    internal static void Write(string dataDir, STS2Mobile.StartupPatchOrchestrator.StartupPatchResult result)
    {
        try
        {
            var branch = LauncherPreferences.ReadGameBranch();
            var failures = result.FailureMessages().Take(20).ToArray();
            var status = result.CriticalFailed
                ? "critical_failed"
                : result.HasFailures
                    ? "passed_with_noncritical_failures"
                    : "passed";
            var runtimeId = RuntimeCacheValue(dataDir, "Runtime ID:");
            var selectedPckSha256 = RuntimeCacheValue(dataDir, "Selected PCK SHA256:");
            var selectedSourceAssemblySha256 = RuntimeCacheValue(dataDir, "Selected source sts2.dll SHA256:");
            var activeAndroidAssemblySha256 = RuntimeCacheValue(dataDir, "Active source sts2.dll SHA256:");
            var runtimePackDirectory = RuntimeCacheValue(dataDir, "Runtime pack directory:");
            var runtimePackGameAssembly = RuntimeCacheValue(dataDir, "Runtime pack game assembly:");
            var slot = GameRuntimeSlot.Inspect(dataDir, branch);

            var payload = new
            {
                status,
                utc = DateTime.UtcNow.ToString("O"),
                selectedBranch = SteamGameBranch.Normalize(branch),
                selectedVersion = SteamGameBranch.DisplayName(branch),
                selectedVersionSlotKind = SteamGameInstallPaths.VersionSlotKind(branch),
                selectedVersionSlotDirectory = SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch),
                runtimeSlotId = slot.RuntimeSlotId,
                runtimeCacheId = runtimeId,
                selectedPckSha256,
                selectedSourceAssemblySha256,
                activeAndroidAssemblySha256,
                runtimePackId = slot.RuntimePack?.PackId ?? runtimePackDirectory,
                runtimePackStatus = RuntimePackStatus(runtimePackDirectory, runtimePackGameAssembly),
                patchCompatibleBeforeLaunch = !result.CriticalFailed,
                runtimeCompatibleBeforeLaunch = !string.IsNullOrWhiteSpace(activeAndroidAssemblySha256)
                    && !activeAndroidAssemblySha256.StartsWith("<", StringComparison.Ordinal),
                playableBeforeLaunch = !result.CriticalFailed
                    && !string.IsNullOrWhiteSpace(activeAndroidAssemblySha256)
                    && !activeAndroidAssemblySha256.StartsWith("<", StringComparison.Ordinal),
                criticalFailed = result.CriticalFailed,
                hasFailures = result.HasFailures,
                appliedPatchCount = result.AppliedPatchCount,
                failedPatchCount = result.FailedPatchCount,
                totalPatchCount = result.TotalPatchCount,
                durationMs = result.Duration.TotalMilliseconds,
                failureMessages = failures
            };

            File.WriteAllText(
                MarkerPath(dataDir),
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write runtime patch validation evidence: {ex.Message}");
        }
    }

    private static string RuntimeCacheValue(string dataDir, string prefix)
    {
        try
        {
            var path = Path.Combine(dataDir, RuntimeCacheMarkerFileName);
            if (!File.Exists(path))
                return "<missing>";

            foreach (var line in File.ReadLines(path))
            {
                if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = line[prefix.Length..].Trim();
                return string.IsNullOrWhiteSpace(value) ? "<empty>" : value;
            }

            return "<missing>";
        }
        catch (Exception ex)
        {
            return $"<unavailable:{ex.GetType().Name}>";
        }
    }

    private static string RuntimePackStatus(string runtimePackDirectory, string runtimePackGameAssembly)
    {
        if (string.IsNullOrWhiteSpace(runtimePackDirectory) || runtimePackDirectory == "<none>")
            return "not required";
        if (string.IsNullOrWhiteSpace(runtimePackGameAssembly) || runtimePackGameAssembly == "<none>")
            return "missing runtime pack assembly";
        return "runtime pack selected";
    }

    internal static string Status(string dataDir)
        => ReadString(dataDir, "status");

    internal static string Utc(string dataDir)
        => ReadString(dataDir, "utc");

    internal static bool UtcParseable(string dataDir)
        => DateTime.TryParse(
            Utc(dataDir),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out _
        );

    internal static string SelectedBranch(string dataDir)
        => ReadString(dataDir, "selectedBranch");

    internal static string SelectedVersion(string dataDir)
        => ReadString(dataDir, "selectedVersion");

    internal static string SelectedPckSha256(string dataDir)
        => ReadString(dataDir, "selectedPckSha256");

    internal static string SelectedSourceAssemblySha256(string dataDir)
        => ReadString(dataDir, "selectedSourceAssemblySha256");

    internal static string RuntimeSlotId(string dataDir)
        => ReadString(dataDir, "runtimeSlotId");

    internal static string ActiveAndroidAssemblySha256(string dataDir)
        => ReadString(dataDir, "activeAndroidAssemblySha256");

    internal static string RuntimePackId(string dataDir)
        => ReadString(dataDir, "runtimePackId");

    internal static string RuntimePackStatus(string dataDir)
        => ReadString(dataDir, "runtimePackStatus");

    internal static string AppliedPatchCount(string dataDir)
        => ReadString(dataDir, "appliedPatchCount");

    internal static string FailedPatchCount(string dataDir)
        => ReadString(dataDir, "failedPatchCount");

    internal static string TotalPatchCount(string dataDir)
        => ReadString(dataDir, "totalPatchCount");

    internal static string FailureMessages(string dataDir)
    {
        try
        {
            if (!File.Exists(MarkerPath(dataDir)))
                return "<none>";

            using var document = JsonDocument.Parse(File.ReadAllText(MarkerPath(dataDir)));
            if (!document.RootElement.TryGetProperty("failureMessages", out var failures)
                || failures.ValueKind != JsonValueKind.Array)
            {
                return "<missing>";
            }

            var selected = failures
                .EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Take(10)
                .ToArray();
            return selected.Length == 0 ? "<none>" : string.Join(" | ", selected);
        }
        catch
        {
            return "<read failed>";
        }
    }

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
