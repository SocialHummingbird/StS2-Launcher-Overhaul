#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task<AutomaticSaveSnapshot>
        CaptureAutomaticSnapshotAsync(
            ManualSyncContext sync,
            CloudOperationKind source,
            SaveContext context
        )
    {
        // Steam metadata is intentionally refreshed for every manifest capture.
        sync.PrepareCloudMetadata();
        return await CaptureAutomaticSnapshotCoreAsync(
            context,
            source == CloudOperationKind.Push ? "local" : "steam",
            "automatic-sync",
            path => sync.SourceFileExistsAsync(source, path),
            path => sync.ReadSourceFileBytesAsync(source, path),
            directory => sync.GetSourceHistoryFiles(source, directory)
        ).ConfigureAwait(false);
    }

    internal static async Task<RetainedAutomaticSaveSnapshot>
        CaptureRetainedLocalSnapshotAsync(
            ISaveStore local,
            SaveContext context,
            string sourceLabel,
            CancellationToken cancellationToken
        )
    {
        ArgumentNullException.ThrowIfNull(local);
        var snapshot = await CaptureAutomaticSnapshotCoreAsync(
            context,
            "local",
            sourceLabel,
            path =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(local.FileExists(path));
            },
            path => CancellableSaveStore.ReadBytesAsync(
                local,
                path,
                cancellationToken
            ),
            directory => GetLocalSnapshotHistoryFiles(
                local,
                directory,
                cancellationToken
            )
        ).ConfigureAwait(false);
        var retained = await WriteRetainedSnapshotAsync(
            local,
            context,
            snapshot,
            cancellationToken
        ).ConfigureAwait(false);
        await PruneRetainedSnapshotsAsync(
            local,
            context,
            new[] { retained.Path },
            cancellationToken
        ).ConfigureAwait(false);
        return retained;
    }

    private static async Task<AutomaticSaveSnapshot>
        CaptureAutomaticSnapshotCoreAsync(
            SaveContext context,
            string sourceKind,
            string sourceLabel,
            Func<string, Task<bool>> fileExists,
            Func<string, Task<byte[]>> readBytes,
            Func<string, IReadOnlyList<string>> getHistoryFiles
        )
    {
        var entries = new List<AutomaticSaveManifestEntry>();
        var files = new List<AutomaticSaveContentEntry>();

        await CaptureAutomaticPathAsync(
            SaveTransferAllowlist.SharedProfilePath,
            entries,
            files,
            fileExists,
            readBytes
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
                await CaptureAutomaticPathAsync(
                    $"{saveDirectory}/{filename}",
                    entries,
                    files,
                    fileExists,
                    readBytes
                ).ConfigureAwait(false);
            }

            var historyDirectory = SaveTransferAllowlist.HistoryDirectory(
                context.Namespace,
                profileId
            );
            var histories = getHistoryFiles(historyDirectory)
                .Where(SaveTransferAllowlist.IsAllowedHistoryFilename)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(path => path, StringComparer.Ordinal)
                .Take(SaveTransferAllowlist.RunHistoryLimit)
                .ToArray();
            foreach (var filename in histories)
            {
                await CaptureAutomaticPathAsync(
                    $"{historyDirectory}/{filename}",
                    entries,
                    files,
                    fileExists,
                    readBytes,
                    required: true
                ).ConfigureAwait(false);
            }
        }

        return new AutomaticSaveSnapshot
        {
            ContextMarker = context.SerializeMarker(),
            Coverage = "full",
            SourceKind = sourceKind,
            SourceLabel = sourceLabel,
            CapturedUtc = DateTimeOffset.UtcNow,
            Manifest = AutomaticSaveManifest.Create(entries),
            Files = files
                .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(file => file.Path, StringComparer.Ordinal)
                .ToList(),
        };
    }

    private static async Task CaptureAutomaticPathAsync(
        string path,
        ICollection<AutomaticSaveManifestEntry> entries,
        ICollection<AutomaticSaveContentEntry> files,
        Func<string, Task<bool>> fileExists,
        Func<string, Task<byte[]>> readBytes,
        bool required = false
    )
    {
        var canonical = CloudSavePath.Relative(path);
        if (!await fileExists(canonical).ConfigureAwait(false))
        {
            if (required)
            {
                throw new IOException(
                    $"Save file disappeared while its manifest was captured: {canonical}"
                );
            }

            entries.Add(MissingEntry(canonical));
            return;
        }

        var bytes = await readBytes(canonical).ConfigureAwait(false);
        var content = DecodeAutomaticSnapshotBytes(bytes, canonical);
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidDataException($"Save file is empty: {canonical}");

        entries.Add(new AutomaticSaveManifestEntry
        {
            Path = canonical,
            Exists = true,
            Sha256 = AutomaticSyncHash.Compute(content),
            ByteSha256 = AutomaticSyncHash.Compute(bytes),
        });
        files.Add(new AutomaticSaveContentEntry
        {
            Path = canonical,
            ContentBase64 = Convert.ToBase64String(bytes),
            ByteSha256 = AutomaticSyncHash.Compute(bytes),
        });
    }

    private static IReadOnlyList<string> GetLocalSnapshotHistoryFiles(
        ISaveStore local,
        string directory,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!local.DirectoryExists(directory))
            return Array.Empty<string>();
        var files = local.GetFilesInDirectory(directory);
        cancellationToken.ThrowIfCancellationRequested();
        return files;
    }

    private static AutomaticSaveManifest ProjectTransferDestination(
        AutomaticSaveManifest source,
        AutomaticSaveManifest destination
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        var projected = new List<AutomaticSaveManifestEntry>();
        var shared = source.Entry(SaveTransferAllowlist.SharedProfilePath);
        projected.Add(
            shared.Exists
                ? shared
                : destination.Entry(SaveTransferAllowlist.SharedProfilePath)
        );
        projected.AddRange(source.Entries.Where(entry => !string.Equals(
            entry.Path,
            SaveTransferAllowlist.SharedProfilePath,
            StringComparison.OrdinalIgnoreCase
        )));
        return AutomaticSaveManifest.Create(projected);
    }

    private static AutomaticSaveManifest ManifestFromTransferCapture(
        CapturedTransferSnapshot captured
    )
        => AutomaticSaveManifest.Create(
            captured.SourceBaseline.Select(entry =>
                new AutomaticSaveManifestEntry
                {
                    Path = entry.Key,
                    Exists = entry.Value.Exists,
                    Sha256 = entry.Value.Exists ? entry.Value.Sha256 : "",
                    ByteSha256 = entry.Value.Exists ? entry.Value.Sha256 : "",
                }
            )
        );

}
