using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2.Internal;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void CloudOwnedCleanupVerifiesAbsenceAfterAcknowledgement()
    {
        var owned = new[] { "sts2-launcher-validation/test/owned.bin" };
        using var absent = new CleanupClient("absent");
        True(!CloudOwnedCleanup.DeleteAsync(absent, owned, owned[0], default).GetAwaiter().GetResult(), "FileNotFound after an acknowledged delete verifies absence, without claiming a tombstone.");
        Equal(3, absent.Calls, "Cleanup checks presence, acknowledges deletion, then verifies absence.");
        Equal(0, absent.Operations, "Cleanup operation ownership must balance.");
        using var tombstone = new CleanupClient("tombstone");
        True(CloudOwnedCleanup.DeleteAsync(tombstone, owned, owned[0], default).GetAwaiter().GetResult(), "An explicit valid tombstone is also accepted.");
        using var alreadyAbsent = new CleanupClient("already-absent");
        True(!CloudOwnedCleanup.DeleteAsync(alreadyAbsent, owned, owned[0], default).GetAwaiter().GetResult(), "An already absent owned file must not require another delete.");
        Equal(1, alreadyAbsent.Calls, "An absent file is established by one metadata query.");
        using var duplicateDelete = new CleanupClient("duplicate-delete");
        True(!CloudOwnedCleanup.DeleteAsync(duplicateDelete, owned, owned[0], default).GetAwaiter().GetResult(), "An idempotent delete response requires subsequent independent absence verification.");
        foreach (var mode in new[] { "present", "wrong-app", "delete-fails", "access-denied", "duplicate-present" })
        {
            using var failed = new CleanupClient(mode);
            Throws<Exception>(() => CloudOwnedCleanup.DeleteAsync(failed, owned, owned[0], default).GetAwaiter().GetResult());
            Equal(0, failed.Operations, "Failure must release cleanup operation ownership.");
            if (mode == "delete-fails") Equal(2, failed.Calls, "A failed delete cannot be interpreted as verified absence.");
        }
        using var outside = new CleanupClient("absent");
        Throws<IOException>(() => CloudOwnedCleanup.DeleteAsync(outside, owned, "settings.save", default).GetAwaiter().GetResult());
        Equal(0, outside.Calls, "An unowned path must be rejected before any network request.");
    }

    private static void CloudPreservationCheckpointRequiresMatchingLiveMetadata()
    {
        var hash = new string('A', 40);
        var baseline = new[] { new CloudPreservedFile("settings.save", 7, new string('B', 64), hash, 10) };
        var current = new[] { new SteamCloudTransport.RemoteFile("settings.save", 7, DateTimeOffset.FromUnixTimeSeconds(10), hash) };
        True(CloudOwnedCleanup.CanReuseSnapshot(baseline, baseline.ToArray(), current, Array.Empty<string>()), "A completed full-byte comparison can be resumed only with identical live metadata.");
        True(!CloudOwnedCleanup.CanReuseSnapshot(baseline, new[] { baseline[0] with { Sha256 = new string('C', 64) } }, current, Array.Empty<string>()), "Changed cached byte hashes cannot be reused.");
        foreach (var changed in new[] { current[0] with { Size = 8 }, current[0] with { ContentHash = new string('C', 40) }, current[0] with { ModifiedAt = DateTimeOffset.FromUnixTimeSeconds(11) }, current[0] with { Path = "other.save" } })
            True(!CloudOwnedCleanup.CanReuseSnapshot(baseline, baseline, new[] { changed }, Array.Empty<string>()), "Live metadata changes require a fresh byte comparison.");
        True(!CloudOwnedCleanup.CanReuseSnapshot(baseline, baseline, Array.Empty<SteamCloudTransport.RemoteFile>(), Array.Empty<string>()), "Missing files cannot reuse a snapshot.");
        foreach (var invalid in new[] { "", "invalid", new string('X', 40), Convert.ToBase64String(new byte[3]) })
        {
            var invalidBaseline = new[] { baseline[0] with { MetadataHash = invalid } };
            True(!CloudOwnedCleanup.CanReuseSnapshot(invalidBaseline, invalidBaseline, new[] { current[0] with { ContentHash = invalid } }, Array.Empty<string>()), "Matching unusable hashes cannot authorize checkpoint reuse.");
        }
        var plusOwned = current.Append(new SteamCloudTransport.RemoteFile("owned.bin", 1, DateTimeOffset.UtcNow, "")).ToArray();
        True(CloudOwnedCleanup.CanReuseSnapshot(baseline, baseline, plusOwned, new[] { "owned.bin" }), "Only ledger-owned test entries are excluded from preservation comparison.");
        True(!CloudOwnedCleanup.CanReuseSnapshot(baseline, baseline, plusOwned, Array.Empty<string>()), "Unrelated extra files cannot be excluded.");
    }

    private sealed class CleanupClient(string mode) : ICloudMessageClient
    {
        internal int Calls, Operations;
        private bool _deleteAttempted;
        public void BeginOperation() => Operations++;
        public void EndOperation() => Operations--;
        public void Dispose() { }
        public Task<TResult> SendCloudAsync<TRequest, TResult>(string method, TRequest request, CancellationToken token)
            where TRequest : ProtoBuf.IExtensible, new() where TResult : ProtoBuf.IExtensible, new()
        {
            Calls++; token.ThrowIfCancellationRequested();
            if (request is CCloud_ClientDeleteFile_Request)
            {
                _deleteAttempted = true;
                if (mode == "delete-fails") throw new InvalidOperationException("Cloud.ClientDeleteFile failed: FileNotFound");
                if (mode is "duplicate-delete" or "duplicate-present") throw new InvalidOperationException("Cloud.ClientDeleteFile failed: DuplicateRequest");
                return Task.FromResult((TResult)(object)new CCloud_ClientDeleteFile_Response());
            }
            if (mode == "already-absent" || (_deleteAttempted && mode is "absent" or "duplicate-delete")) throw new InvalidOperationException("Cloud.ClientFileDownload failed: FileNotFound");
            if (mode == "access-denied") throw new InvalidOperationException("Cloud.ClientFileDownload failed: AccessDenied");
            return Task.FromResult((TResult)(object)new CCloud_ClientFileDownload_Response { appid = mode == "wrong-app" ? 0 : SteamGameApp.AppId, is_explicit_delete = _deleteAttempted && mode == "tombstone" });
        }
    }
}
