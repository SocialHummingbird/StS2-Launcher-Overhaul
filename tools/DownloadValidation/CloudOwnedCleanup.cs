using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2.Internal;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed record CloudPreservedFile(string Path, long Size, string Sha256, string MetadataHash, long ModifiedAt);

// Validation-only cleanup. It deliberately does not change production deletion policy.
internal static class CloudOwnedCleanup
{
    internal static async Task<bool> DeleteAsync(ICloudMessageClient client, IReadOnlyCollection<string> owned, string path, CancellationToken cancellationToken)
    {
        if (!owned.Contains(path, StringComparer.Ordinal)) throw new IOException("Cloud cleanup escaped its exact owned-file ledger.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(3));
        client.BeginOperation();
        try
        {
            var prior = await InspectAsync(client, path, deadline.Token).ConfigureAwait(false);
            if (prior.Absent) return prior.Tombstone;
            try
            {
                await client.SendCloudAsync<CCloud_ClientDeleteFile_Request, CCloud_ClientDeleteFile_Response>("ClientDeleteFile",
                    new() { appid = SteamGameApp.AppId, filename = path, is_explicit_delete = true }, deadline.Token).ConfigureAwait(false);
            }
            catch (InvalidOperationException error) when (error.Message == "Cloud.ClientDeleteFile failed: DuplicateRequest")
            {
                // This may be a repeated delete, but it is not success until the
                // independent metadata query confirms the exact owned path absent.
            }
            deadline.Token.ThrowIfCancellationRequested();
            var after = await InspectAsync(client, path, deadline.Token).ConfigureAwait(false);
            if (!after.Absent) throw new InvalidDataException("Owned Cloud file remains present after deletion.");
            return after.Tombstone;
        }
        finally { client.EndOperation(); }
    }

    private static async Task<(bool Absent, bool Tombstone)> InspectAsync(ICloudMessageClient client, string path, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            var metadata = await client.SendCloudAsync<CCloud_ClientFileDownload_Request, CCloud_ClientFileDownload_Response>("ClientFileDownload",
                new() { appid = SteamGameApp.AppId, filename = path }, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (metadata.appid != SteamGameApp.AppId) throw new InvalidDataException("Owned Cloud metadata belongs to the wrong app.");
            return (metadata.is_explicit_delete, metadata.is_explicit_delete);
        }
        catch (InvalidOperationException error) when (error.Message == "Cloud.ClientFileDownload failed: FileNotFound")
        {
            token.ThrowIfCancellationRequested();
            return (true, false);
        }
    }

    internal static bool CanReuseSnapshot(IReadOnlyList<CloudPreservedFile> baseline, IReadOnlyList<CloudPreservedFile> cached,
        IReadOnlyList<SteamCloudTransport.RemoteFile> current, IReadOnlyCollection<string> owned)
    {
        if (!baseline.SequenceEqual(cached)) return false;
        var files = current.Where(file => !owned.Contains(file.Path, StringComparer.Ordinal)).OrderBy(file => file.Path, StringComparer.Ordinal).ToArray();
        return files.Length == baseline.Count && baseline.Zip(files, (saved, remote) =>
            saved.Path == remote.Path && saved.Size == remote.Size && saved.MetadataHash == remote.ContentHash
            && saved.ModifiedAt == remote.ModifiedAt.ToUnixTimeSeconds() && HasUsableHash(remote.ContentHash)).All(match => match);
    }

    private static bool HasUsableHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) return false;
        if (hash.Length == 40 && hash.All(Uri.IsHexDigit)) return true;
        try { return Convert.FromBase64String(hash).Length == 20; }
        catch (FormatException) { return false; }
    }
}
