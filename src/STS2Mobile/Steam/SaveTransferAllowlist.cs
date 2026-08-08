using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static class SaveTransferAllowlist
{
    internal const int ProfileCount = 3;
    internal const int RunHistoryLimit = 100;
    internal const string SharedProfilePath = "profile.save";
    internal const string ProgressFile = "progress.save";
    internal const string LegacyPreferencesFile = "prefs";
    internal const string PreferencesFile = "prefs.save";
    internal const string CurrentRunFile = "current_run.save";
    internal const string CurrentMultiplayerRunFile = "current_run_mp.save";

    internal static IReadOnlyList<string> FixedProfileFiles { get; } =
        new[]
        {
            ProgressFile,
            LegacyPreferencesFile,
            PreferencesFile,
            CurrentRunFile,
            CurrentMultiplayerRunFile,
        };

    internal static string SaveDirectory(
        SaveNamespace saveNamespace,
        int profileId
    )
    {
        if (profileId < 1 || profileId > ProfileCount)
            throw new ArgumentOutOfRangeException(nameof(profileId));

        var prefix = saveNamespace == SaveNamespace.Modded ? "modded/" : "";
        return $"{prefix}profile{profileId}/saves";
    }

    internal static string HistoryDirectory(
        SaveNamespace saveNamespace,
        int profileId
    )
        => $"{SaveDirectory(saveNamespace, profileId)}/history";

    internal static bool IsAllowedPath(
        SaveNamespace saveNamespace,
        string path
    )
    {
        var canonical = CloudSavePath.Relative(path);
        if (string.Equals(
                canonical,
                SharedProfilePath,
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return true;
        }

        for (var profileId = 1; profileId <= ProfileCount; profileId++)
        {
            var saveDirectory = SaveDirectory(saveNamespace, profileId);
            foreach (var fixedFile in FixedProfileFiles)
            {
                if (string.Equals(
                        canonical,
                        $"{saveDirectory}/{fixedFile}",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    return true;
                }
            }

            var historyPrefix = HistoryDirectory(saveNamespace, profileId) + "/";
            if (
                canonical.StartsWith(
                    historyPrefix,
                    StringComparison.OrdinalIgnoreCase
                )
                && IsAllowedHistoryFilename(canonical[historyPrefix.Length..])
            )
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsAllowedBackupCleanupPath(
        SaveNamespace saveNamespace,
        string path
    )
    {
        const string suffix = ".backup";
        var canonical = CloudSavePath.Relative(path);
        return canonical.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            && IsAllowedPath(
                saveNamespace,
                canonical[..^suffix.Length]
            );
    }

    internal static bool IsAllowedHistoryFilename(string filename)
        => filename.Length > ".run".Length
            && !filename.Contains('/')
            && !filename.Contains('\\')
            && filename.EndsWith(".run", StringComparison.OrdinalIgnoreCase);

    internal static bool IsAllowedHistoryBackupFilename(string filename)
    {
        const string suffix = ".backup";
        return filename.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            && IsAllowedHistoryFilename(filename[..^suffix.Length]);
    }

    internal static bool HasAnyContent(
        ISaveStore store,
        SaveNamespace saveNamespace
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        for (var profileId = 1; profileId <= ProfileCount; profileId++)
        {
            var saveDirectory = SaveDirectory(saveNamespace, profileId);
            foreach (var fixedFile in FixedProfileFiles)
            {
                if (HasContent(store, $"{saveDirectory}/{fixedFile}"))
                    return true;
            }

            var historyDirectory = HistoryDirectory(saveNamespace, profileId);
            try
            {
                if (!store.DirectoryExists(historyDirectory))
                    continue;

                foreach (var filename in store.GetFilesInDirectory(historyDirectory))
                {
                    if (
                        IsAllowedHistoryFilename(filename)
                        && HasContent(store, $"{historyDirectory}/{filename}")
                    )
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                PatchHelper.Log(
                    $"[Cloud] Could not inspect allowlisted history "
                        + $"directory {historyDirectory}: {ex.Message}"
                );
            }
        }

        return false;
    }

    private static bool HasContent(ISaveStore store, string path)
    {
        try
        {
            return store.FileExists(path) && store.GetFileSize(path) > 0;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Could not inspect allowlisted save {path}: {ex.Message}"
            );
            return false;
        }
    }
}
