using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using STS2Mobile;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed class LauncherModSourceIdentity
{
    private const long MaxMetadataIdentityBytes = 1024 * 1024;
    private const int MaxDirectoryIdentitySampleFiles = 256;
    private static readonly string[] LaunchRelevantPatterns = { "*.json", "*.pck", "*.dll" };

    private LauncherModSourceIdentity(
        string selectionIdentity,
        string workshopManifestIdentity,
        string workshopStagedIdentity,
        string externalModsIdentity
    )
    {
        SelectionIdentity = selectionIdentity;
        WorkshopManifestIdentity = workshopManifestIdentity;
        WorkshopStagedIdentity = workshopStagedIdentity;
        ExternalModsIdentity = externalModsIdentity;
    }

    internal string SelectionIdentity { get; }
    private string WorkshopManifestIdentity { get; }
    private string WorkshopStagedIdentity { get; }
    private string ExternalModsIdentity { get; }

    internal static LauncherModSourceIdentity Create()
        => new(
            MetadataFileIdentity(AppPaths.AppPrivateModSelectionPath),
            MetadataFileIdentity(AppPaths.AppPrivateWorkshopManifestPath),
            LaunchRelevantDirectoryIdentity(AppPaths.AppPrivateWorkshopStagedModsDir),
            LaunchRelevantDirectoryIdentity(AppPaths.ExternalModsDir)
        );

    internal bool Matches(LauncherModSourceIdentity other)
        => other != null
            && string.Equals(SelectionIdentity, other.SelectionIdentity, StringComparison.Ordinal)
            && string.Equals(WorkshopManifestIdentity, other.WorkshopManifestIdentity, StringComparison.Ordinal)
            && string.Equals(WorkshopStagedIdentity, other.WorkshopStagedIdentity, StringComparison.Ordinal)
            && string.Equals(ExternalModsIdentity, other.ExternalModsIdentity, StringComparison.Ordinal);

    private static string MetadataFileIdentity(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "<missing-path>";

        try
        {
            if (!File.Exists(path))
                return $"missing:{path}";

            var info = new FileInfo(path);
            var mtime = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeMilliseconds();
            if (info.Length > MaxMetadataIdentityBytes)
                return $"oversized-metadata:{path};bytes={info.Length};mtime={mtime}";

            byte[] hash;
            if (OperatingSystem.IsAndroid())
            {
                hash = AndroidJavaCrypto.Sha256FileHashData(path);
            }
            else
            {
                using var stream = File.OpenRead(path);
                hash = SHA256.HashData(stream);
            }

            var hashText = Convert.ToHexString(hash).ToLowerInvariant();
            return $"metadata:{path};bytes={info.Length};mtime={mtime};sha256={hashText}";
        }
        catch (Exception ex)
        {
            return $"unavailable:{path};{ex.GetType().Name}";
        }
    }

    private static string LaunchRelevantDirectoryIdentity(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "<missing-path>";

        try
        {
            if (!Directory.Exists(path))
                return $"missing:{path}";

            var files = Directory
                .EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Where(IsLaunchRelevantFile)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase);

            long totalBytes = 0;
            long newestMtime = 0;
            var fileCount = 0;
            var sample = new List<string>(MaxDirectoryIdentitySampleFiles);
            using var metadata = new MemoryStream();
            var metadataTruncated = false;

            foreach (var file in files)
            {
                var identity = LaunchRelevantFileIdentity.Capture(file);
                fileCount++;
                AddHashText(metadata, identity.Text, ref metadataTruncated);
                if (identity.Exists)
                {
                    totalBytes += identity.Bytes;
                    newestMtime = Math.Max(newestMtime, identity.Mtime);
                }

                if (sample.Count < MaxDirectoryIdentitySampleFiles)
                    sample.Add(identity.Text);
            }

            if (!metadata.TryGetBuffer(out var metadataBuffer))
                throw new InvalidOperationException("Mod metadata identity buffer is unavailable.");

            var digest = Convert.ToHexString(
                AndroidJavaCrypto.Sha256HashData(
                    metadataBuffer.AsSpan(0, checked((int)metadata.Length))
                )
            ).ToLowerInvariant();
            var truncated = fileCount > sample.Count ? "true" : "false";
            return $"dir-launch-relevant:{path};count={fileCount};bytes={totalBytes};newestMtime={newestMtime};metadataSha256={digest};metadataBytes={metadata.Length};metadataTruncated={metadataTruncated.ToString().ToLowerInvariant()};sampleCount={sample.Count};truncated={truncated};patterns={string.Join(",", LaunchRelevantPatterns)};{string.Join("|", sample)}";
        }
        catch (Exception ex)
        {
            return $"unavailable:{path};{ex.GetType().Name}";
        }
    }

    private static bool IsLaunchRelevantFile(string path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".pck", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase);
    }

    private readonly struct LaunchRelevantFileIdentity
    {
        private LaunchRelevantFileIdentity(
            string text,
            bool exists,
            long bytes,
            long mtime
        )
        {
            Text = text;
            Exists = exists;
            Bytes = bytes;
            Mtime = mtime;
        }

        internal string Text { get; }
        internal bool Exists { get; }
        internal long Bytes { get; }
        internal long Mtime { get; }

        internal static LaunchRelevantFileIdentity Capture(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return new("<missing-path>", exists: false, bytes: 0, mtime: 0);

            try
            {
                var info = new FileInfo(path);
                if (!info.Exists)
                    return new($"missing:{path}", exists: false, bytes: 0, mtime: 0);

                var mtime = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeMilliseconds();
                return new(
                    $"file:{path};bytes={info.Length};mtime={mtime}",
                    exists: true,
                    bytes: info.Length,
                    mtime: mtime
                );
            }
            catch (Exception ex)
            {
                return new($"unavailable:{path};{ex.GetType().Name}", exists: false, bytes: 0, mtime: 0);
            }
        }
    }

    private static void AddHashText(MemoryStream metadata, string value, ref bool truncated)
    {
        if (truncated)
            return;

        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        var remaining = MaxMetadataIdentityBytes - metadata.Length;
        if (remaining <= 0)
        {
            truncated = true;
            return;
        }

        var bytesToWrite = (int)Math.Min(bytes.Length, remaining);
        metadata.Write(bytes, 0, bytesToWrite);
        if (bytesToWrite != bytes.Length || metadata.Length >= MaxMetadataIdentityBytes)
        {
            truncated = true;
            return;
        }

        metadata.WriteByte((byte)'\n');
    }
}
