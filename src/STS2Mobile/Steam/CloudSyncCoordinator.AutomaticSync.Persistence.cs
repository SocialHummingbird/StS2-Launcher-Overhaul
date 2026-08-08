#nullable enable

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static readonly JsonSerializerOptions AutomaticSyncJsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = false,
            WriteIndented = false,
        };

    internal static bool HasPendingAutomaticSync(ISaveStore local)
    {
        ArgumentNullException.ThrowIfNull(local);
        return local.FileExists(AutomaticSyncPendingPath);
    }

    internal static bool HasPendingAutomaticSync()
        => HasPendingAutomaticSync(CloudSaveStoreFactory.CreateLocalStore());

    private static async Task<AutomaticSyncPendingDocument?>
        ReadPendingAutomaticSyncAsync(
            ISaveStore local,
            CancellationToken cancellationToken
        )
        => await ReadAutomaticSyncDocumentAsync<AutomaticSyncPendingDocument>(
            local,
            AutomaticSyncPendingPath,
            cancellationToken
        ).ConfigureAwait(false);

    private static async Task<AutomaticSyncBaselineDocument?>
        ReadAutomaticBaselineAsync(
            ISaveStore local,
            SaveContext context,
            CancellationToken cancellationToken
        )
    {
        var document = await ReadAutomaticSyncDocumentAsync<
            AutomaticSyncBaselineDocument
        >(
            local,
            AutomaticSyncBaselinePath(context),
            cancellationToken
        ).ConfigureAwait(false);
        if (document is null)
            return null;

        if (document.Version != AutomaticSyncDocumentVersion)
            throw new InvalidDataException("Automatic sync baseline has an unsupported version");
        RequireDocumentContext(document.ContextMarker, context, "baseline");
        Normalize(document.LocalManifest, context);
        Normalize(document.RemoteManifest, context);
        return document;
    }

    private static async Task WriteAutomaticBaselineAsync(
        ISaveStore local,
        SaveContext context,
        AutomaticSaveManifest localManifest,
        AutomaticSaveManifest remoteManifest,
        CancellationToken cancellationToken
    )
    {
        var document = new AutomaticSyncBaselineDocument
        {
            ContextMarker = context.SerializeMarker(),
            LocalManifest = AutomaticSaveManifest.Create(localManifest.Entries),
            RemoteManifest = AutomaticSaveManifest.Create(remoteManifest.Entries),
        };
        await WriteAtomicAutomaticSyncDocumentAsync(
            local,
            AutomaticSyncBaselinePath(context),
            document,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private static async Task WritePendingAutomaticSyncAsync(
        ISaveStore local,
        AutomaticSyncPendingDocument pending,
        CancellationToken cancellationToken
    )
    {
        NormalizePending(pending);
        await WriteAtomicAutomaticSyncDocumentAsync(
            local,
            AutomaticSyncPendingPath,
            pending,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private static async Task DeletePendingAutomaticSyncAsync(
        ISaveStore local,
        CancellationToken cancellationToken
    )
    {
        await CancellableSaveStore.DeleteFileAsync(
            local,
            AutomaticSyncPendingPath,
            cancellationToken
        ).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (local.FileExists(AutomaticSyncPendingPath))
            throw new IOException("Automatic sync pending record could not be cleared");
    }

    private static async Task VerifyPendingBeforeGameSnapshotAsync(
        ISaveStore local,
        SaveContext context,
        AutomaticSyncPendingDocument pending,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(pending.BeforeGameSnapshotSha256))
        {
            if (pending.Phase == AutomaticSyncGameRunningPhase)
            {
                throw new InvalidDataException(
                    "Game-running automatic sync record has no before-game snapshot"
                );
            }
            return;
        }

        var path = string.IsNullOrWhiteSpace(pending.BeforeGameSnapshotPath)
            ? AutomaticSyncBeforeGamePath(context)
            : CloudSavePath.Relative(pending.BeforeGameSnapshotPath);
        if (!local.FileExists(path))
            throw new InvalidDataException("The immutable before-game snapshot is missing");
        var content = await CancellableSaveStore.ReadFileAsync(
            local,
            path,
            cancellationToken
        ).ConfigureAwait(false);
        var actualHash = AutomaticSyncHash.Compute(content);
        if (!string.Equals(
                actualHash,
                pending.BeforeGameSnapshotSha256,
                StringComparison.OrdinalIgnoreCase
            ))
        {
            throw new InvalidDataException(
                "The immutable before-game snapshot hash does not match its pending record"
            );
        }

        var snapshot = await ReadVerifiedSnapshotAsync(
            local,
            path,
            context,
            cancellationToken
        ).ConfigureAwait(false);
        if (!snapshot.Manifest.ContentEquals(pending.LocalBaseline))
        {
            throw new InvalidDataException(
                "The immutable before-game snapshot does not match the pending local baseline"
            );
        }
    }

    private static async Task<T?> ReadAutomaticSyncDocumentAsync<T>(
        ISaveStore local,
        string path,
        CancellationToken cancellationToken
    ) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!local.FileExists(path))
            return null;

        var content = await CancellableSaveStore.ReadFileAsync(
            local,
            path,
            cancellationToken
        ).ConfigureAwait(false);
        try
        {
            var document = JsonSerializer.Deserialize<T>(
                content,
                AutomaticSyncJsonOptions
            );
            return document ?? throw new InvalidDataException(
                $"Automatic sync document is empty: {path}"
            );
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"Automatic sync document is not valid JSON: {path}",
                ex
            );
        }
    }

    private static Task WriteAtomicAutomaticSyncDocumentAsync<T>(
        ISaveStore local,
        string path,
        T document,
        CancellationToken cancellationToken
    )
        => WriteAtomicAutomaticSyncTextAsync(
            local,
            path,
            SerializeAutomaticSyncDocument(document),
            cancellationToken
        );

    private static string SerializeAutomaticSyncDocument<T>(T document)
        => JsonSerializer.Serialize(document, AutomaticSyncJsonOptions);

    private static async Task WriteAtomicAutomaticSyncTextAsync(
        ISaveStore local,
        string path,
        string content,
        CancellationToken cancellationToken
    )
    {
        var canonical = CloudSavePath.Relative(path);
        var staging = $"{canonical}.automatic-sync-{Guid.NewGuid():N}.tmp";
        try
        {
            await CancellableSaveStore.WriteFileAsync(
                local,
                staging,
                content,
                cancellationToken
            ).ConfigureAwait(false);
            var staged = await CancellableSaveStore.ReadFileAsync(
                local,
                staging,
                cancellationToken
            ).ConfigureAwait(false);
            RequireHash(staging, AutomaticSyncHash.Compute(content), staged, "local staging");
            cancellationToken.ThrowIfCancellationRequested();
            local.RenameFile(staging, canonical);
            cancellationToken.ThrowIfCancellationRequested();
            var committed = await CancellableSaveStore.ReadFileAsync(
                local,
                canonical,
                cancellationToken
            ).ConfigureAwait(false);
            RequireHash(canonical, AutomaticSyncHash.Compute(content), committed, "local record");
        }
        finally
        {
            if (local.FileExists(staging))
                local.DeleteFile(staging);
        }
    }

    private static void NormalizePending(AutomaticSyncPendingDocument pending)
    {
        if (pending is null || pending.Version != AutomaticSyncDocumentVersion)
            throw new InvalidDataException("Automatic sync pending record has an unsupported version");
        if (pending.Phase is not (
                AutomaticSyncGameRunningPhase
                or AutomaticSyncUploadingPhase
                or AutomaticSyncDownloadingPhase
            ))
        {
            throw new InvalidDataException("Automatic sync pending record has an invalid phase");
        }
        var context = SaveContext.ParseMarker(pending.ContextMarker);
        Normalize(pending.LocalBaseline, context);
        Normalize(pending.RemoteBaseline, context);
        RequireFileState(pending.RemoteMarkerBaseline, "remote marker baseline");
        if (!string.IsNullOrWhiteSpace(pending.BeforeGameSnapshotSha256)
            && !IsSha256(pending.BeforeGameSnapshotSha256))
        {
            throw new InvalidDataException(
                "Automatic sync pending record has an invalid before-game snapshot hash"
            );
        }
        if (!string.IsNullOrWhiteSpace(pending.BeforeGameSnapshotPath))
        {
            var expectedPath = AutomaticSyncSnapshotPath(
                context,
                pending.BeforeGameSnapshotSha256
            );
            if (!string.Equals(
                    CloudSavePath.Relative(pending.BeforeGameSnapshotPath),
                    expectedPath,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                throw new InvalidDataException(
                    "Automatic sync pending record has an invalid before-game snapshot path"
                );
            }
        }
        if (pending.Phase != AutomaticSyncGameRunningPhase)
        {
            if (pending.SourceManifest is null
                || pending.ExpectedDestinationManifest is null)
            {
                throw new InvalidDataException(
                    "Automatic sync transfer intent is incomplete"
                );
            }
            Normalize(pending.SourceManifest, context);
            Normalize(pending.ExpectedDestinationManifest, context);
        }
    }

    private static void Normalize(
        AutomaticSaveManifest manifest,
        SaveContext context
    )
        => Normalize(manifest, context.Namespace);

    private static void Normalize(
        AutomaticSaveManifest manifest,
        SaveNamespace saveNamespace
    )
    {
        if (manifest is null || manifest.Version != AutomaticSyncDocumentVersion)
            throw new InvalidDataException("Automatic sync manifest has an unsupported version");
        var seen = new System.Collections.Generic.HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var entry in manifest.Entries)
        {
            var path = CloudSavePath.Relative(entry.Path);
            if (!seen.Add(path))
                throw new InvalidDataException($"Automatic sync manifest repeats path: {path}");
            if (!SaveTransferAllowlist.IsAllowedPath(saveNamespace, path))
                throw new InvalidDataException($"Automatic sync manifest path is not allowlisted: {path}");
            if (entry.Exists && !IsSha256(entry.Sha256))
                throw new InvalidDataException($"Automatic sync manifest hash is invalid: {path}");
            if (entry.Exists
                && !string.IsNullOrEmpty(entry.ByteSha256)
                && !IsSha256(entry.ByteSha256))
            {
                throw new InvalidDataException(
                    $"Automatic sync manifest byte hash is invalid: {path}"
                );
            }
            if (!entry.Exists && !string.IsNullOrEmpty(entry.Sha256))
                throw new InvalidDataException($"Missing automatic sync path has a hash: {path}");
            if (!entry.Exists && !string.IsNullOrEmpty(entry.ByteSha256))
            {
                throw new InvalidDataException(
                    $"Missing automatic sync path has a byte hash: {path}"
                );
            }
        }
        manifest.Entries = AutomaticSaveManifest.Create(manifest.Entries).Entries;
    }

    private static void RequireFileState(AutomaticFileState state, string label)
    {
        if (state.Exists && !IsSha256(state.Sha256))
            throw new InvalidDataException($"Automatic sync {label} hash is invalid");
        if (!state.Exists && !string.IsNullOrEmpty(state.Sha256))
            throw new InvalidDataException($"Missing automatic sync {label} has a hash");
    }

    private static bool IsSha256(string? value)
    {
        if (value is null || value.Length != 64)
            return false;
        foreach (var character in value)
        {
            if (!Uri.IsHexDigit(character))
                return false;
        }
        return true;
    }

    private static void RequireDocumentContext(
        string marker,
        SaveContext context,
        string label
    )
    {
        try
        {
            SaveContext.ParseMarker(marker).RequireExactMatch(context);
        }
        catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException)
        {
            throw new InvalidDataException(
                $"Automatic sync {label} belongs to another save context",
                ex
            );
        }
    }
}
