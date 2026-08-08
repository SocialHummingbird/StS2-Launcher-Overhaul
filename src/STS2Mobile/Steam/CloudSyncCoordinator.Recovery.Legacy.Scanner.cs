#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const int LegacyRecoveryCandidateLimit = 128;
    private const int LegacyRecoveryFileLimit = 1000;
    private const int LegacyRecoveryMaximumFileBytes = 32 * 1024 * 1024;
    private const long LegacyRecoveryMaximumImportedBytes = 256L * 1024 * 1024;

    internal static async Task<LegacySaveRecoveryScanResult>
        ScanAndImportLegacySavesAsync(
            ISaveStore local,
            SaveNamespace selectedNamespace,
            string runtimeIdentity,
            string? modSetFingerprint,
            ulong? knownSteamId64,
            string? externalSavesRoot,
            CancellationToken cancellationToken = default
        )
    {
        ArgumentNullException.ThrowIfNull(local);
        var selection = LegacyRecoverySelection.Create(
            selectedNamespace,
            runtimeIdentity,
            modSetFingerprint,
            knownSteamId64
        );
        var session = new LegacyRecoveryScanSession(
            local,
            selection,
            externalSavesRoot
        );
        return await session.ScanAsync(cancellationToken).ConfigureAwait(false);
    }

    private readonly record struct LegacyRecoverySelection(
        SaveNamespace Namespace,
        string RuntimeIdentity,
        string ModSetFingerprint,
        ulong? SteamId64
    )
    {
        internal LegacyRecoverySelection WithSteamId64(ulong steamId64)
            => this with { SteamId64 = steamId64 };

        internal static LegacyRecoverySelection Create(
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string? modSetFingerprint,
            ulong? steamId64
        )
        {
            if (!Enum.IsDefined(saveNamespace))
                throw new ArgumentOutOfRangeException(nameof(saveNamespace));
            if (string.IsNullOrWhiteSpace(runtimeIdentity))
            {
                throw new ArgumentException(
                    "Runtime compatibility identity is required",
                    nameof(runtimeIdentity)
                );
            }
            if (steamId64 == 0)
                throw new ArgumentOutOfRangeException(nameof(steamId64));

            var runtime = SteamGameBranch.StorageIdentity(runtimeIdentity);
            var fingerprint = string.IsNullOrWhiteSpace(modSetFingerprint)
                ? string.Empty
                : modSetFingerprint.Trim().ToLowerInvariant();
            if (saveNamespace == SaveNamespace.Vanilla && fingerprint.Length != 0)
            {
                throw new ArgumentException(
                    "Vanilla recovery selection cannot carry a mod-set fingerprint",
                    nameof(modSetFingerprint)
                );
            }
            if (saveNamespace == SaveNamespace.Modded && fingerprint.Length == 0)
            {
                throw new ArgumentException(
                    "Modded recovery selection requires a mod-set fingerprint",
                    nameof(modSetFingerprint)
                );
            }

            return new LegacyRecoverySelection(
                saveNamespace,
                runtime,
                fingerprint,
                steamId64
            );
        }
    }

    private sealed record LegacyRecoveryFileInput(
        string TargetPath,
        string SourcePath,
        DateTimeOffset ModifiedUtc,
        Func<CancellationToken, Task<byte[]>> ReadBytesAsync
    );

    private sealed partial class LegacyRecoveryScanSession
    {
        private readonly ISaveStore _local;
        private LegacyRecoverySelection _selection;
        private readonly string? _externalSavesRoot;
        private readonly List<LegacySaveRecoveryCandidate> _candidates = new();
        private readonly List<LegacyRecoveryExportOnlyFile> _exportOnly = new();
        private readonly HashSet<string> _payloadIdentities = new(
            StringComparer.OrdinalIgnoreCase
        );
        private int _sourceFilesExamined;
        private int _duplicatesSkipped;
        private int _candidateLimitSkipped;
        private int _acceptedSourceFiles;
        private long _acceptedSourceBytes;

        internal LegacyRecoveryScanSession(
            ISaveStore local,
            LegacyRecoverySelection selection,
            string? externalSavesRoot
        )
        {
            _local = local;
            _selection = selection;
            _externalSavesRoot = NormalizeOptionalExternalRoot(externalSavesRoot);
        }

        internal async Task<LegacySaveRecoveryScanResult> ScanAsync(
            CancellationToken cancellationToken
        )
        {
            await ScanStage3SnapshotsAsync(cancellationToken).ConfigureAwait(false);
            await ScanStage2TransferBackupsAsync(cancellationToken).ConfigureAwait(false);
            await ScanLiveSidecarsAsync(cancellationToken).ConfigureAwait(false);
            await ScanManualPullBackupsAsync(cancellationToken).ConfigureAwait(false);
            await ScanExternalBackupsAsync(cancellationToken).ConfigureAwait(false);

            return new LegacySaveRecoveryScanResult(
                _candidates.ToArray(),
                _exportOnly.ToArray(),
                _selection.SteamId64,
                _sourceFilesExamined,
                _duplicatesSkipped,
                _candidateLimitSkipped
            );
        }

        private async Task ImportPartialGroupAsync(
            LegacyRecoverySourceKind sourceKind,
            string sourceLabel,
            IReadOnlyCollection<LegacyRecoveryFileInput> inputs,
            LegacyRecoveryProvenance provenance,
            string contextMarker,
            bool invalidContext,
            IReadOnlyCollection<string>? initialProblems,
            CancellationToken cancellationToken
        )
        {
            if (inputs.Count == 0)
                return;

            var files = new List<AutomaticSaveContentEntry>();
            var manifest = new List<AutomaticSaveManifestEntry>();
            var sourcePaths = new List<string>();
            var problems = new List<string>(initialProblems ?? Array.Empty<string>());
            var capturedUtc = DateTimeOffset.UnixEpoch;
            long totalBytes = 0;

            foreach (var input in inputs
                .GroupBy(item => item.TargetPath, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(item => item.TargetPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.TargetPath, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_acceptedSourceFiles >= LegacyRecoveryFileLimit)
                {
                    _candidateLimitSkipped++;
                    break;
                }

                _sourceFilesExamined++;
                try
                {
                    var bytes = await ReadSourceTwiceAsync(
                        input,
                        cancellationToken
                    ).ConfigureAwait(false);
                    if (bytes.Length > LegacyRecoveryMaximumFileBytes)
                    {
                        throw new InvalidDataException(
                            $"Legacy save exceeds {LegacyRecoveryMaximumFileBytes} bytes"
                        );
                    }
                    if (_acceptedSourceBytes + bytes.Length
                        > LegacyRecoveryMaximumImportedBytes)
                    {
                        _candidateLimitSkipped++;
                        break;
                    }

                    var target = CloudSavePath.Relative(input.TargetPath);
                    var content = DecodeAutomaticSnapshotBytes(bytes, target);
                    if (string.IsNullOrWhiteSpace(content))
                        throw new InvalidDataException("Legacy save file is empty");

                    manifest.Add(new AutomaticSaveManifestEntry
                    {
                        Path = target,
                        Exists = true,
                        Sha256 = AutomaticSyncHash.Compute(content),
                        ByteSha256 = AutomaticSyncHash.Compute(bytes),
                    });
                    files.Add(new AutomaticSaveContentEntry
                    {
                        Path = target,
                        ContentBase64 = Convert.ToBase64String(bytes),
                        ByteSha256 = AutomaticSyncHash.Compute(bytes),
                    });
                    sourcePaths.Add(input.SourcePath);
                    capturedUtc = Later(capturedUtc, input.ModifiedUtc);
                    totalBytes += bytes.Length;
                    _acceptedSourceFiles++;
                    _acceptedSourceBytes += bytes.Length;
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    problems.Add($"{input.SourcePath}: {ex.Message}");
                    AddInvalidCandidate(
                        sourceKind,
                        sourceLabel,
                        input.SourcePath,
                        provenance,
                        ex.Message,
                        input.ModifiedUtc
                    );
                }
            }

            if (files.Count == 0)
                return;

            var snapshot = new AutomaticSaveSnapshot
            {
                ContextMarker = invalidContext ? string.Empty : contextMarker,
                Coverage = "partial",
                SourceKind = RecoverySourceKindLabel(sourceKind),
                SourceLabel = SnapshotSourceLabel(sourceLabel),
                CapturedUtc = NormalizeCapturedUtc(capturedUtc),
                Manifest = AutomaticSaveManifest.Create(manifest),
                Files = files,
            };
            await ImportSnapshotAsync(
                sourceKind,
                sourceLabel,
                sourcePaths,
                provenance,
                snapshot,
                overwriteOnly: true,
                invalidContext,
                problems,
                totalBytes,
                cancellationToken
            ).ConfigureAwait(false);
        }

        private async Task ImportSnapshotAsync(
            LegacyRecoverySourceKind sourceKind,
            string sourceLabel,
            IReadOnlyList<string> sourcePaths,
            LegacyRecoveryProvenance provenance,
            AutomaticSaveSnapshot snapshot,
            bool overwriteOnly,
            bool invalid,
            IReadOnlyList<string> problems,
            long totalBytes,
            CancellationToken cancellationToken
        )
        {
            if (_candidates.Count >= LegacyRecoveryCandidateLimit)
            {
                _candidateLimitSkipped++;
                return;
            }

            var payloadIdentity = SnapshotPayloadIdentity(snapshot);
            if (!_payloadIdentities.Add(payloadIdentity))
            {
                _duplicatesSkipped++;
                return;
            }

            var classification = Classify(provenance, invalid);
            var recoveryDirectory = RecoveryDirectory(classification);
            var imported = await WriteImportedSnapshotAsync(
                _local,
                recoveryDirectory,
                snapshot,
                cancellationToken
            ).ConfigureAwait(false);
            _candidates.Add(new LegacySaveRecoveryCandidate(
                sourceKind,
                sourceLabel,
                sourcePaths,
                provenance,
                classification,
                snapshot.Coverage,
                overwriteOnly,
                imported.Path,
                imported.Sha256,
                snapshot.Files.Count,
                totalBytes,
                snapshot.CapturedUtc,
                classification is not (
                    LegacyRecoveryClassification.QuarantinedForeignAccount
                    or LegacyRecoveryClassification.Invalid
                ),
                problems
            ));
        }

        private static async Task<byte[]> ReadSourceTwiceAsync(
            LegacyRecoveryFileInput input,
            CancellationToken cancellationToken
        )
        {
            var first = await input.ReadBytesAsync(cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var second = await input.ReadBytesAsync(cancellationToken)
                .ConfigureAwait(false);
            if (!first.AsSpan().SequenceEqual(second))
            {
                throw new IOException(
                    "Legacy save changed while its immutable recovery copy was read"
                );
            }
            return first;
        }

        private void AddInvalidCandidate(
            LegacyRecoverySourceKind sourceKind,
            string sourceLabel,
            string sourcePath,
            LegacyRecoveryProvenance provenance,
            string problem,
            DateTimeOffset capturedUtc
        )
        {
            if (_candidates.Count >= LegacyRecoveryCandidateLimit)
            {
                _candidateLimitSkipped++;
                return;
            }

            _candidates.Add(new LegacySaveRecoveryCandidate(
                sourceKind,
                sourceLabel,
                new[] { sourcePath },
                provenance,
                LegacyRecoveryClassification.Invalid,
                "partial",
                OverwriteOnly: true,
                RecoverySnapshotPath: string.Empty,
                SnapshotSha256: string.Empty,
                FileCount: 0,
                TotalBytes: 0,
                NormalizeCapturedUtc(capturedUtc),
                CanRestore: false,
                new[] { problem }
            ));
        }

        private LegacyRecoveryClassification Classify(
            LegacyRecoveryProvenance provenance,
            bool invalid
        )
        {
            if (invalid)
                return LegacyRecoveryClassification.Invalid;
            if (provenance.SteamId64.MatchesSelection == false)
            {
                return LegacyRecoveryClassification
                    .QuarantinedForeignAccount;
            }
            if (provenance.IsFullyExact)
            {
                return provenance.MatchesSelectedContext == true
                    ? LegacyRecoveryClassification.ExactSelectedContext
                    : LegacyRecoveryClassification.ExactOtherContext;
            }
            return LegacyRecoveryClassification.UnknownContext;
        }

        private static string RecoveryDirectory(
            LegacyRecoveryClassification classification
        )
            => classification switch
            {
                LegacyRecoveryClassification.ExactSelectedContext
                    or LegacyRecoveryClassification.ExactOtherContext
                    => RecoveryImportedDirectory,
                LegacyRecoveryClassification.UnknownContext
                    => RecoveryUnknownDirectory,
                _ => RecoveryQuarantineDirectory,
            };

        private LegacyRecoveryProvenance ProvenanceFromContext(
            SaveContext context
        )
            => new(
                Exact(
                    context.SteamId64.ToString(),
                    _selection.SteamId64.HasValue
                        ? context.SteamId64 == _selection.SteamId64.Value
                        : null
                ),
                Exact(
                    context.NamespaceName,
                    context.Namespace == _selection.Namespace
                ),
                Exact(
                    context.RuntimeIdentity,
                    string.Equals(
                        context.RuntimeIdentity,
                        _selection.RuntimeIdentity,
                        StringComparison.Ordinal
                    )
                ),
                Exact(
                    context.ModSetFingerprint,
                    string.Equals(
                        context.ModSetFingerprint,
                        _selection.ModSetFingerprint,
                        StringComparison.Ordinal
                    )
                )
            );

        private LegacyRecoveryProvenance ProvenanceFromNamespace(
            SaveNamespace? saveNamespace
        )
        {
            var namespaceField = saveNamespace.HasValue
                ? Exact(
                    NamespaceName(saveNamespace.Value),
                    saveNamespace.Value == _selection.Namespace
                )
                : Unknown();
            var modSet = saveNamespace == SaveNamespace.Vanilla
                ? Exact(
                    string.Empty,
                    _selection.Namespace == SaveNamespace.Vanilla
                        && _selection.ModSetFingerprint.Length == 0
                )
                : Unknown();
            return new LegacyRecoveryProvenance(
                Unknown(),
                namespaceField,
                Unknown(),
                modSet
            );
        }

        private static LegacyRecoveryProvenanceField Exact(
            string value,
            bool? matchesSelection
        )
            => new(
                LegacyRecoveryProvenanceKnowledge.Exact,
                value ?? string.Empty,
                matchesSelection
            );

        private static LegacyRecoveryProvenanceField Unknown()
            => new(
                LegacyRecoveryProvenanceKnowledge.Unknown,
                string.Empty,
                null
            );

        private static string SnapshotPayloadIdentity(
            AutomaticSaveSnapshot snapshot
        )
        {
            var identity = new StringBuilder();
            identity.Append(snapshot.ContextMarker).Append('\n');
            identity.Append(snapshot.Coverage).Append('\n');
            foreach (var file in snapshot.Files
                .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(file => file.Path, StringComparer.Ordinal))
            {
                identity.Append(CloudSavePath.Relative(file.Path))
                    .Append(':')
                    .Append(file.ByteSha256)
                    .Append('\n');
            }
            return AutomaticSyncHash.Compute(identity.ToString());
        }

        private static DateTimeOffset Later(
            DateTimeOffset left,
            DateTimeOffset right
        )
            => NormalizeCapturedUtc(right) > NormalizeCapturedUtc(left)
                ? NormalizeCapturedUtc(right)
                : NormalizeCapturedUtc(left);

        private static DateTimeOffset NormalizeCapturedUtc(
            DateTimeOffset value
        )
            => value == default
                ? DateTimeOffset.UnixEpoch
                : value.ToUniversalTime();

        private static string RecoverySourceKindLabel(
            LegacyRecoverySourceKind sourceKind
        )
            => sourceKind switch
            {
                LegacyRecoverySourceKind.Stage3Snapshot => "stage3-snapshot",
                LegacyRecoverySourceKind.Stage2TransferBackup => "stage2-backup",
                LegacyRecoverySourceKind.LiveSidecar => "live-sidecar",
                LegacyRecoverySourceKind.ManualPullBackup => "manual-pull-backup",
                LegacyRecoverySourceKind.ExternalCurrent => "external-current",
                LegacyRecoverySourceKind.ExternalHistory => "external-history",
                LegacyRecoverySourceKind.ExternalTimestampedBackup
                    => "external-timestamped-backup",
                _ => "legacy-recovery",
            };

        private static string SnapshotSourceLabel(string sourceLabel)
        {
            var sanitized = new string((sourceLabel ?? string.Empty)
                .Where(character => !char.IsControl(character))
                .ToArray()).Trim();
            if (sanitized.Length == 0)
                return "legacy recovery candidate";
            if (sanitized.Length <= 256)
                return sanitized;
            return sanitized[..207]
                + "-sha256-"
                + AutomaticSyncHash.Compute(sanitized);
        }

        private static string NamespaceName(SaveNamespace saveNamespace)
            => saveNamespace == SaveNamespace.Modded ? "modded" : "vanilla";

        private static string? NormalizeOptionalExternalRoot(string? root)
        {
            if (string.IsNullOrWhiteSpace(root))
                return null;
            return Path.GetFullPath(root);
        }
    }
}
