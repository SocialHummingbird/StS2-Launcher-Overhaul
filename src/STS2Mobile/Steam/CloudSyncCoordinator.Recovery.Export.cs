#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const int SaveRecoveryExportVersion = 2;
    private const long SaveRecoveryExportMaximumSnapshotBytes =
        96L * 1024 * 1024;

    private sealed class SaveRecoveryExportBundle
    {
        public int Version { get; set; } = SaveRecoveryExportVersion;
        public string ExportId { get; set; } = "";
        public string CurrentAndroidTreeSha256 { get; set; } = "";
        public string SelectedSaveContextSha256 { get; set; } = "";
        public DateTimeOffset CreatedUtc { get; set; }
        public string Warning { get; set; } = "";
        public string SelectedCandidateId { get; set; } = "";
        public bool OriginalSourcesWereModified { get; set; }
        public bool SteamWasContacted { get; set; }
        public bool CloudSyncEnabled { get; set; }
        public SaveRecoveryExportSelectedSaveContext SelectedSaveContext {
            get;
            set;
        } = new();
        public AutomaticSaveSnapshot CurrentAndroidSnapshot { get; set; } = new();
        public List<SaveRecoveryExportSnapshot> RecoverySnapshots { get; set; } = new();
        public List<LegacyRecoveryExportOnlyFile> ExportOnlyFiles { get; set; } = new();
        public string RecoveryHoldJson { get; set; } = "";
        public string RecoveryJournalJson { get; set; } = "";
        public string AutomaticSyncPendingJson { get; set; } = "";
        public string AutomaticSyncBaselineJson { get; set; } = "";
        public string AutomaticSyncBeforeGameSnapshotJson { get; set; } = "";
        public int OmittedSnapshotCount { get; set; }
    }

    private sealed class SaveRecoveryExportSelectedSaveContext
    {
        public string SaveNamespace { get; set; } = "";
        public string RuntimeIdentity { get; set; } = "";
        public string ModSetFingerprint { get; set; } = "";
        public ulong SteamId64 { get; set; }
    }

    private sealed class SaveRecoveryExportSnapshot
    {
        public string CandidateId { get; set; } = "";
        public LegacyRecoverySourceKind SourceKind { get; set; }
        public string SourceLabel { get; set; } = "";
        public IReadOnlyList<string> SourcePaths { get; set; } =
            Array.Empty<string>();
        public LegacyRecoveryProvenance? Provenance { get; set; }
        public LegacyRecoveryClassification Classification { get; set; }
        public string Coverage { get; set; } = "";
        public string SnapshotPath { get; set; } = "";
        public string SnapshotSha256 { get; set; } = "";
        public AutomaticSaveSnapshot Snapshot { get; set; } = new();
    }

    internal static async Task<string> BuildSaveRecoveryExportBundleAsync(
        ISaveStore local,
        LegacySaveRecoveryScanResult scan,
        string selectedCandidateId,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        bool cloudSyncEnabled,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(scan);
        var bundle = await BuildCurrentSaveExportBundleCoreAsync(
            local,
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            cloudSyncEnabled,
            includeExistingSnapshots: false,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
        bundle.SelectedCandidateId = selectedCandidateId ?? "";
        bundle.ExportOnlyFiles = scan.ExportOnlyFiles.ToList();

        long includedBytes = bundle.CurrentAndroidSnapshot.Files.Sum(file =>
            (long)ReadSnapshotFileBytes(
                bundle.CurrentAndroidSnapshot,
                file.Path
            ).Length
        );
        var ordered = scan.Candidates
            .Where(candidate => candidate.Imported)
            .OrderByDescending(candidate => string.Equals(
                candidate.SnapshotSha256,
                selectedCandidateId,
                StringComparison.OrdinalIgnoreCase
            ))
            .ThenByDescending(candidate => candidate.CapturedUtc)
            .ToArray();
        foreach (var candidate in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (includedBytes + candidate.TotalBytes
                > SaveRecoveryExportMaximumSnapshotBytes)
            {
                bundle.OmittedSnapshotCount++;
                continue;
            }

            AutomaticSaveSnapshot snapshot;
            try
            {
                snapshot = await ReadRecoveryOrRetainedSnapshotAsync(
                    local,
                    candidate.RecoverySnapshotPath,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (Exception ex) when (
                ex is InvalidDataException or IOException
            )
            {
                bundle.OmittedSnapshotCount++;
                continue;
            }

            includedBytes += candidate.TotalBytes;
            bundle.RecoverySnapshots.Add(new SaveRecoveryExportSnapshot
            {
                CandidateId = candidate.SnapshotSha256,
                SourceKind = candidate.SourceKind,
                SourceLabel = candidate.SourceLabel,
                SourcePaths = candidate.SourcePaths,
                Provenance = candidate.Provenance,
                Classification = candidate.Classification,
                Coverage = candidate.Coverage,
                SnapshotPath = candidate.RecoverySnapshotPath,
                SnapshotSha256 = candidate.SnapshotSha256,
                Snapshot = snapshot,
            });
        }

        if (!string.IsNullOrWhiteSpace(selectedCandidateId)
            && !bundle.RecoverySnapshots.Any(item => string.Equals(
                item.CandidateId,
                selectedCandidateId,
                StringComparison.OrdinalIgnoreCase
            )))
        {
            throw new InvalidOperationException(
                "The selected recovery snapshot is not verified or is too large to export safely."
            );
        }

        return JsonSerializer.Serialize(bundle, AutomaticSyncJsonOptions);
    }

    internal static async Task<string> BuildCurrentSaveExportBundleAsync(
        ISaveStore local,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        bool cloudSyncEnabled,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(local);
        var bundle = await BuildCurrentSaveExportBundleCoreAsync(
            local,
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            cloudSyncEnabled,
            includeExistingSnapshots: true,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
        return JsonSerializer.Serialize(bundle, AutomaticSyncJsonOptions);
    }

    private static async Task<SaveRecoveryExportBundle>
        BuildCurrentSaveExportBundleCoreAsync(
            ISaveStore local,
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string modSetFingerprint,
            bool cloudSyncEnabled,
            bool includeExistingSnapshots,
            CancellationToken cancellationToken
        )
    {
        var selection = NormalizeRecoverySelection(
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            steamId64: null
        );
        var pendingJson = ReadOptionalLocalText(
            local,
            AutomaticSyncPendingPath
        );
        var recoveryJournalJson = ReadOptionalLocalText(
            local,
            SaveRecoveryJournalPath
        );
        var exactContext = FindLocallyKnownSelectedContext(
            local,
            selection,
            pendingJson
        );
        var current = await CaptureRecoveryLocalSnapshotAsync(
            local,
            exactContext,
            selection,
            "support-export-current-android",
            cancellationToken
        ).ConfigureAwait(false);
        var selectedSaveContext = new SaveRecoveryExportSelectedSaveContext
        {
            SaveNamespace = RecoveryNamespaceName(selection.Namespace),
            RuntimeIdentity = selection.RuntimeIdentity,
            ModSetFingerprint = selection.ModSetFingerprint,
            SteamId64 = exactContext?.SteamId64 ?? 0,
        };
        var bundle = new SaveRecoveryExportBundle
        {
            ExportId = Guid.NewGuid().ToString("N"),
            CreatedUtc = DateTimeOffset.UtcNow,
            Warning =
                "Contains raw game save bytes and may contain Steam account identifiers. Share only with trusted support staff.",
            OriginalSourcesWereModified = false,
            SteamWasContacted = false,
            CloudSyncEnabled = cloudSyncEnabled,
            SelectedSaveContext = selectedSaveContext,
            CurrentAndroidSnapshot = current,
            RecoveryHoldJson = ReadOptionalLocalText(
                local,
                SaveRecoveryHoldPath
            ),
            RecoveryJournalJson = recoveryJournalJson,
            AutomaticSyncPendingJson = pendingJson,
        };
        bundle.CurrentAndroidTreeSha256 =
            ComputeSaveRecoveryExportTreeSha256(current, exactContext);
        bundle.SelectedSaveContextSha256 =
            ComputeSaveRecoveryExportContextSha256(selectedSaveContext);
        if (exactContext.HasValue)
        {
            var context = exactContext.Value;
            bundle.AutomaticSyncBaselineJson = ReadOptionalLocalText(
                local,
                AutomaticSyncBaselinePath(context)
            );
            bundle.AutomaticSyncBeforeGameSnapshotJson =
                ReadSelectedBeforeGameSnapshotText(
                    local,
                    context,
                    pendingJson
                );
        }

        if (includeExistingSnapshots)
        {
            var includedBytes = SnapshotPayloadBytes(current);
            includedBytes = await IncludeRecoveryJournalSnapshotsAsync(
                local,
                bundle,
                recoveryJournalJson,
                exactContext,
                includedBytes,
                cancellationToken
            ).ConfigureAwait(false);
            if (exactContext.HasValue)
            {
                _ = await IncludeRetainedSnapshotsAsync(
                    local,
                    bundle,
                    exactContext.Value,
                    includedBytes,
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }

        return bundle;
    }

    private static string ComputeSaveRecoveryExportTreeSha256(
        AutomaticSaveSnapshot snapshot,
        SaveContext? exactContext
    )
    {
        ValidateAutomaticSnapshotPayload(
            snapshot,
            exactContext,
            allowUnknownContext: !exactContext.HasValue
        );
        var lines = snapshot.Manifest.Entries
            .Select(entry =>
            {
                var path = CloudSavePath.Relative(entry.Path);
                if (!entry.Exists)
                    return (Path: path, Line: $"{path}\tfalse\t0\t");

                var bytes = ReadSnapshotFileBytes(snapshot, path);
                var sha256 = AutomaticSyncHash.Compute(bytes);
                if (!string.Equals(
                        sha256,
                        entry.ByteSha256,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    throw new InvalidDataException(
                        $"Current Android export byte hash changed at {path}"
                    );
                }
                return (
                    Path: path,
                    Line: $"{path}\ttrue\t{bytes.LongLength}\t{sha256}"
                );
            })
            .OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Path, StringComparer.Ordinal)
            .Select(item => item.Line);
        return AutomaticSyncHash.Compute(
            Encoding.UTF8.GetBytes(string.Join("\n", lines) + "\n")
        );
    }

    private static string ComputeSaveRecoveryExportContextSha256(
        SaveRecoveryExportSelectedSaveContext context
    )
        => AutomaticSyncHash.Compute(
            Encoding.UTF8.GetBytes(
                context.SteamId64
                    + "\0"
                    + context.SaveNamespace
                    + "\0"
                    + context.RuntimeIdentity
                    + "\0"
                    + context.ModSetFingerprint
                    + "\0"
            )
        );

    private static async Task<long> IncludeRecoveryJournalSnapshotsAsync(
        ISaveStore local,
        SaveRecoveryExportBundle bundle,
        string journalJson,
        SaveContext? selectedContext,
        long includedBytes,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(journalJson))
            return includedBytes;

        SaveRecoveryJournalDocument journal;
        try
        {
            journal = JsonSerializer.Deserialize<SaveRecoveryJournalDocument>(
                journalJson,
                AutomaticSyncJsonOptions
            ) ?? throw new InvalidDataException(
                "The local recovery journal is empty"
            );
            NormalizeRecoveryJournal(journal);
        }
        catch (Exception ex) when (IsRecoveryMetadataException(ex))
        {
            bundle.OmittedSnapshotCount++;
            PatchHelper.Log(
                $"[Recovery] Current-save export preserved the unreadable recovery journal without following its snapshot references: {ex.Message}"
            );
            return includedBytes;
        }

        // Undo is the byte-for-byte rollback authority, so it must have first
        // claim on the bounded export. Applied and source snapshots remain
        // useful corroborating evidence when they are full snapshots too.
        var references = new[]
        {
            (Role: "Undo", Path: journal.UndoSnapshotPath,
                Sha256: journal.UndoSnapshotSha256),
            (Role: "applied", Path: journal.AppliedSnapshotPath,
                Sha256: journal.AppliedSnapshotSha256),
            (Role: "source", Path: journal.SourceSnapshotPath,
                Sha256: journal.SourceSnapshotSha256),
        };
        foreach (var reference in references)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(reference.Path))
                continue;
            try
            {
                RequireSnapshotDocumentHash(
                    reference.Path,
                    reference.Sha256
                );
                var snapshot = await ReadRecoveryOrRetainedSnapshotAsync(
                    local,
                    reference.Path,
                    cancellationToken
                ).ConfigureAwait(false);
                if (!string.Equals(
                        snapshot.Coverage,
                        "full",
                        StringComparison.Ordinal
                    ))
                {
                    bundle.OmittedSnapshotCount++;
                    continue;
                }

                includedBytes = AddVerifiedSnapshotToExport(
                    bundle,
                    snapshot,
                    reference.Path,
                    reference.Sha256,
                    LegacyRecoverySourceKind.Stage3Snapshot,
                    $"Recovery journal {reference.Role}: {snapshot.SourceLabel}",
                    selectedContext,
                    includedBytes
                );
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (IsRecoveryMetadataException(ex))
            {
                bundle.OmittedSnapshotCount++;
                PatchHelper.Log(
                    $"[Recovery] Current-save export omitted an unverified {reference.Role} snapshot: {ex.Message}"
                );
            }
        }
        return includedBytes;
    }

    private static async Task<long> IncludeRetainedSnapshotsAsync(
        ISaveStore local,
        SaveRecoveryExportBundle bundle,
        SaveContext context,
        long includedBytes,
        CancellationToken cancellationToken
    )
    {
        var directory = AutomaticSyncSnapshotDirectory(context);
        string[] filenames;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!local.DirectoryExists(directory))
                return includedBytes;
            filenames = local.GetFilesInDirectory(directory);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsRecoveryMetadataException(ex))
        {
            bundle.OmittedSnapshotCount++;
            PatchHelper.Log(
                $"[Recovery] Current-save export could not enumerate retained snapshots: {ex.Message}"
            );
            return includedBytes;
        }

        var safeFilenames = filenames
            .Where(IsSafeExportSnapshotFilename)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (safeFilenames.Length > LegacyRecoveryFilesPerDirectoryLimit)
        {
            bundle.OmittedSnapshotCount +=
                safeFilenames.Length - LegacyRecoveryFilesPerDirectoryLimit;
            safeFilenames = safeFilenames
                .Take(LegacyRecoveryFilesPerDirectoryLimit)
                .ToArray();
        }

        var verified = new List<(
            string Path,
            string Sha256,
            AutomaticSaveSnapshot Snapshot
        )>();
        foreach (var filename in safeFilenames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = $"{directory}/{filename}";
            try
            {
                var sha256 = SnapshotShaFromPath(path);
                var snapshot = await ReadVerifiedSnapshotAsync(
                    local,
                    path,
                    context,
                    cancellationToken
                ).ConfigureAwait(false);
                if (!string.Equals(
                        snapshot.Coverage,
                        "full",
                        StringComparison.Ordinal
                    ))
                {
                    bundle.OmittedSnapshotCount++;
                    continue;
                }
                verified.Add((path, sha256, snapshot));
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (IsRecoveryMetadataException(ex))
            {
                bundle.OmittedSnapshotCount++;
                PatchHelper.Log(
                    $"[Recovery] Current-save export omitted an unverified retained snapshot {path}: {ex.Message}"
                );
            }
        }

        var ordered = verified
            .OrderByDescending(item => item.Snapshot.CapturedUtc)
            .ThenByDescending(item => item.Path, StringComparer.Ordinal)
            .ToArray();
        if (ordered.Length > AutomaticSyncRetainedSnapshotLimit)
        {
            bundle.OmittedSnapshotCount +=
                ordered.Length - AutomaticSyncRetainedSnapshotLimit;
        }
        foreach (var item in ordered.Take(AutomaticSyncRetainedSnapshotLimit))
        {
            includedBytes = AddVerifiedSnapshotToExport(
                bundle,
                item.Snapshot,
                item.Path,
                item.Sha256,
                LegacyRecoverySourceKind.Stage3Snapshot,
                item.Snapshot.SourceLabel,
                context,
                includedBytes
            );
        }
        return includedBytes;
    }

    private static long AddVerifiedSnapshotToExport(
        SaveRecoveryExportBundle bundle,
        AutomaticSaveSnapshot snapshot,
        string snapshotPath,
        string snapshotSha256,
        LegacyRecoverySourceKind sourceKind,
        string sourceLabel,
        SaveContext? selectedContext,
        long includedBytes
    )
    {
        if (bundle.RecoverySnapshots.Any(item => string.Equals(
                item.SnapshotSha256,
                snapshotSha256,
                StringComparison.OrdinalIgnoreCase
            )))
        {
            return includedBytes;
        }

        var payloadBytes = SnapshotPayloadBytes(snapshot);
        if (payloadBytes > SaveRecoveryExportMaximumSnapshotBytes
            || includedBytes
                > SaveRecoveryExportMaximumSnapshotBytes - payloadBytes)
        {
            bundle.OmittedSnapshotCount++;
            return includedBytes;
        }

        bundle.RecoverySnapshots.Add(new SaveRecoveryExportSnapshot
        {
            CandidateId = snapshotSha256,
            SourceKind = sourceKind,
            SourceLabel = sourceLabel,
            SourcePaths = new[] { snapshotPath },
            Provenance = null,
            Classification = ClassifyExportSnapshot(
                snapshot,
                selectedContext
            ),
            Coverage = snapshot.Coverage,
            SnapshotPath = snapshotPath,
            SnapshotSha256 = snapshotSha256,
            Snapshot = snapshot,
        });
        return includedBytes + payloadBytes;
    }

    private static long SnapshotPayloadBytes(AutomaticSaveSnapshot snapshot)
    {
        long total = 0;
        foreach (var file in snapshot.Files)
        {
            total = checked(
                total + ReadSnapshotFileBytes(snapshot, file.Path).Length
            );
        }
        return total;
    }

    private static LegacyRecoveryClassification ClassifyExportSnapshot(
        AutomaticSaveSnapshot snapshot,
        SaveContext? selectedContext
    )
    {
        if (string.IsNullOrWhiteSpace(snapshot.ContextMarker))
            return LegacyRecoveryClassification.UnknownContext;
        var context = SaveContext.ParseMarker(snapshot.ContextMarker);
        return selectedContext.HasValue && context == selectedContext.Value
            ? LegacyRecoveryClassification.ExactSelectedContext
            : LegacyRecoveryClassification.ExactOtherContext;
    }

    private static bool IsSafeExportSnapshotFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename)
            || filename.Contains('/')
            || filename.Contains('\\')
            || filename.Contains(':')
            || !filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return IsSha256(Path.GetFileNameWithoutExtension(filename));
    }

    private static SaveContext? FindLocallyKnownSelectedContext(
        ISaveStore local,
        SaveRecoverySelection selection,
        string pendingJson
    )
    {
        var contexts = new List<SaveContext>();
        if (TryReadContextMarker(pendingJson, out var pendingContext)
            && MatchesSelection(pendingContext, selection))
        {
            contexts.Add(pendingContext);
        }
        contexts.AddRange(ReadEstablishedBaselineContexts(local, selection));

        var recovery = InspectSaveRecoveryStatus(local);
        if (recovery.SteamId64.HasValue)
        {
            try
            {
                var recoveryContext = SaveContext.Create(
                    recovery.SteamId64.Value,
                    recovery.SaveNamespace,
                    recovery.RuntimeIdentity,
                    recovery.ModSetFingerprint
                );
                if (MatchesSelection(recoveryContext, selection))
                    contexts.Add(recoveryContext);
            }
            catch (ArgumentException)
            {
                // Invalid recovery metadata is still exported verbatim, but it
                // cannot prove the current account context.
            }
        }

        var distinct = contexts
            .Select(context => context.SteamId64)
            .Distinct()
            .ToArray();
        return distinct.Length == 1
            ? contexts.First(context => context.SteamId64 == distinct[0])
            : null;
    }

    private static IReadOnlyList<SaveContext> ReadEstablishedBaselineContexts(
        ISaveStore local,
        SaveRecoverySelection selection
    )
    {
        const string contextsDirectory =
            ".sts2-launcher/automatic-sync/contexts";
        var contexts = new List<SaveContext>();
        try
        {
            if (!local.DirectoryExists(contextsDirectory))
                return contexts;
            foreach (var directory in local.GetDirectoriesInDirectory(
                contextsDirectory
            ))
            {
                var baselinePath =
                    $"{contextsDirectory}/{directory}/baseline.json";
                if (!local.FileExists(baselinePath))
                    continue;
                var baselineJson = local.ReadFile(baselinePath) ?? "";
                if (!TryReadContextMarker(baselineJson, out var context)
                    || !MatchesSelection(context, selection)
                    || !string.Equals(
                        directory,
                        context.StorageKey,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    continue;
                }
                contexts.Add(context);
            }
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Recovery] Current-save export could not inspect established sync baselines: {ex.Message}"
            );
        }
        return contexts;
    }

    private static bool TryReadContextMarker(
        string documentJson,
        out SaveContext context
    )
    {
        context = default;
        if (string.IsNullOrWhiteSpace(documentJson))
            return false;
        try
        {
            using var document = JsonDocument.Parse(documentJson);
            if (!document.RootElement.TryGetProperty(
                    "ContextMarker",
                    out var markerElement
                )
                || markerElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }
            var marker = markerElement.GetString();
            if (string.IsNullOrWhiteSpace(marker))
                return false;
            context = SaveContext.ParseMarker(marker);
            return true;
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            return false;
        }
    }

    private static bool MatchesSelection(
        SaveContext context,
        SaveRecoverySelection selection
    )
        => context.Namespace == selection.Namespace
            && string.Equals(
                context.RuntimeIdentity,
                selection.RuntimeIdentity,
                StringComparison.Ordinal
            )
            && string.Equals(
                context.ModSetFingerprint,
                selection.ModSetFingerprint,
                StringComparison.Ordinal
            );

    private static string ReadSelectedBeforeGameSnapshotText(
        ISaveStore local,
        SaveContext context,
        string pendingJson
    )
    {
        if (TryReadContextMarker(pendingJson, out var pendingContext)
            && pendingContext == context
            && TryReadPendingSnapshotPath(
                pendingJson,
                context,
                out var snapshotPath
            ))
        {
            return ReadOptionalLocalText(local, snapshotPath);
        }

        return ReadOptionalLocalText(
            local,
            AutomaticSyncBeforeGamePath(context)
        );
    }

    private static bool TryReadPendingSnapshotPath(
        string pendingJson,
        SaveContext context,
        out string snapshotPath
    )
    {
        snapshotPath = "";
        try
        {
            using var document = JsonDocument.Parse(pendingJson);
            if (!document.RootElement.TryGetProperty(
                    "BeforeGameSnapshotPath",
                    out var pathElement
                )
                || pathElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }
            var candidate = pathElement.GetString();
            if (string.IsNullOrWhiteSpace(candidate))
                return false;
            var canonical = CloudSavePath.Relative(candidate);
            var expectedPrefix = AutomaticSyncSnapshotDirectory(context) + "/";
            if (!canonical.StartsWith(
                    expectedPrefix,
                    StringComparison.OrdinalIgnoreCase
                )
                || !canonical.EndsWith(
                    ".json",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return false;
            }
            snapshotPath = canonical;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string ReadOptionalLocalText(ISaveStore local, string path)
    {
        if (!local.FileExists(path))
            return "";
        return local.ReadFile(path) ?? "";
    }
}
