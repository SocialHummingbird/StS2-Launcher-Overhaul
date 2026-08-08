#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const int LegacyRecoveryDirectoryLimit = 64;
    private const int LegacyRecoveryFilesPerDirectoryLimit = 256;
    private const string Stage2TransferBackupRoot =
        ".sts2-launcher/transfer-backups";
    private const string ManualPullBackupRoot =
        ".launcher_backups/manual-pull";
    private const string AutomaticSyncContextsRoot =
        ".sts2-launcher/automatic-sync/contexts";
    private const string LegacyAutomaticSyncSnapshotRoot =
        ".sts2-launcher/automatic-sync/snapshots";
    private const string LegacyAutomaticSyncBeforeGamePath =
        ".sts2-launcher/automatic-sync/before-game.json";

    private sealed record VerifiedStage3Source(
        string Path,
        AutomaticSaveSnapshot Snapshot,
        SaveContext? Context,
        DateTimeOffset ModifiedUtc
    );

    private sealed partial class LegacyRecoveryScanSession
    {
        private async Task ScanStage3SnapshotsAsync(
            CancellationToken cancellationToken
        )
        {
            var paths = DiscoverStage3SnapshotPaths(cancellationToken);
            var verified = new List<VerifiedStage3Source>();
            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _sourceFilesExamined++;
                try
                {
                    verified.Add(await ReadVerifiedStage3SourceAsync(
                        path,
                        cancellationToken
                    ).ConfigureAwait(false));
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    AddInvalidCandidate(
                        LegacyRecoverySourceKind.Stage3Snapshot,
                        $"Stage 3 snapshot {path}",
                        path,
                        ProvenanceFromNamespace(saveNamespace: null),
                        ex.Message,
                        LocalModifiedUtc(path)
                    );
                }
            }

            InferSteamIdFromStage3(verified);
            foreach (var source in verified)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var snapshot = UpgradeStage3Snapshot(source);
                var provenance = source.Context.HasValue
                    ? ProvenanceFromContext(source.Context.Value)
                    : ProvenanceFromNamespace(InferSnapshotNamespace(snapshot));
                var payloadBytes = snapshot.Files.Sum(file =>
                    (long)ReadSnapshotFileBytes(snapshot, file.Path).Length
                );
                await ImportSnapshotAsync(
                    LegacyRecoverySourceKind.Stage3Snapshot,
                    $"Stage 3 snapshot {source.Path}",
                    new[] { source.Path },
                    provenance,
                    snapshot,
                    overwriteOnly: snapshot.Coverage != "full",
                    invalid: false,
                    problems: Array.Empty<string>(),
                    payloadBytes,
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }

        private IReadOnlyList<string> DiscoverStage3SnapshotPaths(
            CancellationToken cancellationToken
        )
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var contextDirectory in LocalDirectories(
                AutomaticSyncContextsRoot,
                cancellationToken
            ).Take(LegacyRecoveryDirectoryLimit))
            {
                var contextRoot = $"{AutomaticSyncContextsRoot}/{contextDirectory}";
                var beforeGame = $"{contextRoot}/before-game.json";
                if (_local.FileExists(beforeGame))
                    paths.Add(beforeGame);
                AddJsonFiles(
                    paths,
                    $"{contextRoot}/snapshots",
                    cancellationToken
                );
            }

            AddJsonFiles(
                paths,
                LegacyAutomaticSyncSnapshotRoot,
                cancellationToken
            );
            if (_local.FileExists(LegacyAutomaticSyncBeforeGamePath))
                paths.Add(LegacyAutomaticSyncBeforeGamePath);
            return paths
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Take(LegacyRecoveryCandidateLimit)
                .ToArray();
        }

        private void AddJsonFiles(
            ISet<string> paths,
            string directory,
            CancellationToken cancellationToken
        )
        {
            foreach (var filename in LocalFiles(directory, cancellationToken)
                .Where(filename => filename.EndsWith(
                    ".json",
                    StringComparison.OrdinalIgnoreCase
                ))
                .OrderByDescending(filename => filename, StringComparer.Ordinal)
                .Take(LegacyRecoveryFilesPerDirectoryLimit))
            {
                paths.Add($"{directory}/{filename}");
            }
        }

        private async Task<VerifiedStage3Source> ReadVerifiedStage3SourceAsync(
            string path,
            CancellationToken cancellationToken
        )
        {
            var first = await CancellableSaveStore.ReadBytesAsync(
                _local,
                path,
                cancellationToken
            ).ConfigureAwait(false);
            var text = DecodeAutomaticSnapshotBytes(first, path);
            AutomaticSaveSnapshot preview;
            try
            {
                preview = JsonSerializer.Deserialize<AutomaticSaveSnapshot>(
                    text,
                    AutomaticSyncJsonOptions
                ) ?? throw new InvalidDataException("Stage 3 snapshot is empty");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException(
                    "Stage 3 snapshot is not valid JSON",
                    ex
                );
            }

            SaveContext? context = null;
            if (!string.IsNullOrWhiteSpace(preview.ContextMarker))
                context = SaveContext.ParseMarker(preview.ContextMarker);

            AutomaticSaveSnapshot verified;
            if (context.HasValue && IsCurrentStage3Path(path, context.Value))
            {
                verified = await ReadVerifiedSnapshotAsync(
                    _local,
                    path,
                    context.Value,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            else
            {
                ValidateAutomaticSnapshotPayload(
                    preview,
                    expectedContext: null,
                    allowUnknownContext: true
                );
                verified = preview;
            }

            var second = await CancellableSaveStore.ReadBytesAsync(
                _local,
                path,
                cancellationToken
            ).ConfigureAwait(false);
            if (!first.AsSpan().SequenceEqual(second))
            {
                throw new IOException(
                    "Stage 3 snapshot changed while recovery inspected it"
                );
            }

            return new VerifiedStage3Source(
                path,
                verified,
                context,
                LocalModifiedUtc(path)
            );
        }

        private static bool IsCurrentStage3Path(
            string path,
            SaveContext context
        )
        {
            if (string.Equals(
                    path,
                    AutomaticSyncBeforeGamePath(context),
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return true;
            }
            var filename = Path.GetFileNameWithoutExtension(
                path.Replace('/', Path.DirectorySeparatorChar)
            );
            return IsSha256(filename)
                && string.Equals(
                    path,
                    AutomaticSyncSnapshotPath(context, filename),
                    StringComparison.OrdinalIgnoreCase
                );
        }

        private void InferSteamIdFromStage3(
            IEnumerable<VerifiedStage3Source> sources
        )
        {
            if (_selection.SteamId64.HasValue)
                return;
            var matchingIds = sources
                .Where(source => source.Context.HasValue)
                .Select(source => source.Context!.Value)
                .Where(context => context.Namespace == _selection.Namespace)
                .Where(context => string.Equals(
                    context.RuntimeIdentity,
                    _selection.RuntimeIdentity,
                    StringComparison.Ordinal
                ))
                .Where(context => string.Equals(
                    context.ModSetFingerprint,
                    _selection.ModSetFingerprint,
                    StringComparison.Ordinal
                ))
                .Select(context => context.SteamId64)
                .Distinct()
                .ToArray();
            if (matchingIds.Length == 1)
                _selection = _selection.WithSteamId64(matchingIds[0]);
        }

        private static AutomaticSaveSnapshot UpgradeStage3Snapshot(
            VerifiedStage3Source source
        )
        {
            var original = source.Snapshot;
            var files = original.Manifest.Entries
                .Where(entry => entry.Exists)
                .OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Path, StringComparer.Ordinal)
                .Select(entry =>
                {
                    var bytes = ReadSnapshotFileBytes(original, entry.Path);
                    return new AutomaticSaveContentEntry
                    {
                        Path = CloudSavePath.Relative(entry.Path),
                        ContentBase64 = Convert.ToBase64String(bytes),
                        ByteSha256 = AutomaticSyncHash.Compute(bytes),
                    };
                })
                .ToList();
            return new AutomaticSaveSnapshot
            {
                ContextMarker = original.ContextMarker ?? string.Empty,
                Coverage = original.Version == AutomaticSyncLegacySnapshotVersion
                    ? "full"
                    : original.Coverage,
                SourceKind = "stage3-snapshot",
                SourceLabel = SnapshotSourceLabel(
                    $"Stage 3 snapshot {source.Path}"
                ),
                CapturedUtc = original.Version == AutomaticSyncLegacySnapshotVersion
                    ? NormalizeCapturedUtc(source.ModifiedUtc)
                    : NormalizeCapturedUtc(original.CapturedUtc),
                Manifest = AutomaticSaveManifest.Create(original.Manifest.Entries),
                Files = files,
            };
        }

        private static SaveNamespace? InferSnapshotNamespace(
            AutomaticSaveSnapshot snapshot
        )
        {
            var hasModded = snapshot.Manifest.Entries.Any(entry =>
                SaveTransferAllowlist.IsAllowedPath(SaveNamespace.Modded, entry.Path)
                && !SaveTransferAllowlist.IsAllowedPath(
                    SaveNamespace.Vanilla,
                    entry.Path
                ));
            if (hasModded)
                return SaveNamespace.Modded;
            var hasVanilla = snapshot.Manifest.Entries.Any(entry =>
                !string.Equals(
                    entry.Path,
                    SaveTransferAllowlist.SharedProfilePath,
                    StringComparison.OrdinalIgnoreCase
                )
                && SaveTransferAllowlist.IsAllowedPath(
                    SaveNamespace.Vanilla,
                    entry.Path
                ));
            return hasVanilla ? SaveNamespace.Vanilla : null;
        }

        private async Task ScanStage2TransferBackupsAsync(
            CancellationToken cancellationToken
        )
        {
            foreach (var operation in LocalDirectories(
                Stage2TransferBackupRoot,
                cancellationToken
            ).OrderByDescending(value => value, StringComparer.Ordinal)
                .Take(LegacyRecoveryDirectoryLimit))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var root = $"{Stage2TransferBackupRoot}/{operation}";
                var contextPath = $"{root}/context.json";
                if (!_local.FileExists(contextPath))
                    continue;

                SaveContext? context = null;
                string? contextProblem = null;
                _sourceFilesExamined++;
                try
                {
                    var marker = DecodeAutomaticSnapshotBytes(
                        await ReadStableLocalBytesAsync(
                            contextPath,
                            cancellationToken
                        ).ConfigureAwait(false),
                        contextPath
                    );
                    context = SaveContext.ParseMarker(marker);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    contextProblem = $"Invalid Stage 2 context.json: {ex.Message}";
                    AddInvalidCandidate(
                        LegacyRecoverySourceKind.Stage2TransferBackup,
                        $"Stage 2 transfer backup {operation}",
                        contextPath,
                        ProvenanceFromNamespace(saveNamespace: null),
                        contextProblem,
                        LocalModifiedUtc(contextPath)
                    );
                }

                var filesRoot = $"{root}/files";
                AddLocalSettingsExportOnly(filesRoot);
                if (context.HasValue)
                {
                    await ImportPartialGroupAsync(
                        LegacyRecoverySourceKind.Stage2TransferBackup,
                        $"Stage 2 transfer backup {operation}",
                        CollectLocalBackupTree(
                            filesRoot,
                            context.Value.Namespace,
                            includeSharedProfile: true,
                            allowBackupFallback: true,
                            cancellationToken
                        ),
                        ProvenanceFromContext(context.Value),
                        context.Value.SerializeMarker(),
                        invalidContext: false,
                        initialProblems: null,
                        cancellationToken
                    ).ConfigureAwait(false);
                    continue;
                }

                var problems = contextProblem is null
                    ? Array.Empty<string>()
                    : new[] { contextProblem };
                await ImportPartialGroupAsync(
                    LegacyRecoverySourceKind.Stage2TransferBackup,
                    $"Stage 2 transfer backup {operation} vanilla",
                    CollectLocalBackupTree(
                        filesRoot,
                        SaveNamespace.Vanilla,
                        includeSharedProfile: true,
                        allowBackupFallback: true,
                        cancellationToken
                    ),
                    ProvenanceFromNamespace(SaveNamespace.Vanilla),
                    contextMarker: string.Empty,
                    invalidContext: true,
                    problems,
                    cancellationToken
                ).ConfigureAwait(false);
                await ImportPartialGroupAsync(
                    LegacyRecoverySourceKind.Stage2TransferBackup,
                    $"Stage 2 transfer backup {operation} modded",
                    CollectLocalBackupTree(
                        filesRoot,
                        SaveNamespace.Modded,
                        includeSharedProfile: false,
                        allowBackupFallback: true,
                        cancellationToken
                    ),
                    ProvenanceFromNamespace(SaveNamespace.Modded),
                    contextMarker: string.Empty,
                    invalidContext: true,
                    problems,
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }

        private async Task ScanLiveSidecarsAsync(
            CancellationToken cancellationToken
        )
        {
            foreach (var filename in LocalFiles(string.Empty, cancellationToken))
            {
                if (!TryMapSidecarFilename(filename, out var targetName)
                    || !string.Equals(
                        targetName,
                        SaveTransferAllowlist.SharedProfilePath,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    continue;
                }
                await ImportLiveSidecarAsync(
                    filename,
                    targetName,
                    saveNamespace: null,
                    cancellationToken
                ).ConfigureAwait(false);
            }

            foreach (var saveNamespace in new[]
                {
                    SaveNamespace.Vanilla,
                    SaveNamespace.Modded,
                })
            {
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
                    foreach (var filename in LocalFiles(
                        saveDirectory,
                        cancellationToken
                    ))
                    {
                        if (!TryMapSidecarFilename(filename, out var targetName)
                            || !SaveTransferAllowlist.FixedProfileFiles.Contains(
                                targetName,
                                StringComparer.OrdinalIgnoreCase
                            ))
                        {
                            continue;
                        }
                        await ImportLiveSidecarAsync(
                            $"{saveDirectory}/{filename}",
                            $"{saveDirectory}/{targetName}",
                            saveNamespace,
                            cancellationToken
                        ).ConfigureAwait(false);
                    }

                    var historyDirectory = SaveTransferAllowlist.HistoryDirectory(
                        saveNamespace,
                        profileId
                    );
                    foreach (var filename in LocalFiles(
                        historyDirectory,
                        cancellationToken
                    ))
                    {
                        if (!TryMapSidecarFilename(filename, out var targetName)
                            || !SaveTransferAllowlist.IsAllowedHistoryFilename(
                                targetName
                            ))
                        {
                            continue;
                        }
                        await ImportLiveSidecarAsync(
                            $"{historyDirectory}/{filename}",
                            $"{historyDirectory}/{targetName}",
                            saveNamespace,
                            cancellationToken
                        ).ConfigureAwait(false);
                    }
                }
            }
        }

        private Task ImportLiveSidecarAsync(
            string sourcePath,
            string targetPath,
            SaveNamespace? saveNamespace,
            CancellationToken cancellationToken
        )
            => ImportPartialGroupAsync(
                LegacyRecoverySourceKind.LiveSidecar,
                $"Live save sidecar {sourcePath}",
                new[] { LocalInput(targetPath, sourcePath) },
                ProvenanceFromNamespace(saveNamespace),
                contextMarker: string.Empty,
                invalidContext: false,
                initialProblems: null,
                cancellationToken
            );

        private async Task ScanManualPullBackupsAsync(
            CancellationToken cancellationToken
        )
        {
            foreach (var generation in LocalDirectories(
                ManualPullBackupRoot,
                cancellationToken
            ).Where(IsManualPullGeneration)
                .OrderByDescending(value => value, StringComparer.Ordinal)
                .Take(LegacyRecoveryDirectoryLimit))
            {
                var root = $"{ManualPullBackupRoot}/{generation}";
                await ImportPartialGroupAsync(
                    LegacyRecoverySourceKind.ManualPullBackup,
                    $"Legacy manual Pull backup {generation}",
                    CollectLocalBackupTree(
                        root,
                        SaveNamespace.Modded,
                        includeSharedProfile: false,
                        allowBackupFallback: false,
                        cancellationToken
                    ),
                    ProvenanceFromNamespace(SaveNamespace.Modded),
                    contextMarker: string.Empty,
                    invalidContext: false,
                    initialProblems: null,
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }

        private IReadOnlyList<LegacyRecoveryFileInput> CollectLocalBackupTree(
            string root,
            SaveNamespace saveNamespace,
            bool includeSharedProfile,
            bool allowBackupFallback,
            CancellationToken cancellationToken
        )
        {
            var inputs = new List<LegacyRecoveryFileInput>();
            if (includeSharedProfile)
            {
                AddPreferredLocalInput(
                    inputs,
                    SaveTransferAllowlist.SharedProfilePath,
                    $"{root}/{SaveTransferAllowlist.SharedProfilePath}",
                    allowBackupFallback
                );
            }

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
                    AddPreferredLocalInput(
                        inputs,
                        target,
                        $"{root}/{target}",
                        allowBackupFallback
                    );
                }

                var historyDirectory = SaveTransferAllowlist.HistoryDirectory(
                    saveNamespace,
                    profileId
                );
                var sourceHistory = $"{root}/{historyDirectory}";
                var selected = new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase
                );
                foreach (var filename in LocalFiles(
                    sourceHistory,
                    cancellationToken
                ))
                {
                    if (SaveTransferAllowlist.IsAllowedHistoryFilename(filename))
                    {
                        selected[filename] = filename;
                    }
                    else if (allowBackupFallback
                        && SaveTransferAllowlist.IsAllowedHistoryBackupFilename(
                            filename
                        ))
                    {
                        var targetName = filename[..^".backup".Length];
                        selected.TryAdd(targetName, filename);
                    }
                }
                foreach (var pair in selected
                    .OrderByDescending(pair => pair.Key, StringComparer.Ordinal)
                    .Take(SaveTransferAllowlist.RunHistoryLimit))
                {
                    inputs.Add(LocalInput(
                        $"{historyDirectory}/{pair.Key}",
                        $"{sourceHistory}/{pair.Value}"
                    ));
                }
            }
            return inputs;
        }

        private void AddPreferredLocalInput(
            ICollection<LegacyRecoveryFileInput> inputs,
            string targetPath,
            string sourcePath,
            bool allowBackupFallback
        )
        {
            if (_local.FileExists(sourcePath))
            {
                inputs.Add(LocalInput(targetPath, sourcePath));
                return;
            }
            var backupPath = $"{sourcePath}.backup";
            if (allowBackupFallback && _local.FileExists(backupPath))
                inputs.Add(LocalInput(targetPath, backupPath));
        }

        private LegacyRecoveryFileInput LocalInput(
            string targetPath,
            string sourcePath
        )
            => new(
                targetPath,
                sourcePath,
                LocalModifiedUtc(sourcePath),
                cancellationToken => CancellableSaveStore.ReadBytesAsync(
                    _local,
                    sourcePath,
                    cancellationToken
                )
            );

        private void AddLocalSettingsExportOnly(string root)
        {
            var path = $"{root}/settings.save";
            if (!_local.FileExists(path))
                return;
            _sourceFilesExamined++;
            try
            {
                _exportOnly.Add(new LegacyRecoveryExportOnlyFile(
                    path,
                    _local.GetFileSize(path),
                    LocalModifiedUtc(path),
                    "Device settings are export-only and are never a restorable save snapshot"
                ));
            }
            catch
            {
                _exportOnly.Add(new LegacyRecoveryExportOnlyFile(
                    path,
                    0,
                    LocalModifiedUtc(path),
                    "Device settings are export-only and are never a restorable save snapshot"
                ));
            }
        }

        private async Task<byte[]> ReadStableLocalBytesAsync(
            string path,
            CancellationToken cancellationToken
        )
            => await ReadSourceTwiceAsync(
                LocalInput(path, path),
                cancellationToken
            ).ConfigureAwait(false);

        private DateTimeOffset LocalModifiedUtc(string path)
        {
            try
            {
                return _local.FileExists(path)
                    ? _local.GetLastModifiedTime(path).ToUniversalTime()
                    : DateTimeOffset.UnixEpoch;
            }
            catch
            {
                return DateTimeOffset.UnixEpoch;
            }
        }

        private IReadOnlyList<string> LocalFiles(
            string directory,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!_local.DirectoryExists(directory))
                    return Array.Empty<string>();
                return _local.GetFilesInDirectory(directory)
                    .Where(IsSafeChildName)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .Take(LegacyRecoveryFilesPerDirectoryLimit)
                    .ToArray();
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private IReadOnlyList<string> LocalDirectories(
            string directory,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!_local.DirectoryExists(directory))
                    return Array.Empty<string>();
                return _local.GetDirectoriesInDirectory(directory)
                    .Where(IsSafeChildName)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .Take(LegacyRecoveryDirectoryLimit)
                    .ToArray();
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static bool TryMapSidecarFilename(
            string filename,
            out string targetName
        )
        {
            targetName = string.Empty;
            if (filename.EndsWith(".backup", StringComparison.OrdinalIgnoreCase))
            {
                targetName = filename[..^".backup".Length];
                return targetName.Length != 0;
            }
            if (!filename.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                return false;

            var stem = filename[..^".tmp".Length];
            const string cloudToken = ".sts2-cloud-";
            var token = stem.LastIndexOf(
                cloudToken,
                StringComparison.OrdinalIgnoreCase
            );
            if (token >= 0)
            {
                var suffix = stem[(token + cloudToken.Length)..];
                if (suffix.Length != 32 || !suffix.All(Uri.IsHexDigit))
                    return false;
                stem = stem[..token];
            }
            targetName = stem;
            return targetName.Length != 0;
        }

        private static bool IsManualPullGeneration(string value)
            => DateTimeOffset.TryParseExact(
                value,
                "yyyyMMdd'T'HHmmssfff'Z'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out _
            );

        private static bool IsSafeChildName(string value)
            => !string.IsNullOrWhiteSpace(value)
                && value is not ("." or "..")
                && !value.Contains('/')
                && !value.Contains('\\')
                && !value.Contains(':');
    }
}
