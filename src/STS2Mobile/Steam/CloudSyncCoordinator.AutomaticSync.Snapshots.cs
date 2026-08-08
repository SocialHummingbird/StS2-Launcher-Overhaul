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
    private static readonly string[] AllowedImportedSnapshotDirectories =
    {
        ".sts2-launcher/recovery/imported",
        ".sts2-launcher/recovery/unknown",
        ".sts2-launcher/recovery/quarantine",
        ".sts2-launcher/recovery/undo",
        ".sts2-launcher/recovery/applied",
    };

    internal static async Task<RetainedAutomaticSaveSnapshot>
        WriteRetainedSnapshotAsync(
            ISaveStore local,
            SaveContext context,
            AutomaticSaveSnapshot snapshot,
            CancellationToken cancellationToken
        )
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(snapshot);
        RequireCurrentSnapshotVersion(snapshot);
        ValidateAutomaticSnapshotPayload(
            snapshot,
            context,
            allowUnknownContext: false
        );

        var content = SerializeAutomaticSyncDocument(snapshot);
        var sha256 = AutomaticSyncHash.Compute(content);
        var path = AutomaticSyncSnapshotPath(context, sha256);
        if (local.FileExists(path))
        {
            var existing = await CancellableSaveStore.ReadFileAsync(
                local,
                path,
                cancellationToken
            ).ConfigureAwait(false);
            RequireHash(path, sha256, existing, "retained snapshot");
        }
        else
        {
            await WriteAtomicAutomaticSyncTextAsync(
                local,
                path,
                content,
                cancellationToken
            ).ConfigureAwait(false);
        }

        var verified = await ReadVerifiedSnapshotAsync(
            local,
            path,
            context,
            cancellationToken
        ).ConfigureAwait(false);
        return new RetainedAutomaticSaveSnapshot(path, sha256, verified);
    }

    internal static async Task<RetainedAutomaticSaveSnapshot>
        WriteImportedSnapshotAsync(
            ISaveStore local,
            string recoveryDirectory,
            AutomaticSaveSnapshot snapshot,
            CancellationToken cancellationToken
        )
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(snapshot);
        RequireCurrentSnapshotVersion(snapshot);
        var directory = CloudSavePath.Relative(recoveryDirectory).TrimEnd('/');
        if (!AllowedImportedSnapshotDirectories.Contains(
                directory,
                StringComparer.OrdinalIgnoreCase
            ))
        {
            throw new InvalidDataException(
                "Imported snapshots must remain inside launcher recovery storage"
            );
        }
        ValidateAutomaticSnapshotPayload(
            snapshot,
            expectedContext: null,
            allowUnknownContext: true
        );

        var content = SerializeAutomaticSyncDocument(snapshot);
        var sha256 = AutomaticSyncHash.Compute(content);
        var path = $"{directory}/{sha256}.json";
        if (local.FileExists(path))
        {
            var existing = await CancellableSaveStore.ReadFileAsync(
                local,
                path,
                cancellationToken
            ).ConfigureAwait(false);
            RequireHash(path, sha256, existing, "imported snapshot");
        }
        else
        {
            await WriteAtomicAutomaticSyncTextAsync(
                local,
                path,
                content,
                cancellationToken
            ).ConfigureAwait(false);
        }

        var verified = await ReadVerifiedImportedSnapshotAsync(
            local,
            path,
            cancellationToken
        ).ConfigureAwait(false);
        return new RetainedAutomaticSaveSnapshot(path, sha256, verified);
    }

    internal static async Task<AutomaticSaveSnapshot>
        ReadVerifiedSnapshotAsync(
            ISaveStore local,
            string path,
            SaveContext? expectedContext,
            CancellationToken cancellationToken
        )
    {
        if (!expectedContext.HasValue)
        {
            throw new InvalidDataException(
                "A retained snapshot requires an exact save context"
            );
        }
        return await ReadVerifiedSnapshotCoreAsync(
            local,
            path,
            expectedContext,
            allowUnknownContext: false,
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal static async Task<AutomaticSaveSnapshot>
        ReadVerifiedImportedSnapshotAsync(
            ISaveStore local,
            string path,
            CancellationToken cancellationToken
        )
        => await ReadVerifiedImportedSnapshotAsync(
            local,
            path,
            expectedContext: null,
            cancellationToken
        ).ConfigureAwait(false);

    internal static async Task<AutomaticSaveSnapshot>
        ReadVerifiedImportedSnapshotAsync(
            ISaveStore local,
            string path,
            SaveContext? expectedContext,
            CancellationToken cancellationToken
        )
        => await ReadVerifiedSnapshotCoreAsync(
            local,
            path,
            expectedContext,
            allowUnknownContext: true,
            cancellationToken
        ).ConfigureAwait(false);

    internal static async Task PruneRetainedSnapshotsAsync(
        ISaveStore local,
        SaveContext context,
        IEnumerable<string>? protectedPaths,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(local);
        var directory = AutomaticSyncSnapshotDirectory(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!local.DirectoryExists(directory))
            return;

        var protectedSet = new HashSet<string>(
            (protectedPaths ?? Array.Empty<string>())
                .Select(CloudSavePath.Relative),
            StringComparer.OrdinalIgnoreCase
        );
        await AddPendingSnapshotProtectionAsync(
            local,
            context,
            protectedSet,
            cancellationToken
        ).ConfigureAwait(false);

        var verified = new List<(string Path, AutomaticSaveSnapshot Snapshot)>();
        foreach (var filename in local.GetFilesInDirectory(directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;
            var path = $"{directory}/{filename}";
            try
            {
                var snapshot = await ReadVerifiedSnapshotAsync(
                    local,
                    path,
                    context,
                    cancellationToken
                ).ConfigureAwait(false);
                verified.Add((path, snapshot));
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Corrupt evidence is left in place for Stage 4 quarantine and
                // support export. It is never counted as a restorable snapshot.
                PatchHelper.Log(
                    $"[Recovery] Ignoring unverified retained snapshot {path}: {ex.Message}"
                );
            }
        }

        var stale = verified
            .Where(item => !protectedSet.Contains(item.Path))
            .OrderByDescending(item => item.Snapshot.CapturedUtc)
            .ThenByDescending(item => item.Path, StringComparer.Ordinal)
            .Skip(AutomaticSyncRetainedSnapshotLimit)
            .ToArray();
        foreach (var item in stale)
        {
            await CancellableSaveStore.DeleteFileAsync(
                local,
                item.Path,
                cancellationToken
            ).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (local.FileExists(item.Path))
            {
                throw new IOException(
                    $"Retained snapshot could not be pruned: {item.Path}"
                );
            }
        }
    }

    internal static byte[] ReadSnapshotFileBytes(
        AutomaticSaveSnapshot snapshot,
        string path
    )
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var canonical = CloudSavePath.Relative(path);
        var file = snapshot.Files.SingleOrDefault(candidate => string.Equals(
            candidate.Path,
            canonical,
            StringComparison.OrdinalIgnoreCase
        )) ?? throw new FileNotFoundException(
            $"Snapshot does not contain save bytes: {canonical}"
        );
        return SnapshotEntryBytes(snapshot.Version, file, canonical);
    }

    private static async Task<AutomaticSaveSnapshot>
        ReadVerifiedSnapshotCoreAsync(
            ISaveStore local,
            string path,
            SaveContext? expectedContext,
            bool allowUnknownContext,
            CancellationToken cancellationToken
        )
    {
        ArgumentNullException.ThrowIfNull(local);
        var canonical = CloudSavePath.Relative(path);
        if (!local.FileExists(canonical))
            throw new FileNotFoundException("Save snapshot is missing", canonical);
        var content = await CancellableSaveStore.ReadFileAsync(
            local,
            canonical,
            cancellationToken
        ).ConfigureAwait(false);
        RequireContentAddressedSnapshotPath(
            canonical,
            content,
            expectedContext,
            allowUnknownContext
        );

        AutomaticSaveSnapshot snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<AutomaticSaveSnapshot>(
                content,
                AutomaticSyncJsonOptions
            ) ?? throw new InvalidDataException("Save snapshot is empty");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Save snapshot is not valid JSON", ex);
        }

        RequireSnapshotVersionMatchesPath(canonical, snapshot.Version);

        ValidateAutomaticSnapshotPayload(
            snapshot,
            expectedContext,
            allowUnknownContext
        );
        return snapshot;
    }

    private static void RequireCurrentSnapshotVersion(
        AutomaticSaveSnapshot snapshot
    )
    {
        if (snapshot.Version != AutomaticSyncSnapshotVersion)
        {
            throw new InvalidDataException(
                "New save snapshots must use the raw-byte snapshot version"
            );
        }
    }

    private static void RequireSnapshotVersionMatchesPath(
        string path,
        int snapshotVersion
    )
    {
        var filename = Path.GetFileName(
            path.Replace('/', Path.DirectorySeparatorChar)
        );
        var contentAddressed = IsSha256(
            Path.GetFileNameWithoutExtension(filename)
        );
        var expectedVersion = contentAddressed
            ? AutomaticSyncSnapshotVersion
            : AutomaticSyncLegacySnapshotVersion;
        if (snapshotVersion != expectedVersion)
        {
            throw new InvalidDataException(
                contentAddressed
                    ? "Content-addressed save snapshots must use raw-byte payloads"
                    : "Only the legacy text snapshot is valid at the fixed before-game path"
            );
        }
    }

    private static void ValidateAutomaticSnapshotPayload(
        AutomaticSaveSnapshot snapshot,
        SaveContext? expectedContext,
        bool allowUnknownContext
    )
    {
        if (snapshot.Version is not (
                AutomaticSyncLegacySnapshotVersion
                or AutomaticSyncSnapshotVersion
            ))
        {
            throw new InvalidDataException(
                "Save snapshot has an unsupported version"
            );
        }

        SaveContext? actualContext = null;
        if (string.IsNullOrWhiteSpace(snapshot.ContextMarker))
        {
            if (expectedContext.HasValue
                || !allowUnknownContext
                || snapshot.Version < AutomaticSyncSnapshotVersion)
            {
                throw new InvalidDataException(
                    "Save snapshot has no verified save context"
                );
            }
        }
        else
        {
            try
            {
                actualContext = SaveContext.ParseMarker(snapshot.ContextMarker);
                if (expectedContext.HasValue)
                    expectedContext.Value.RequireExactMatch(actualContext.Value);
            }
            catch (Exception ex)
                when (ex is InvalidDataException or InvalidOperationException)
            {
                throw new InvalidDataException(
                    "Save snapshot belongs to another save context",
                    ex
                );
            }
        }

        if (snapshot.Version == AutomaticSyncSnapshotVersion)
            ValidateSnapshotMetadata(snapshot);
        ValidateSnapshotManifest(snapshot, actualContext);
        ValidateSnapshotFiles(snapshot, actualContext);
    }

    private static void ValidateSnapshotMetadata(AutomaticSaveSnapshot snapshot)
    {
        if (snapshot.Coverage is not ("full" or "partial"))
            throw new InvalidDataException("Save snapshot coverage is invalid");
        RequireSnapshotLabel(snapshot.SourceKind, "source kind", 64);
        RequireSnapshotLabel(snapshot.SourceLabel, "source label", 256);
        if (snapshot.CapturedUtc == default
            || snapshot.CapturedUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidDataException(
                "Save snapshot capture time is missing or not UTC"
            );
        }
    }

    private static void ValidateSnapshotManifest(
        AutomaticSaveSnapshot snapshot,
        SaveContext? context
    )
    {
        if (snapshot.Manifest is null)
            throw new InvalidDataException("Save snapshot manifest is missing");
        if (context.HasValue)
        {
            Normalize(snapshot.Manifest, context.Value);
            if (snapshot.Coverage == "full")
                RequireFullManifestCoverage(snapshot.Manifest, context.Value.Namespace);
            return;
        }

        NormalizeUnknownSnapshotManifest(snapshot.Manifest);
    }

    private static void ValidateSnapshotFiles(
        AutomaticSaveSnapshot snapshot,
        SaveContext? context
    )
    {
        if (snapshot.Files is null)
            throw new InvalidDataException("Save snapshot file payload is missing");
        var manifestFiles = snapshot.Manifest.Entries
            .Where(entry => entry.Exists)
            .ToDictionary(
                entry => entry.Path,
                entry => entry,
                StringComparer.OrdinalIgnoreCase
            );
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in snapshot.Files)
        {
            var path = CloudSavePath.Relative(file.Path);
            if (!seen.Add(path))
                throw new InvalidDataException($"Save snapshot repeats path: {path}");
            if (!manifestFiles.TryGetValue(path, out var manifestEntry))
            {
                throw new InvalidDataException(
                    $"Save snapshot contains bytes absent from its manifest: {path}"
                );
            }
            if (context.HasValue)
            {
                if (!SaveTransferAllowlist.IsAllowedPath(context.Value.Namespace, path))
                    throw new InvalidDataException($"Save snapshot path is not allowlisted: {path}");
            }
            else if (!IsAllowedUnknownSnapshotPath(path))
            {
                throw new InvalidDataException($"Imported snapshot path is not allowlisted: {path}");
            }

            var bytes = SnapshotEntryBytes(snapshot.Version, file, path);
            var content = DecodeAutomaticSnapshotBytes(bytes, path);
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidDataException($"Save snapshot file is empty: {path}");
            if (!string.Equals(
                    AutomaticSyncHash.Compute(content),
                    manifestEntry.Sha256,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                throw new InvalidDataException(
                    $"Save snapshot text hash does not match its manifest: {path}"
                );
            }
            if (!string.IsNullOrEmpty(manifestEntry.ByteSha256)
                && !string.Equals(
                    AutomaticSyncHash.Compute(bytes),
                    manifestEntry.ByteSha256,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                throw new InvalidDataException(
                    $"Save snapshot byte hash does not match its manifest: {path}"
                );
            }
        }

        if (seen.Count != manifestFiles.Count
            || manifestFiles.Keys.Any(path => !seen.Contains(path)))
        {
            throw new InvalidDataException(
                "Save snapshot manifest and file payload do not contain the same existing paths"
            );
        }
    }

    private static byte[] SnapshotEntryBytes(
        int snapshotVersion,
        AutomaticSaveContentEntry file,
        string path
    )
    {
        if (snapshotVersion == AutomaticSyncLegacySnapshotVersion)
        {
            if (!string.IsNullOrEmpty(file.ContentBase64)
                || !string.IsNullOrEmpty(file.ByteSha256))
            {
                throw new InvalidDataException(
                    $"Legacy snapshot mixes text and raw-byte payloads: {path}"
                );
            }
            return Encoding.UTF8.GetBytes(file.Content ?? string.Empty);
        }

        if (!string.IsNullOrEmpty(file.Content))
        {
            throw new InvalidDataException(
                $"Version 2 snapshot contains an ambiguous text payload: {path}"
            );
        }
        if (!IsSha256(file.ByteSha256))
            throw new InvalidDataException($"Save snapshot byte hash is invalid: {path}");

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(file.ContentBase64 ?? string.Empty);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException(
                $"Save snapshot raw payload is not valid Base64: {path}",
                ex
            );
        }
        if (!string.Equals(
                AutomaticSyncHash.Compute(bytes),
                file.ByteSha256,
                StringComparison.OrdinalIgnoreCase
            ))
        {
            throw new InvalidDataException(
                $"Save snapshot raw-byte hash does not match: {path}"
            );
        }
        return bytes;
    }

    private static void NormalizeUnknownSnapshotManifest(
        AutomaticSaveManifest manifest
    )
    {
        if (manifest is null || manifest.Version != AutomaticSyncDocumentVersion)
            throw new InvalidDataException("Save snapshot manifest has an unsupported version");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in manifest.Entries)
        {
            var path = CloudSavePath.Relative(entry.Path);
            if (!seen.Add(path))
                throw new InvalidDataException($"Save snapshot manifest repeats path: {path}");
            if (!IsAllowedUnknownSnapshotPath(path))
                throw new InvalidDataException($"Imported snapshot path is not allowlisted: {path}");
            if (entry.Exists && !IsSha256(entry.Sha256))
                throw new InvalidDataException($"Save snapshot manifest hash is invalid: {path}");
            if (entry.Exists
                && !string.IsNullOrEmpty(entry.ByteSha256)
                && !IsSha256(entry.ByteSha256))
            {
                throw new InvalidDataException(
                    $"Save snapshot manifest byte hash is invalid: {path}"
                );
            }
            if (!entry.Exists && !string.IsNullOrEmpty(entry.Sha256))
                throw new InvalidDataException($"Missing snapshot path has a hash: {path}");
            if (!entry.Exists && !string.IsNullOrEmpty(entry.ByteSha256))
            {
                throw new InvalidDataException(
                    $"Missing snapshot path has a byte hash: {path}"
                );
            }
        }
        var hasVanilla = manifest.Entries.Any(entry =>
            !string.Equals(
                entry.Path,
                SaveTransferAllowlist.SharedProfilePath,
                StringComparison.OrdinalIgnoreCase
            )
            && SaveTransferAllowlist.IsAllowedPath(SaveNamespace.Vanilla, entry.Path)
        );
        var hasModded = manifest.Entries.Any(entry =>
            SaveTransferAllowlist.IsAllowedPath(SaveNamespace.Modded, entry.Path)
            && !SaveTransferAllowlist.IsAllowedPath(SaveNamespace.Vanilla, entry.Path)
        );
        if (hasVanilla && hasModded)
            throw new InvalidDataException("Imported snapshot mixes vanilla and modded paths");
        manifest.Entries = AutomaticSaveManifest.Create(manifest.Entries).Entries;
    }

    private static bool IsAllowedUnknownSnapshotPath(string path)
        => SaveTransferAllowlist.IsAllowedPath(SaveNamespace.Vanilla, path)
            || SaveTransferAllowlist.IsAllowedPath(SaveNamespace.Modded, path);

    private static void RequireFullManifestCoverage(
        AutomaticSaveManifest manifest,
        SaveNamespace saveNamespace
    )
    {
        var paths = manifest.Entries
            .Select(entry => entry.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!paths.Contains(SaveTransferAllowlist.SharedProfilePath))
            throw new InvalidDataException("Full save snapshot omits profile.save");
        for (var profileId = 1; profileId <= SaveTransferAllowlist.ProfileCount; profileId++)
        {
            var directory = SaveTransferAllowlist.SaveDirectory(
                saveNamespace,
                profileId
            );
            foreach (var filename in SaveTransferAllowlist.FixedProfileFiles)
            {
                var path = $"{directory}/{filename}";
                if (!paths.Contains(path))
                    throw new InvalidDataException($"Full save snapshot omits path: {path}");
            }
        }
    }

    private static async Task AddPendingSnapshotProtectionAsync(
        ISaveStore local,
        SaveContext context,
        ISet<string> protectedPaths,
        CancellationToken cancellationToken
    )
    {
        if (!local.FileExists(AutomaticSyncPendingPath))
            return;
        AutomaticSyncPendingDocument? pending;
        try
        {
            pending = await ReadPendingAutomaticSyncAsync(
                local,
                cancellationToken
            ).ConfigureAwait(false);
            if (pending is null)
                return;
            NormalizePending(pending);
            SaveContext.ParseMarker(pending.ContextMarker).RequireExactMatch(context);
        }
        catch (Exception ex)
            when (ex is InvalidDataException or InvalidOperationException)
        {
            // Unknown pending state is a reason to preserve every snapshot.
            foreach (var filename in local.GetFilesInDirectory(
                AutomaticSyncSnapshotDirectory(context)
            ))
            {
                protectedPaths.Add(
                    $"{AutomaticSyncSnapshotDirectory(context)}/{filename}"
                );
            }
            return;
        }

        if (!string.IsNullOrWhiteSpace(pending.BeforeGameSnapshotPath))
            protectedPaths.Add(CloudSavePath.Relative(pending.BeforeGameSnapshotPath));
    }

    private static void RequireContentAddressedSnapshotPath(
        string path,
        string content,
        SaveContext? expectedContext,
        bool allowUnknownContext
    )
    {
        var filename = Path.GetFileName(path.Replace('/', Path.DirectorySeparatorChar));
        var name = Path.GetFileNameWithoutExtension(filename);
        if (IsSha256(name))
        {
            RequireHash(path, name, content, "snapshot payload");
            if (allowUnknownContext)
            {
                var separator = path.LastIndexOf('/');
                var directory = separator < 0 ? "" : path[..separator];
                if (!AllowedImportedSnapshotDirectories.Contains(
                        directory,
                        StringComparer.OrdinalIgnoreCase
                    ))
                {
                    throw new InvalidDataException(
                        "Imported snapshot path is outside recovery storage"
                    );
                }
                return;
            }
            if (expectedContext.HasValue)
            {
                var expectedPath = AutomaticSyncSnapshotPath(
                    expectedContext.Value,
                    name
                );
                if (!string.Equals(
                        path,
                        expectedPath,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    throw new InvalidDataException(
                        "Retained snapshot path belongs to another save context"
                    );
                }
            }
            return;
        }

        if (allowUnknownContext
            || !expectedContext.HasValue
            || !string.Equals(
                path,
                AutomaticSyncBeforeGamePath(expectedContext.Value),
                StringComparison.OrdinalIgnoreCase
            ))
        {
            throw new InvalidDataException(
                "Save snapshot path is not immutable and content-addressed"
            );
        }
    }

    private static void RequireSnapshotLabel(
        string value,
        string label,
        int maximumLength
    )
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > maximumLength
            || value.Any(char.IsControl))
        {
            throw new InvalidDataException($"Save snapshot {label} is invalid");
        }
    }

    internal static string DecodeAutomaticSnapshotBytes(
        byte[] bytes,
        string path
    )
    {
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = new StreamReader(
                stream,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false,
                    throwOnInvalidBytes: true
                ),
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: false
            );
            return reader.ReadToEnd();
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidDataException(
                $"Save snapshot contains invalid text bytes: {path}",
                ex
            );
        }
    }
}
