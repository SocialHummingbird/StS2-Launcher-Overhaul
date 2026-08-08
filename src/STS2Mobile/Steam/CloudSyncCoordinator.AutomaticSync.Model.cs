#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    internal const string AutomaticSyncPendingPath =
        ".sts2-launcher/automatic-sync/pending-sync.json";
    internal const int AutomaticSyncRetainedSnapshotLimit = 5;

    private const int AutomaticSyncDocumentVersion = 1;
    private const int AutomaticSyncSnapshotVersion = 2;
    private const int AutomaticSyncLegacySnapshotVersion = 1;
    private const string AutomaticSyncGameRunningPhase = "game-running";
    private const string AutomaticSyncUploadingPhase = "uploading";
    private const string AutomaticSyncDownloadingPhase = "downloading";

    internal static string AutomaticSyncBaselinePath(SaveContext context)
        => $".sts2-launcher/automatic-sync/contexts/{context.StorageKey}/baseline.json";

    internal static string AutomaticSyncBeforeGamePath(SaveContext context)
        => $".sts2-launcher/automatic-sync/contexts/{context.StorageKey}/before-game.json";

    internal static string AutomaticSyncSnapshotDirectory(SaveContext context)
        => $".sts2-launcher/automatic-sync/contexts/{context.StorageKey}/snapshots";

    internal static string AutomaticSyncSnapshotPath(
        SaveContext context,
        string sha256
    )
        => $"{AutomaticSyncSnapshotDirectory(context)}/{sha256}.json";

    internal sealed class AutomaticSaveManifestEntry
    {
        public string Path { get; set; } = "";
        public bool Exists { get; set; }
        public string Sha256 { get; set; } = "";
        public string ByteSha256 { get; set; } = "";
    }

    internal sealed class AutomaticSaveManifest
    {
        public int Version { get; set; } = AutomaticSyncDocumentVersion;
        public List<AutomaticSaveManifestEntry> Entries { get; set; } = new();

        internal static AutomaticSaveManifest Create(
            IEnumerable<AutomaticSaveManifestEntry> entries
        )
            => new()
            {
                Entries = (entries ?? Array.Empty<AutomaticSaveManifestEntry>())
                    .GroupBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.Last())
                    .OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(entry => entry.Path, StringComparer.Ordinal)
                    .Select(CloneEntry)
                    .ToList(),
            };

        internal bool ContentEquals(AutomaticSaveManifest? other)
        {
            if (other is null || Version != AutomaticSyncDocumentVersion)
                return false;

            var paths = Entries.Select(entry => entry.Path)
                .Concat(other.Entries.Select(entry => entry.Path))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            return paths.All(path => State(path).Equals(other.State(path)));
        }

        internal bool IsPathwiseBlendOf(
            AutomaticSaveManifest baseline,
            AutomaticSaveManifest target
        )
        {
            ArgumentNullException.ThrowIfNull(baseline);
            ArgumentNullException.ThrowIfNull(target);
            var paths = Entries.Select(entry => entry.Path)
                .Concat(baseline.Entries.Select(entry => entry.Path))
                .Concat(target.Entries.Select(entry => entry.Path))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var path in paths)
            {
                var actual = State(path);
                if (!actual.Equals(baseline.State(path))
                    && !actual.Equals(target.State(path)))
                {
                    return false;
                }
            }

            return true;
        }

        internal AutomaticSaveManifestEntry Entry(string path)
        {
            var existing = Entries.FirstOrDefault(entry => string.Equals(
                entry.Path,
                path,
                StringComparison.OrdinalIgnoreCase
            ));
            return existing is null
                ? MissingEntry(path)
                : CloneEntry(existing);
        }

        private AutomaticFileState State(string path)
        {
            var entry = Entries.FirstOrDefault(candidate => string.Equals(
                candidate.Path,
                path,
                StringComparison.OrdinalIgnoreCase
            ));
            return entry is null || !entry.Exists
                ? AutomaticFileState.Missing
                : new AutomaticFileState(
                    true,
                    string.IsNullOrEmpty(entry.ByteSha256)
                        ? entry.Sha256 ?? ""
                        : entry.ByteSha256
                );
        }

        private static AutomaticSaveManifestEntry CloneEntry(
            AutomaticSaveManifestEntry entry
        )
            => new()
            {
                Path = CloudSavePath.Relative(entry.Path),
                Exists = entry.Exists,
                Sha256 = entry.Exists ? (entry.Sha256 ?? "") : "",
                ByteSha256 = entry.Exists ? (entry.ByteSha256 ?? "") : "",
            };
    }

    internal sealed class AutomaticSaveContentEntry
    {
        public string Path { get; set; } = "";
        // Version 1 snapshots stored text only. It remains readable so an
        // interrupted Stage 3 session can still recover after an upgrade.
        public string Content { get; set; } = "";
        public string ContentBase64 { get; set; } = "";
        public string ByteSha256 { get; set; } = "";
    }

    internal sealed class AutomaticSaveSnapshot
    {
        public int Version { get; set; } = AutomaticSyncSnapshotVersion;
        public string ContextMarker { get; set; } = "";
        public string Coverage { get; set; } = "full";
        public string SourceKind { get; set; } = "";
        public string SourceLabel { get; set; } = "";
        public DateTimeOffset CapturedUtc { get; set; }
        public AutomaticSaveManifest Manifest { get; set; } = new();
        public List<AutomaticSaveContentEntry> Files { get; set; } = new();
    }

    internal readonly record struct RetainedAutomaticSaveSnapshot(
        string Path,
        string Sha256,
        AutomaticSaveSnapshot Snapshot
    );

    private sealed class AutomaticSyncBaselineDocument
    {
        public int Version { get; set; } = AutomaticSyncDocumentVersion;
        public string ContextMarker { get; set; } = "";
        public AutomaticSaveManifest LocalManifest { get; set; } = new();
        public AutomaticSaveManifest RemoteManifest { get; set; } = new();
    }

    private sealed class AutomaticSyncPendingDocument
    {
        public int Version { get; set; } = AutomaticSyncDocumentVersion;
        public string Phase { get; set; } = "";
        public string ContextMarker { get; set; } = "";
        public AutomaticSaveManifest LocalBaseline { get; set; } = new();
        public AutomaticSaveManifest RemoteBaseline { get; set; } = new();
        public AutomaticSaveManifest? SourceManifest { get; set; }
        public AutomaticSaveManifest? ExpectedDestinationManifest { get; set; }
        public AutomaticFileState RemoteMarkerBaseline { get; set; }
        public string BeforeGameSnapshotPath { get; set; } = "";
        public string BeforeGameSnapshotSha256 { get; set; } = "";
        public bool AllowUnreadableMarkerAdoption { get; set; }
    }

    internal readonly record struct AutomaticFileState(bool Exists, string Sha256)
    {
        internal static AutomaticFileState Missing => new(false, "");
    }

    private static AutomaticSaveManifestEntry MissingEntry(string path)
        => new()
        {
            Path = CloudSavePath.Relative(path),
            Exists = false,
            Sha256 = "",
            ByteSha256 = "",
        };
}
