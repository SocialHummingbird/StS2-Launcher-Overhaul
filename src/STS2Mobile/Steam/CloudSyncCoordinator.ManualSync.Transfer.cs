#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const string IncompletePullMarkerContent =
        "{\"version\":1,\"state\":\"pull-incomplete\"}";

    private readonly record struct TransferSnapshotItem(
        string Path,
        byte[] Content,
        string Sha256,
        bool IsTombstone
    )
    {
        internal static TransferSnapshotItem File(string path, byte[] content)
            => new(
                path,
                content.ToArray(),
                HashContent(content),
                false
            );

        internal static TransferSnapshotItem Tombstone(string path)
            => new(path, Array.Empty<byte>(), "", true);
    }

    private readonly record struct FileBaseline(
        bool Exists,
        string Sha256
    )
    {
        internal static FileBaseline Missing => new(false, "");

        internal static FileBaseline FromContent(string content)
            => new(true, HashContent(content));

        internal static FileBaseline FromContent(byte[] content)
            => new(true, HashContent(content));
    }

    private sealed record CapturedTransferSnapshot(
        IReadOnlyList<TransferSnapshotItem> Items,
        IReadOnlyDictionary<string, FileBaseline> SourceBaseline
    );

    private static async Task<ManualCloudSyncResult> RunTransferAsync(
        ManualSyncContext sync,
        CloudOperationKind direction,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        AutomaticSaveManifest? expectedSourceManifest = null,
        bool allowUnmarkedPush = true,
        bool allowEmptySource = false
    )
    {
        var steamId64 = await sync.AuthenticateAsync().ConfigureAwait(false);
        var context = SaveContext.Create(
            steamId64,
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint
        );
        RequireApprovedRecoveryTransferContext(sync.LocalStore, context);
        var markerContent = context.SerializeMarker();

        var markerExists = await sync.RemoteFileExistsAsync(
            context.MarkerPath
        ).ConfigureAwait(false);
        var trustedMarkerBaseline = FileBaseline.Missing;
        if (markerExists)
        {
            var existingMarker = await sync.ReadRemoteFileForVerificationAsync(
                context.MarkerPath
            ).ConfigureAwait(false);
            trustedMarkerBaseline = FileBaseline.FromContent(existingMarker);
            try
            {
                SaveContext.ParseMarker(existingMarker).RequireExactMatch(context);
            }
            catch (InvalidDataException ex)
                when (direction == CloudOperationKind.Push
                    && allowUnmarkedPush)
            {
                PatchHelper.Log(
                    "[Cloud] Push will replace an unreadable save-context "
                        + $"marker after backing it up: {ex.Message}"
                );
            }
        }
        else if (
            direction == CloudOperationKind.Push
            && !allowUnmarkedPush
        )
        {
            throw new InvalidOperationException(
                "Steam Cloud saves have no exact launcher save context. "
                    + "Automatic Push cannot adopt them without an explicit Local source choice."
            );
        }
        else if (direction == CloudOperationKind.Pull)
        {
            throw new InvalidOperationException(
                "Steam Cloud saves have no trusted launcher context. "
                    + "Pull cannot infer their account, branch, or mod set."
            );
        }

        var incompletePullPath = IncompletePullMarkerPath(context.Namespace);
        if (direction == CloudOperationKind.Push)
            await RequireNoIncompletePullAsync(sync).ConfigureAwait(false);

        sync.ReportEnumerationStarted(
            $"Snapshotting {context.NamespaceName} save files"
        );
        var captured = await CaptureSourceSnapshotAsync(
            sync,
            direction,
            context
        ).ConfigureAwait(false);
        var snapshot = captured.Items;
        if (
            expectedSourceManifest is not null
            && !ManifestFromTransferCapture(captured)
                .ContentEquals(expectedSourceManifest)
        )
        {
            throw new IOException(
                "Save source changed after the automatic transfer intent was persisted"
            );
        }
        if (!allowEmptySource && !snapshot.Any(item =>
                !item.IsTombstone
                && !string.Equals(
                    item.Path,
                    SaveTransferAllowlist.SharedProfilePath,
                    StringComparison.OrdinalIgnoreCase
                )
            ))
        {
            throw new InvalidOperationException(
                $"No transferable {context.NamespaceName} profile save files were found"
            );
        }

        sync.ReportEnumerationCompleted(snapshot.Count);
        var backupRoot = await CreateBackupSessionAsync(
            sync,
            direction,
            context
        ).ConfigureAwait(false);
        var destinationBaselines = await BackupDestinationsAsync(
            sync,
            direction,
            snapshot,
            direction == CloudOperationKind.Push
                ? context.MarkerPath
                : null,
            backupRoot
        ).ConfigureAwait(false);

        if (direction == CloudOperationKind.Push)
        {
            RequireSameBaseline(
                context.MarkerPath,
                trustedMarkerBaseline,
                destinationBaselines[context.MarkerPath],
                "Steam save context"
            );
        }

        if (
            direction == CloudOperationKind.Pull
            && !await sync.LocalFileExistsAsync(incompletePullPath)
                .ConfigureAwait(false)
        )
        {
            await WriteAndVerifyLocalAsync(
                sync,
                incompletePullPath,
                IncompletePullMarkerContent
            ).ConfigureAwait(false);
        }

        if (direction == CloudOperationKind.Push)
        {
            var markerBaseline = destinationBaselines[context.MarkerPath];
            await RequireRemoteBaselineAsync(
                sync,
                context.MarkerPath,
                markerBaseline,
                "Steam save context"
            ).ConfigureAwait(false);
            if (markerBaseline.Exists)
            {
                await sync.DeleteRemoteFileAsync(context.MarkerPath)
                    .ConfigureAwait(false);
            }

            if (await sync.RemoteFileExistsAsync(context.MarkerPath)
                    .ConfigureAwait(false))
            {
                throw new IOException(
                    "Steam Cloud save-context marker could not be invalidated "
                        + "before Push"
                );
            }
        }
        sync.ReportTransferStarted(snapshot.Count);
        foreach (var item in snapshot)
        {
            sync.Checkpoint();
            sync.ReportTransferPathStarted(item.Path);
            var destinationBaseline = destinationBaselines[item.Path];
            await RequireDestinationBaselineAsync(
                sync,
                direction,
                item.Path,
                destinationBaseline,
                "Destination"
            ).ConfigureAwait(false);
            if (item.IsTombstone)
            {
                if (destinationBaseline.Exists)
                {
                    await sync.DeleteDestinationFileAsync(
                        direction,
                        item.Path
                    ).ConfigureAwait(false);
                }

                if (await sync.DestinationFileExistsAsync(
                        direction,
                        item.Path
                    ).ConfigureAwait(false))
                {
                    throw new IOException(
                        $"Destination deletion could not be verified: {item.Path}"
                    );
                }
            }
            else
            {
                await sync.WriteDestinationFileBytesAsync(
                    direction,
                    item.Path,
                    item.Content
                ).ConfigureAwait(false);
                var verified = await sync.ReadDestinationFileBytesAsync(
                    direction,
                    item.Path
                ).ConfigureAwait(false);
                RequireHash(
                    item.Path,
                    item.Sha256,
                    verified,
                    direction == CloudOperationKind.Push
                        ? "Steam Cloud"
                        : "Android local storage"
                );
            }

            sync.ReportTransferProcessed(item.Path);
        }

        if (direction == CloudOperationKind.Push)
        {
            sync.ReportFinalizing(
                "Verifying the exact Steam save mirror"
            );
            sync.PrepareCloudMetadata();
            await VerifyDestinationSnapshotAsync(
                sync,
                direction,
                context,
                snapshot
            ).ConfigureAwait(false);
            await RequireRemoteMissingAsync(
                sync,
                context.MarkerPath,
                "Steam save context"
            ).ConfigureAwait(false);
            try
            {
                await WriteAndVerifyRemoteAsync(
                    sync,
                    context.MarkerPath,
                    markerContent
                ).ConfigureAwait(false);
            }
            catch
            {
                await BestEffortInvalidateMarkerAsync(
                    sync,
                    context.MarkerPath,
                    HashContent(markerContent)
                ).ConfigureAwait(false);
                throw;
            }
        }
        else
        {
            sync.ReportFinalizing(
                "Verifying the exact local and Steam source snapshots"
            );
            await VerifyDestinationSnapshotAsync(
                sync,
                direction,
                context,
                snapshot
            ).ConfigureAwait(false);
            sync.PrepareCloudMetadata();
            await VerifySourceSnapshotAsync(
                sync,
                direction,
                context,
                captured.SourceBaseline
            ).ConfigureAwait(false);
            await RequireRemoteBaselineAsync(
                sync,
                context.MarkerPath,
                trustedMarkerBaseline,
                "Steam save context"
            ).ConfigureAwait(false);
            await sync.DeleteLocalFileAsync(incompletePullPath)
                .ConfigureAwait(false);
            if (await sync.LocalFileExistsAsync(incompletePullPath)
                    .ConfigureAwait(false))
            {
                throw new IOException(
                    "The incomplete Pull marker could not be cleared"
                );
            }
        }

        var progress = sync.ProgressState;
        return new ManualCloudSyncResult(
            direction,
            snapshot.Count,
            progress.BackupCreatedCount,
            $"Verified {snapshot.Count} {context.NamespaceName} save item(s) "
                + "for the authenticated Steam account, runtime "
                + $"{context.RuntimeIdentity}."
        );
    }

    private static async Task RequireNoIncompletePullAsync(
        ManualSyncContext sync
    )
    {
        foreach (
            var saveNamespace in new[]
            {
                SaveNamespace.Vanilla,
                SaveNamespace.Modded,
            }
        )
        {
            if (await sync.LocalFileExistsAsync(
                    IncompletePullMarkerPath(saveNamespace)
                ).ConfigureAwait(false))
            {
                var namespaceName = saveNamespace == SaveNamespace.Modded
                    ? "modded"
                    : "vanilla";
                throw new InvalidOperationException(
                    $"A previous {namespaceName} Pull did not complete. "
                        + "Retry and complete that Pull before Push."
                );
            }
        }
    }

    private static string IncompletePullMarkerPath(
        SaveNamespace saveNamespace
    )
        => saveNamespace == SaveNamespace.Modded
            ? ".sts2-launcher/pull-incomplete/modded.json"
            : ".sts2-launcher/pull-incomplete/vanilla.json";

    private static async Task<CapturedTransferSnapshot>
        CaptureSourceSnapshotAsync(
            ManualSyncContext sync,
            CloudOperationKind direction,
            SaveContext context
        )
    {
        sync.PrepareCloudMetadata();

        var items = new List<TransferSnapshotItem>();
        var sourceBaseline = new Dictionary<string, FileBaseline>(
            StringComparer.OrdinalIgnoreCase
        );
        await AddIfPresentAsync(
            sync,
            direction,
            SaveTransferAllowlist.SharedProfilePath,
            items,
            sourceBaseline
        ).ConfigureAwait(false);

        for (
            var profileId = 1;
            profileId <= SaveTransferAllowlist.ProfileCount;
            profileId++
        )
        {
            var saveDirectory = SaveTransferAllowlist.SaveDirectory(
                context.Namespace,
                profileId
            );
            foreach (var filename in SaveTransferAllowlist.FixedProfileFiles)
            {
                var path = $"{saveDirectory}/{filename}";
                if (!await AddIfPresentAsync(
                        sync,
                        direction,
                        path,
                        items,
                        sourceBaseline
                    ).ConfigureAwait(false))
                {
                    AddAuthoritativeSnapshotItem(
                        items,
                        TransferSnapshotItem.Tombstone(path)
                    );
                }
            }

            var historyDirectory = SaveTransferAllowlist.HistoryDirectory(
                context.Namespace,
                profileId
            );
            var sourceHistoryFiles = sync.GetSourceHistoryFiles(
                    direction,
                    historyDirectory
                )
                .Where(SaveTransferAllowlist.IsAllowedHistoryFilename)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(path => path, StringComparer.Ordinal)
                .Take(SaveTransferAllowlist.RunHistoryLimit)
                .ToArray();
            var sourceHistoryPaths = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );
            foreach (var historyFile in sourceHistoryFiles)
            {
                var path = $"{historyDirectory}/{historyFile}";
                await AddRequiredFileAsync(
                    sync,
                    direction,
                    path,
                    items,
                    sourceBaseline
                ).ConfigureAwait(false);
                sourceHistoryPaths.Add(path);
            }

            var destinationHistoryEntries = sync.GetDestinationHistoryFiles(
                    direction,
                    historyDirectory
                )
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var destinationHistoryFiles = destinationHistoryEntries
                .Where(SaveTransferAllowlist.IsAllowedHistoryFilename)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            foreach (var historyFile in destinationHistoryFiles)
            {
                var path = $"{historyDirectory}/{historyFile}";
                if (sourceHistoryPaths.Contains(path))
                    continue;

                AddAuthoritativeSnapshotItem(
                    items,
                    TransferSnapshotItem.Tombstone(path)
                );
            }

            foreach (
                var historyBackupFile in destinationHistoryEntries
                    .Where(
                        SaveTransferAllowlist.IsAllowedHistoryBackupFilename
                    )
                    .OrderBy(path => path, StringComparer.Ordinal)
            )
            {
                AddSnapshotItem(
                    items,
                    TransferSnapshotItem.Tombstone(
                        $"{historyDirectory}/{historyBackupFile}"
                    )
                );
            }
        }

        if (items.Any(item =>
                !SaveTransferAllowlist.IsAllowedPath(
                    context.Namespace,
                    item.Path
                )
                && (!item.IsTombstone
                    || !SaveTransferAllowlist.IsAllowedBackupCleanupPath(
                        context.Namespace,
                        item.Path
                    ))
            ))
        {
            throw new InvalidOperationException(
                "Save snapshot contained a path outside its explicit allowlist"
            );
        }

        return new CapturedTransferSnapshot(
            items.ToArray(),
            sourceBaseline
        );
    }

    private static async Task<bool> AddIfPresentAsync(
        ManualSyncContext sync,
        CloudOperationKind direction,
        string path,
        ICollection<TransferSnapshotItem> items,
        IDictionary<string, FileBaseline> sourceBaseline
    )
    {
        if (!await sync.SourceFileExistsAsync(direction, path)
                .ConfigureAwait(false))
        {
            sourceBaseline[path] = FileBaseline.Missing;
            return false;
        }

        await AddRequiredFileAsync(
            sync,
            direction,
            path,
            items,
            sourceBaseline
        )
            .ConfigureAwait(false);
        return true;
    }

    private static async Task AddRequiredFileAsync(
        ManualSyncContext sync,
        CloudOperationKind direction,
        string path,
        ICollection<TransferSnapshotItem> items,
        IDictionary<string, FileBaseline> sourceBaseline
    )
    {
        var content = await sync.ReadSourceFileBytesAsync(direction, path)
            .ConfigureAwait(false);
        if (content.Length == 0
            || string.IsNullOrWhiteSpace(
                DecodeAutomaticSnapshotBytes(content, path)
            ))
        {
            throw new InvalidDataException(
                $"Source save file is empty: {path}"
            );
        }

        var item = TransferSnapshotItem.File(path, content);
        sourceBaseline[path] = new FileBaseline(true, item.Sha256);
        AddAuthoritativeSnapshotItem(items, item);
    }

    private static void AddAuthoritativeSnapshotItem(
        ICollection<TransferSnapshotItem> items,
        TransferSnapshotItem item
    )
    {
        AddSnapshotItem(items, item);
        AddSnapshotItem(
            items,
            TransferSnapshotItem.Tombstone($"{item.Path}.backup")
        );
    }

    private static void AddSnapshotItem(
        ICollection<TransferSnapshotItem> items,
        TransferSnapshotItem item
    )
    {
        if (items.Any(existing => string.Equals(
                existing.Path,
                item.Path,
                StringComparison.OrdinalIgnoreCase
            )))
        {
            return;
        }

        items.Add(item);
    }

    private static async Task<string> CreateBackupSessionAsync(
        ManualSyncContext sync,
        CloudOperationKind direction,
        SaveContext context
    )
    {
        var operationId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ")
            + "-"
            + Guid.NewGuid().ToString("N");
        var root = $".sts2-launcher/transfer-backups/{operationId}";
        await WriteAndVerifyLocalAsync(
            sync,
            $"{root}/context.json",
            context.SerializeMarker()
        ).ConfigureAwait(false);
        await WriteAndVerifyLocalAsync(
            sync,
            $"{root}/direction.txt",
            direction == CloudOperationKind.Push ? "push" : "pull"
        ).ConfigureAwait(false);
        return root;
    }

    private static async Task<IReadOnlyDictionary<string, FileBaseline>>
        BackupDestinationsAsync(
        ManualSyncContext sync,
        CloudOperationKind direction,
        IReadOnlyList<TransferSnapshotItem> snapshot,
        string? markerPath,
        string backupRoot
    )
    {
        var paths = snapshot.Select(item => item.Path).ToList();
        if (markerPath is not null)
            paths.Add(markerPath);
        paths = paths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        sync.ReportBackupStarted(paths.Count);
        var baselines = new Dictionary<string, FileBaseline>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var path in paths)
        {
            sync.Checkpoint();
            sync.ReportBackupPathStarted(path);
            var exists = await sync.DestinationFileExistsAsync(
                direction,
                path
            ).ConfigureAwait(false);
            var baseline = FileBaseline.Missing;
            if (exists)
            {
                if (markerPath is not null && string.Equals(
                        path,
                        markerPath,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    var markerContent = await sync
                        .ReadRemoteFileForVerificationAsync(path)
                        .ConfigureAwait(false);
                    await WriteAndVerifyLocalAsync(
                        sync,
                        $"{backupRoot}/files/{CloudSavePath.Relative(path)}",
                        markerContent
                    ).ConfigureAwait(false);
                    baseline = FileBaseline.FromContent(markerContent);
                    baselines[path] = baseline;
                    sync.ReportBackupProcessed(path, exists);
                    continue;
                }

                var content = await sync.ReadDestinationFileBytesAsync(
                    direction,
                    path
                ).ConfigureAwait(false);
                await WriteAndVerifyLocalBytesAsync(
                    sync,
                    $"{backupRoot}/files/{CloudSavePath.Relative(path)}",
                    content
                ).ConfigureAwait(false);
                baseline = FileBaseline.FromContent(content);
            }

            baselines[path] = baseline;
            sync.ReportBackupProcessed(path, exists);
        }

        return baselines;
    }

    private static async Task RequireDestinationBaselineAsync(
        ManualSyncContext sync,
        CloudOperationKind direction,
        string path,
        FileBaseline expected,
        string subject
    )
    {
        var actual = await ReadDestinationBaselineAsync(
            sync,
            direction,
            path
        ).ConfigureAwait(false);
        RequireSameBaseline(path, expected, actual, subject);
    }

    private static async Task<FileBaseline> ReadDestinationBaselineAsync(
        ManualSyncContext sync,
        CloudOperationKind direction,
        string path
    )
    {
        if (!await sync.DestinationFileExistsAsync(direction, path)
                .ConfigureAwait(false))
        {
            return FileBaseline.Missing;
        }

        var content = await sync.ReadDestinationFileBytesAsync(direction, path)
            .ConfigureAwait(false);
        return FileBaseline.FromContent(content);
    }

    private static async Task RequireRemoteBaselineAsync(
        ManualSyncContext sync,
        string path,
        FileBaseline expected,
        string subject
    )
    {
        var actual = await ReadRemoteBaselineAsync(sync, path)
            .ConfigureAwait(false);
        RequireSameBaseline(path, expected, actual, subject);
    }

    private static Task RequireRemoteMissingAsync(
        ManualSyncContext sync,
        string path,
        string subject
    )
        => RequireRemoteBaselineAsync(
            sync,
            path,
            FileBaseline.Missing,
            subject
        );

    private static async Task<FileBaseline> ReadRemoteBaselineAsync(
        ManualSyncContext sync,
        string path
    )
    {
        if (!await sync.RemoteFileExistsAsync(path).ConfigureAwait(false))
            return FileBaseline.Missing;

        var content = await sync.ReadRemoteFileForVerificationAsync(path)
            .ConfigureAwait(false);
        return FileBaseline.FromContent(content);
    }

    private static async Task<FileBaseline> ReadSourceBaselineAsync(
        ManualSyncContext sync,
        CloudOperationKind direction,
        string path
    )
    {
        if (!await sync.SourceFileExistsAsync(direction, path)
                .ConfigureAwait(false))
        {
            return FileBaseline.Missing;
        }

        var content = await sync.ReadSourceFileBytesAsync(direction, path)
            .ConfigureAwait(false);
        return FileBaseline.FromContent(content);
    }

    private static void RequireSameBaseline(
        string path,
        FileBaseline expected,
        FileBaseline actual,
        string subject
    )
    {
        if (expected.Equals(actual))
            return;

        throw new IOException(
            $"{subject} drift detected during save transfer: {path}"
        );
    }

    private static async Task VerifyDestinationSnapshotAsync(
        ManualSyncContext sync,
        CloudOperationKind direction,
        SaveContext context,
        IReadOnlyList<TransferSnapshotItem> snapshot
    )
    {
        foreach (var item in snapshot)
        {
            var expected = item.IsTombstone
                ? FileBaseline.Missing
                : new FileBaseline(true, item.Sha256);
            var actual = await ReadDestinationBaselineAsync(
                sync,
                direction,
                item.Path
            ).ConfigureAwait(false);
            RequireSameBaseline(
                item.Path,
                expected,
                actual,
                "Final destination"
            );
        }

        for (
            var profileId = 1;
            profileId <= SaveTransferAllowlist.ProfileCount;
            profileId++
        )
        {
            var directory = SaveTransferAllowlist.HistoryDirectory(
                context.Namespace,
                profileId
            );
            var prefix = directory + "/";
            var expected = snapshot
                .Where(item => !item.IsTombstone)
                .Select(item => item.Path)
                .Where(path => path.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase
                ))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var actual = sync.GetDestinationHistoryFiles(
                    direction,
                    directory
                )
                .Where(filename =>
                    SaveTransferAllowlist.IsAllowedHistoryFilename(filename)
                    || SaveTransferAllowlist
                        .IsAllowedHistoryBackupFilename(filename)
                )
                .Select(filename => $"{directory}/{filename}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!actual.SetEquals(expected))
            {
                throw new IOException(
                    "Final destination history drift detected during save "
                        + $"transfer: {directory}"
                );
            }
        }
    }

    private static async Task VerifySourceSnapshotAsync(
        ManualSyncContext sync,
        CloudOperationKind direction,
        SaveContext context,
        IReadOnlyDictionary<string, FileBaseline> sourceBaseline
    )
    {
        foreach (var (path, expected) in sourceBaseline)
        {
            var actual = await ReadSourceBaselineAsync(
                sync,
                direction,
                path
            ).ConfigureAwait(false);
            RequireSameBaseline(path, expected, actual, "Steam source");
        }

        for (
            var profileId = 1;
            profileId <= SaveTransferAllowlist.ProfileCount;
            profileId++
        )
        {
            var directory = SaveTransferAllowlist.HistoryDirectory(
                context.Namespace,
                profileId
            );
            var prefix = directory + "/";
            var expected = sourceBaseline
                .Where(entry => entry.Value.Exists)
                .Select(entry => entry.Key)
                .Where(path => path.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase
                ))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var actual = sync.GetSourceHistoryFiles(direction, directory)
                .Where(SaveTransferAllowlist.IsAllowedHistoryFilename)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(path => path, StringComparer.Ordinal)
                .Take(SaveTransferAllowlist.RunHistoryLimit)
                .Select(filename => $"{directory}/{filename}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!actual.SetEquals(expected))
            {
                throw new IOException(
                    "Steam source history drift detected during save "
                        + $"transfer: {directory}"
                );
            }
        }
    }

    private static async Task BestEffortInvalidateMarkerAsync(
        ManualSyncContext sync,
        string markerPath,
        string expectedHash
    )
    {
        try
        {
            if (!await sync.RemoteFileExistsAsync(markerPath)
                    .ConfigureAwait(false))
            {
                return;
            }

            var content = await sync.ReadRemoteFileForVerificationAsync(
                markerPath
            ).ConfigureAwait(false);
            if (!string.Equals(
                    HashContent(content),
                    expectedHash,
                    StringComparison.Ordinal
                ))
            {
                PatchHelper.Log(
                    "[Cloud] Failed Push context marker changed before "
                        + "cleanup and was left untouched"
                );
                return;
            }

            await sync.DeleteRemoteFileAsync(markerPath).ConfigureAwait(false);
            if (await sync.RemoteFileExistsAsync(markerPath)
                    .ConfigureAwait(false))
            {
                PatchHelper.Log(
                    "[Cloud] Failed Push context marker remained after "
                        + "best-effort invalidation"
                );
            }
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                "[Cloud] Could not invalidate failed Push context marker: "
                    + ex.Message
            );
        }
    }

    private static async Task WriteAndVerifyLocalAsync(
        ManualSyncContext sync,
        string path,
        string content
    )
    {
        await sync.WriteLocalFileAsync(path, content).ConfigureAwait(false);
        var verified = await sync.ReadLocalFileAsync(path).ConfigureAwait(false);
        RequireHash(path, HashContent(content), verified, "local storage");
    }

    private static async Task WriteAndVerifyLocalBytesAsync(
        ManualSyncContext sync,
        string path,
        byte[] content
    )
    {
        await sync.WriteLocalFileBytesAsync(path, content).ConfigureAwait(false);
        var verified = await sync.ReadLocalFileBytesAsync(path).ConfigureAwait(false);
        RequireHash(path, HashContent(content), verified, "local storage");
    }

    private static async Task WriteAndVerifyRemoteAsync(
        ManualSyncContext sync,
        string path,
        string content
    )
    {
        await sync.WriteRemoteFileAsync(path, content).ConfigureAwait(false);
        var verified = await sync.ReadRemoteFileForVerificationAsync(path)
            .ConfigureAwait(false);
        RequireHash(path, HashContent(content), verified, "Steam Cloud");
    }

    private static void RequireHash(
        string path,
        string expectedHash,
        string actualContent,
        string destination
    )
    {
        var actualHash = HashContent(actualContent);
        if (!string.Equals(expectedHash, actualHash, StringComparison.Ordinal))
        {
            var message = $"{destination} read-back hash mismatch for {path}: "
                + $"expected={expectedHash}; actual={actualHash}";
            if (string.Equals(destination, "Steam Cloud", StringComparison.Ordinal))
                throw new SaveTransferReadBackMismatchException(message, remote: true);
            throw new InvalidDataException(message);
        }
    }

    private static void RequireHash(
        string path,
        string expectedHash,
        byte[] actualContent,
        string destination
    )
    {
        var actualHash = HashContent(actualContent);
        if (!string.Equals(expectedHash, actualHash, StringComparison.Ordinal))
        {
            var message = $"{destination} read-back hash mismatch for {path}: "
                + $"expected={expectedHash}; actual={actualHash}";
            if (string.Equals(destination, "Steam Cloud", StringComparison.Ordinal))
                throw new SaveTransferReadBackMismatchException(message, remote: true);
            throw new InvalidDataException(message);
        }
    }

    private static string HashContent(string content)
        => AutomaticSyncHash.Compute(content);

    private static string HashContent(byte[] content)
        => AutomaticSyncHash.Compute(content);
}
