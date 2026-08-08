#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    internal const string RecoveryImportedDirectory =
        ".sts2-launcher/recovery/imported";
    internal const string RecoveryUnknownDirectory =
        ".sts2-launcher/recovery/unknown";
    internal const string RecoveryQuarantineDirectory =
        ".sts2-launcher/recovery/quarantine";

    internal enum LegacyRecoverySourceKind
    {
        Stage3Snapshot,
        Stage2TransferBackup,
        LiveSidecar,
        ManualPullBackup,
        ExternalCurrent,
        ExternalHistory,
        ExternalTimestampedBackup,
    }

    internal enum LegacyRecoveryProvenanceKnowledge
    {
        Exact,
        Unknown,
    }

    internal enum LegacyRecoveryClassification
    {
        ExactSelectedContext,
        ExactOtherContext,
        UnknownContext,
        QuarantinedForeignAccount,
        Invalid,
    }

    internal sealed record LegacyRecoveryProvenanceField(
        LegacyRecoveryProvenanceKnowledge Knowledge,
        string Value,
        bool? MatchesSelection
    );

    internal sealed record LegacyRecoveryProvenance(
        LegacyRecoveryProvenanceField SteamId64,
        LegacyRecoveryProvenanceField SaveNamespace,
        LegacyRecoveryProvenanceField RuntimeIdentity,
        LegacyRecoveryProvenanceField ModSetFingerprint
    )
    {
        internal bool IsFullyExact
            => SteamId64.Knowledge == LegacyRecoveryProvenanceKnowledge.Exact
                && SaveNamespace.Knowledge == LegacyRecoveryProvenanceKnowledge.Exact
                && RuntimeIdentity.Knowledge == LegacyRecoveryProvenanceKnowledge.Exact
                && ModSetFingerprint.Knowledge == LegacyRecoveryProvenanceKnowledge.Exact;

        internal bool? MatchesSelectedContext
        {
            get
            {
                var fields = new[]
                {
                    SteamId64,
                    SaveNamespace,
                    RuntimeIdentity,
                    ModSetFingerprint,
                };
                if (Array.Exists(
                        fields,
                        field => field.MatchesSelection == false
                    ))
                {
                    return false;
                }

                return Array.TrueForAll(
                    fields,
                    field => field.MatchesSelection == true
                )
                    ? true
                    : null;
            }
        }
    }

    internal sealed record LegacySaveRecoveryCandidate(
        LegacyRecoverySourceKind SourceKind,
        string SourceLabel,
        IReadOnlyList<string> SourcePaths,
        LegacyRecoveryProvenance Provenance,
        LegacyRecoveryClassification Classification,
        string Coverage,
        bool OverwriteOnly,
        string RecoverySnapshotPath,
        string SnapshotSha256,
        int FileCount,
        long TotalBytes,
        DateTimeOffset CapturedUtc,
        bool CanRestore,
        IReadOnlyList<string> Problems
    )
    {
        internal bool Imported
            => !string.IsNullOrWhiteSpace(RecoverySnapshotPath)
                && !string.IsNullOrWhiteSpace(SnapshotSha256);

        internal ulong? ExactSteamId64
        {
            get
            {
                if (Provenance.SteamId64.Knowledge
                    != LegacyRecoveryProvenanceKnowledge.Exact)
                {
                    return null;
                }
                if (!ulong.TryParse(
                        Provenance.SteamId64.Value,
                        out var steamId64
                    )
                    || steamId64 == 0)
                {
                    throw new InvalidDataException(
                        "Recovery candidate has invalid exact Steam-account provenance."
                    );
                }
                return steamId64;
            }
        }
    }

    internal sealed record LegacyRecoveryExportOnlyFile(
        string SourcePath,
        long ByteCount,
        DateTimeOffset ModifiedUtc,
        string Reason
    );

    internal sealed record LegacySaveRecoveryScanResult(
        IReadOnlyList<LegacySaveRecoveryCandidate> Candidates,
        IReadOnlyList<LegacyRecoveryExportOnlyFile> ExportOnlyFiles,
        ulong? KnownSelectedSteamId64,
        int SourcesExamined,
        int DuplicateCandidatesSkipped,
        int CandidateLimitSkipped
    );
}
