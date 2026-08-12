using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal sealed record LauncherModLaunchResult(
    string Id,
    string Result,
    string Detail
);

internal static class LauncherModLaunchResultStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    internal static void WriteVanilla(LauncherModSelectionDocument selection)
        => Write(
            LauncherModSelectionState.VanillaModeName,
            selection,
            discovered: 0,
            loaded: 0,
            Array.Empty<LauncherModLaunchResult>()
        );

    internal static void WritePlannedNotStarted(
        LauncherModSelectionDocument selection,
        LauncherModLaunchPlan plan
    )
        => Write(
            LauncherModSelectionState.ModdedModeName,
            selection,
            discovered: 0,
            loaded: 0,
            plan == null
                ? Array.Empty<LauncherModLaunchResult>()
                : plan.EnabledMods
                    .Select(mod => new LauncherModLaunchResult(
                        mod.ManifestId,
                        "Failed",
                        "Runtime activation has not completed."
                    ))
                    .ToArray()
        );

    internal static void WritePlanFailure(
        LauncherModSelectionDocument selection,
        LauncherModDiscoveryError error
    )
    {
        var selectedKeys = (selection?.EnabledMods ?? new Dictionary<string, bool>())
            .Where(pair => pair.Value)
            .Select(pair => pair.Key)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        Write(
            LauncherModSelectionState.ModdedModeName,
            selection,
            discovered: 0,
            loaded: 0,
            selectedKeys
                .Select(key => new LauncherModLaunchResult(
                    key,
                    "Failed",
                    string.Equals(key, error?.SelectionKey, StringComparison.OrdinalIgnoreCase)
                        || selectedKeys.Length == 1
                            ? $"Discovery failed: {error?.Code.ToString() ?? "Unknown"}."
                            : "Launch plan failed before this mod was loaded."
                ))
                .ToArray()
        );
    }

    internal static void Write(
        string launchMode,
        LauncherModSelectionDocument selection,
        int discovered,
        int loaded,
        IReadOnlyList<LauncherModLaunchResult> mods
    )
    {
        try
        {
            selection ??= new LauncherModSelectionDocument();
            mods ??= Array.Empty<LauncherModLaunchResult>();
            var shortResults = mods
                .Select(NormalizeResult)
                .ToArray();
            var payload = new
            {
                timestampUtc = DateTime.UtcNow.ToString("O"),
                launchMode,
                selectionFingerprint = LauncherModSelectionState.SelectionFingerprint(selection),
                discovered = Math.Max(0, discovered),
                loaded = Math.Max(0, loaded),
                active = shortResults.Count(mod => string.Equals(mod.Result, "Active", StringComparison.Ordinal)),
                partial = shortResults.Count(mod => string.Equals(mod.Result, "Partial", StringComparison.Ordinal)),
                failed = shortResults.Count(mod => string.Equals(mod.Result, "Failed", StringComparison.Ordinal)),
                mods = shortResults,
            };

            var markerPath = AppPaths.AppPrivateLastModLaunchPath;
            var parent = Path.GetDirectoryName(markerPath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            var tempPath = markerPath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(payload, JsonOptions));
            File.Move(tempPath, markerPath, overwrite: true);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to write mod launch result: {ex.Message}");
        }
    }

    private static LauncherModLaunchResult NormalizeResult(LauncherModLaunchResult result)
    {
        var normalizedResult = result?.Result switch
        {
            "Active" => "Active",
            "Partial" => "Partial",
            _ => "Failed",
        };
        return new LauncherModLaunchResult(
            ShortText(result?.Id, "unknown", 80),
            normalizedResult,
            ShortText(result?.Detail, "No activation result was reported.", 160)
        );
    }

    private static string ShortText(string value, string fallback, int maxLength)
    {
        value = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        value = value.Replace('\r', ' ').Replace('\n', ' ');
        while (value.Contains("  ", StringComparison.Ordinal))
            value = value.Replace("  ", " ", StringComparison.Ordinal);
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
