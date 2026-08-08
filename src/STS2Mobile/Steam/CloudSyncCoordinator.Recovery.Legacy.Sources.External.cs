#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static readonly Regex ExternalTimestampedBackupName = new(
        "^(?<name>profile\\.save|progress\\.save|prefs|prefs\\.save|"
            + "current_run\\.save|current_run_mp\\.save|.+\\.run)\\."
            + "(?<stamp>[0-9]{10})\\."
            + "(?<source>cloud|local|cloud-pre-push|local-pre-push|local-pre-pull)"
            + "\\.bak$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
    );

    private static readonly Regex ExternalTimestampedSettingsBackupName = new(
        "^settings\\.save\\.(?<stamp>[0-9]{10})\\."
            + "(?<source>cloud|local|cloud-pre-push|local-pre-push|local-pre-pull)"
            + "\\.bak$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
    );

    private sealed partial class LegacyRecoveryScanSession
    {
        private async Task ScanExternalBackupsAsync(
            CancellationToken cancellationToken
        )
        {
            if (_externalSavesRoot is null
                || !Directory.Exists(_externalSavesRoot))
            {
                return;
            }

            var currentRoot = Path.Combine(
                _externalSavesRoot,
                LocalSaveBackupPlan.CurrentDirectoryName
            );
            await ScanExternalGenerationAsync(
                currentRoot,
                LegacyRecoverySourceKind.ExternalCurrent,
                "External Current mirror",
                cancellationToken
            ).ConfigureAwait(false);
            AddExternalSettingsExportOnly(currentRoot, cancellationToken);

            var historyRoot = Path.Combine(
                _externalSavesRoot,
                LocalSaveBackupPlan.HistoryDirectoryName
            );
            foreach (var generation in ExternalDirectories(historyRoot)
                .Where(path => IsExternalHistoryGeneration(Path.GetFileName(path)))
                .OrderByDescending(path => Path.GetFileName(path), StringComparer.Ordinal)
                .Take(LocalSaveBackupPlan.MaxHistoryGenerations))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var generationName = Path.GetFileName(generation);
                await ScanExternalGenerationAsync(
                    generation,
                    LegacyRecoverySourceKind.ExternalHistory,
                    $"External History generation {generationName}",
                    cancellationToken
                ).ConfigureAwait(false);
                AddExternalSettingsExportOnly(generation, cancellationToken);
            }

            await ScanExternalTimestampedBackupsAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task ScanExternalGenerationAsync(
            string root,
            LegacyRecoverySourceKind sourceKind,
            string sourceLabel,
            CancellationToken cancellationToken
        )
        {
            if (!Directory.Exists(root))
                return;
            var shared = ExternalFileInput(
                SaveTransferAllowlist.SharedProfilePath,
                root,
                SaveTransferAllowlist.SharedProfilePath
            );
            var vanilla = CollectExternalBackupTree(
                root,
                SaveNamespace.Vanilla,
                cancellationToken
            );
            var modded = CollectExternalBackupTree(
                root,
                SaveNamespace.Modded,
                cancellationToken
            );

            if (vanilla.Count != 0)
            {
                if (shared is not null)
                    vanilla.Add(shared);
                await ImportPartialGroupAsync(
                    sourceKind,
                    $"{sourceLabel} vanilla",
                    vanilla,
                    ProvenanceFromNamespace(SaveNamespace.Vanilla),
                    contextMarker: string.Empty,
                    invalidContext: false,
                    initialProblems: null,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            if (modded.Count != 0)
            {
                if (shared is not null)
                    modded.Add(shared);
                await ImportPartialGroupAsync(
                    sourceKind,
                    $"{sourceLabel} modded",
                    modded,
                    ProvenanceFromNamespace(SaveNamespace.Modded),
                    contextMarker: string.Empty,
                    invalidContext: false,
                    initialProblems: null,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            if (vanilla.Count == 0 && modded.Count == 0 && shared is not null)
            {
                await ImportPartialGroupAsync(
                    sourceKind,
                    $"{sourceLabel} shared profile",
                    new[] { shared },
                    ProvenanceFromNamespace(saveNamespace: null),
                    contextMarker: string.Empty,
                    invalidContext: false,
                    initialProblems: null,
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }

        private List<LegacyRecoveryFileInput> CollectExternalBackupTree(
            string root,
            SaveNamespace saveNamespace,
            CancellationToken cancellationToken
        )
        {
            var inputs = new List<LegacyRecoveryFileInput>();
            for (
                var profileId = 1;
                profileId <= SaveTransferAllowlist.ProfileCount;
                profileId++
            )
            {
                var saveDirectory = SaveTransferAllowlist.SaveDirectory(
                    saveNamespace,
                    profileId
                );
                foreach (var filename in SaveTransferAllowlist.FixedProfileFiles)
                {
                    var target = $"{saveDirectory}/{filename}";
                    var input = ExternalFileInput(target, root, target);
                    if (input is not null)
                        inputs.Add(input);
                }

                var historyDirectory = SaveTransferAllowlist.HistoryDirectory(
                    saveNamespace,
                    profileId
                );
                if (!TryResolveExternalPath(
                        root,
                        historyDirectory,
                        out var fullHistory
                    )
                    || !Directory.Exists(fullHistory))
                {
                    continue;
                }
                foreach (var file in ExternalFiles(fullHistory)
                    .Where(path => SaveTransferAllowlist.IsAllowedHistoryFilename(
                        Path.GetFileName(path)
                    ))
                    .OrderByDescending(path => Path.GetFileName(path), StringComparer.Ordinal)
                    .Take(SaveTransferAllowlist.RunHistoryLimit))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var target = $"{historyDirectory}/{Path.GetFileName(file)}";
                    inputs.Add(ExternalInput(
                        target,
                        file,
                        ExternalModifiedUtc(file)
                    ));
                }
            }
            return inputs;
        }

        private async Task ScanExternalTimestampedBackupsAsync(
            CancellationToken cancellationToken
        )
        {
            if (_externalSavesRoot is null)
                return;
            var locations = new List<(string Directory, SaveNamespace? Namespace, int ProfileId)>
            {
                (Path.Combine(_externalSavesRoot, "default"), null, 0),
            };
            for (
                var profileId = 1;
                profileId <= SaveTransferAllowlist.ProfileCount;
                profileId++
            )
            {
                locations.Add((
                    Path.Combine(_externalSavesRoot, $"profile{profileId}"),
                    SaveNamespace.Vanilla,
                    profileId
                ));
                locations.Add((
                    Path.Combine(
                        _externalSavesRoot,
                        "modded",
                        $"profile{profileId}"
                    ),
                    SaveNamespace.Modded,
                    profileId
                ));
            }

            foreach (var location in locations)
            {
                foreach (var file in ExternalFiles(location.Directory)
                    .Where(path => path.EndsWith(
                        ".bak",
                        StringComparison.OrdinalIgnoreCase
                    ))
                    .Take(LegacyRecoveryFilesPerDirectoryLimit))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var filename = Path.GetFileName(file);
                    var settingsMatch = ExternalTimestampedSettingsBackupName.Match(
                        filename
                    );
                    if (settingsMatch.Success)
                    {
                        AddExportOnlyFile(
                            file,
                            "Device settings are export-only and are never a restorable save snapshot"
                        );
                        continue;
                    }

                    var match = ExternalTimestampedBackupName.Match(filename);
                    if (!match.Success
                        || !TryTimestampedBackupTarget(
                            match.Groups["name"].Value,
                            location.Namespace,
                            location.ProfileId,
                            out var target
                        )
                        || !long.TryParse(
                            match.Groups["stamp"].Value,
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out var unixSeconds
                        ))
                    {
                        continue;
                    }

                    DateTimeOffset capturedUtc;
                    try
                    {
                        capturedUtc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        continue;
                    }
                    await ImportPartialGroupAsync(
                        LegacyRecoverySourceKind.ExternalTimestampedBackup,
                        $"External timestamped backup {filename}",
                        new[] { ExternalInput(target, file, capturedUtc) },
                        ProvenanceFromNamespace(location.Namespace),
                        contextMarker: string.Empty,
                        invalidContext: false,
                        initialProblems: null,
                        cancellationToken
                    ).ConfigureAwait(false);
                }
            }
        }

        private static bool TryTimestampedBackupTarget(
            string filename,
            SaveNamespace? saveNamespace,
            int profileId,
            out string target
        )
        {
            target = string.Empty;
            if (!saveNamespace.HasValue)
            {
                if (!string.Equals(
                        filename,
                        SaveTransferAllowlist.SharedProfilePath,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    return false;
                }
                target = SaveTransferAllowlist.SharedProfilePath;
                return true;
            }
            if (profileId < 1 || profileId > SaveTransferAllowlist.ProfileCount)
                return false;

            var saveDirectory = SaveTransferAllowlist.SaveDirectory(
                saveNamespace.Value,
                profileId
            );
            if (SaveTransferAllowlist.FixedProfileFiles.Contains(
                    filename,
                    StringComparer.OrdinalIgnoreCase
                ))
            {
                target = $"{saveDirectory}/{filename}";
                return true;
            }
            if (!SaveTransferAllowlist.IsAllowedHistoryFilename(filename))
                return false;
            target = $"{SaveTransferAllowlist.HistoryDirectory(
                saveNamespace.Value,
                profileId
            )}/{filename}";
            return true;
        }

        private LegacyRecoveryFileInput? ExternalFileInput(
            string targetPath,
            string root,
            string relativePath
        )
        {
            if (!TryResolveExternalPath(root, relativePath, out var fullPath)
                || !File.Exists(fullPath))
            {
                return null;
            }
            return ExternalInput(
                targetPath,
                fullPath,
                ExternalModifiedUtc(fullPath)
            );
        }

        private static LegacyRecoveryFileInput ExternalInput(
            string targetPath,
            string fullPath,
            DateTimeOffset modifiedUtc
        )
            => new(
                targetPath,
                fullPath,
                modifiedUtc,
                cancellationToken => File.ReadAllBytesAsync(
                    fullPath,
                    cancellationToken
                )
            );

        private void AddExternalSettingsExportOnly(
            string root,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryResolveExternalPath(
                    root,
                    "settings.save",
                    out var settingsPath
                )
                || !File.Exists(settingsPath))
            {
                return;
            }
            AddExportOnlyFile(
                settingsPath,
                "Device settings are export-only and are never a restorable save snapshot"
            );
        }

        private void AddExportOnlyFile(string path, string reason)
        {
            _sourceFilesExamined++;
            try
            {
                var info = new FileInfo(path);
                _exportOnly.Add(new LegacyRecoveryExportOnlyFile(
                    path,
                    info.Exists ? info.Length : 0,
                    info.Exists
                        ? new DateTimeOffset(info.LastWriteTimeUtc).ToUniversalTime()
                        : DateTimeOffset.UnixEpoch,
                    reason
                ));
            }
            catch
            {
                _exportOnly.Add(new LegacyRecoveryExportOnlyFile(
                    path,
                    0,
                    DateTimeOffset.UnixEpoch,
                    reason
                ));
            }
        }

        private static bool TryResolveExternalPath(
            string root,
            string relativePath,
            out string fullPath
        )
            => LocalSaveBackupPlan.TryResolveUnderRoot(
                root,
                relativePath,
                out fullPath
            );

        private static IReadOnlyList<string> ExternalFiles(string directory)
        {
            try
            {
                return Directory.Exists(directory)
                    ? Directory.GetFiles(directory)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .Take(LegacyRecoveryFilesPerDirectoryLimit)
                        .ToArray()
                    : Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static IReadOnlyList<string> ExternalDirectories(string directory)
        {
            try
            {
                return Directory.Exists(directory)
                    ? Directory.GetDirectories(directory)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .Take(LegacyRecoveryDirectoryLimit)
                        .ToArray()
                    : Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static DateTimeOffset ExternalModifiedUtc(string path)
        {
            try
            {
                return new DateTimeOffset(File.GetLastWriteTimeUtc(path))
                    .ToUniversalTime();
            }
            catch
            {
                return DateTimeOffset.UnixEpoch;
            }
        }

        private static bool IsExternalHistoryGeneration(string value)
            => DateTimeOffset.TryParseExact(
                value,
                "yyyyMMdd'T'HHmmssfffffff'Z'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out _
            );
    }
}
