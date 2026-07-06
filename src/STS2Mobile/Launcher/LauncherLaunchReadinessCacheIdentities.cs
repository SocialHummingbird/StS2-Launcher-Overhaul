using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherLaunchReadinessCacheIdentities
{
    private const long MaxMarkerIdentityBytes = 1024 * 1024;
    private const int MaxRuntimePackPayloadSampleFiles = 128;

    private LauncherLaunchReadinessCacheIdentities(
        string pckIdentity,
        string branchMarkerIdentity,
        string releaseInfoIdentity,
        string sourceAssemblyIdentity,
        string activeAndroidAssemblyIdentity,
        string runtimeSlotMarkerIdentity,
        string runtimePackManifestIdentity,
        string runtimePackPayloadIdentity,
        string runtimeCacheMarkerIdentity,
        string runtimePatchValidationMarkerIdentity,
        string patchCompatibilityMarkerIdentity
    )
    {
        PckIdentity = pckIdentity;
        BranchMarkerIdentity = branchMarkerIdentity;
        ReleaseInfoIdentity = releaseInfoIdentity;
        SourceAssemblyIdentity = sourceAssemblyIdentity;
        ActiveAndroidAssemblyIdentity = activeAndroidAssemblyIdentity;
        RuntimeSlotMarkerIdentity = runtimeSlotMarkerIdentity;
        RuntimePackManifestIdentity = runtimePackManifestIdentity;
        RuntimePackPayloadIdentity = runtimePackPayloadIdentity;
        RuntimeCacheMarkerIdentity = runtimeCacheMarkerIdentity;
        RuntimePatchValidationMarkerIdentity = runtimePatchValidationMarkerIdentity;
        PatchCompatibilityMarkerIdentity = patchCompatibilityMarkerIdentity;
    }

    internal string PckIdentity { get; }
    internal string BranchMarkerIdentity { get; }
    internal string ReleaseInfoIdentity { get; }
    internal string SourceAssemblyIdentity { get; }
    internal string ActiveAndroidAssemblyIdentity { get; }
    internal string RuntimeSlotMarkerIdentity { get; }
    internal string RuntimePackManifestIdentity { get; }
    internal string RuntimePackPayloadIdentity { get; }
    internal string RuntimeCacheMarkerIdentity { get; }
    internal string RuntimePatchValidationMarkerIdentity { get; }
    internal string PatchCompatibilityMarkerIdentity { get; }

    internal static LauncherLaunchReadinessCacheIdentities Capture(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        var gameDirectory = LauncherGameFiles.GameDirectoryPath(dataDir, branch);
        var runtimePackDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(dataDir, branch);
        return new LauncherLaunchReadinessCacheIdentities(
            FileIdentity(LauncherGameFiles.PckPath(dataDir, branch)),
            MarkerIdentity(SteamGameInstallPaths.BranchMarkerPath(dataDir, branch)),
            MarkerIdentity(Path.Combine(gameDirectory, "release_info.json")),
            FileIdentity(GameRuntimeSlot.FindSourceAssemblyPath(gameDirectory)),
            FileIdentity(GameRuntimeSlot.FindActiveAndroidAssemblyPath(dataDir)),
            MarkerIdentity(LauncherRuntimeSlotEvidence.MarkerPath(dataDir)),
            MarkerIdentity(Path.Combine(runtimePackDirectory, "compatibility.json")),
            RuntimePackPayloadIdentityFor(runtimePackDirectory),
            MarkerIdentity(LauncherRuntimeCacheEvidence.MarkerPath(dataDir)),
            MarkerIdentity(LauncherRuntimePatchValidationEvidence.MarkerPath(dataDir)),
            MarkerIdentity(Path.Combine(gameDirectory, PatchCompatibilityEvidence.GameDirectoryMarkerFileName))
        );
    }

    internal bool MatchesCurrent(string dataDir, string branch, out string mismatchReason)
    {
        branch = SteamGameBranch.Normalize(branch);
        var gameDirectory = LauncherGameFiles.GameDirectoryPath(dataDir, branch);
        var runtimePackDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(dataDir, branch);
        return MatchesIdentity(
                "pck identity changed",
                PckIdentity,
                FileIdentity(LauncherGameFiles.PckPath(dataDir, branch)),
                out mismatchReason
            )
            && MatchesIdentity(
                "branch marker identity changed",
                BranchMarkerIdentity,
                MarkerIdentity(SteamGameInstallPaths.BranchMarkerPath(dataDir, branch)),
                out mismatchReason
            )
            && MatchesIdentity(
                "release info identity changed",
                ReleaseInfoIdentity,
                MarkerIdentity(Path.Combine(gameDirectory, "release_info.json")),
                out mismatchReason
            )
            && MatchesIdentity(
                "runtime slot marker identity changed",
                RuntimeSlotMarkerIdentity,
                MarkerIdentity(LauncherRuntimeSlotEvidence.MarkerPath(dataDir)),
                out mismatchReason
            )
            && MatchesIdentity(
                "source assembly identity changed",
                SourceAssemblyIdentity,
                FileIdentity(GameRuntimeSlot.FindSourceAssemblyPath(gameDirectory)),
                out mismatchReason
            )
            && MatchesIdentity(
                "active Android assembly identity changed",
                ActiveAndroidAssemblyIdentity,
                FileIdentity(GameRuntimeSlot.FindActiveAndroidAssemblyPath(dataDir)),
                out mismatchReason
            )
            && MatchesIdentity(
                "runtime pack manifest identity changed",
                RuntimePackManifestIdentity,
                MarkerIdentity(Path.Combine(runtimePackDirectory, "compatibility.json")),
                out mismatchReason
            )
            && MatchesIdentity(
                "runtime pack payload identity changed",
                RuntimePackPayloadIdentity,
                RuntimePackPayloadIdentityFor(runtimePackDirectory),
                out mismatchReason
            )
            && MatchesIdentity(
                "runtime cache marker identity changed",
                RuntimeCacheMarkerIdentity,
                MarkerIdentity(LauncherRuntimeCacheEvidence.MarkerPath(dataDir)),
                out mismatchReason
            )
            && MatchesIdentity(
                "runtime patch validation marker identity changed",
                RuntimePatchValidationMarkerIdentity,
                MarkerIdentity(LauncherRuntimePatchValidationEvidence.MarkerPath(dataDir)),
                out mismatchReason
            )
            && MatchesIdentity(
                "patch compatibility marker identity changed",
                PatchCompatibilityMarkerIdentity,
                MarkerIdentity(Path.Combine(gameDirectory, PatchCompatibilityEvidence.GameDirectoryMarkerFileName)),
                out mismatchReason
            );
    }

    private static bool MatchesIdentity(
        string reason,
        string expected,
        string actual,
        out string mismatchReason
    )
    {
        if (string.Equals(expected, actual, StringComparison.Ordinal))
        {
            mismatchReason = string.Empty;
            return true;
        }

        mismatchReason = reason;
        return false;
    }

    private static string FileIdentity(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "<missing-path>";

        try
        {
            if (!File.Exists(path))
                return $"missing:{path}";

            var info = new FileInfo(path);
            var mtime = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeMilliseconds();
            return $"file:{path};bytes={info.Length};mtime={mtime}";
        }
        catch (Exception ex)
        {
            return $"unavailable:{path};{ex.GetType().Name}";
        }
    }

    private static string MarkerIdentity(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "<missing-path>";

        try
        {
            if (!File.Exists(path))
                return $"missing:{path}";

            var info = new FileInfo(path);
            var mtime = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeMilliseconds();
            if (info.Length > MaxMarkerIdentityBytes)
                return $"oversized-marker:{path};bytes={info.Length};mtime={mtime}";

            using var stream = File.OpenRead(path);
            var hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            return $"marker:{path};bytes={info.Length};mtime={mtime};sha256={hash}";
        }
        catch (Exception ex)
        {
            return $"unavailable:{path};{ex.GetType().Name}";
        }
    }

    private static string RuntimePackPayloadIdentityFor(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return "<missing-path>";

        try
        {
            if (!Directory.Exists(directory))
                return $"missing:{directory}";

            var files = Directory
                .EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                .Where(IsRuntimePackPayloadFile)
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);

            long totalBytes = 0;
            long newestMtime = 0;
            var fileCount = 0;
            var sample = new List<string>(MaxRuntimePackPayloadSampleFiles);
            using var sha = SHA256.Create();

            foreach (var file in files)
            {
                var identity = CaptureRuntimePackPayloadFileIdentity(file);
                fileCount++;
                AddHashText(sha, identity.Text);
                if (identity.Exists)
                {
                    totalBytes += identity.Bytes;
                    newestMtime = Math.Max(newestMtime, identity.Mtime);
                }

                if (sample.Count < MaxRuntimePackPayloadSampleFiles)
                    sample.Add(identity.Text);
            }

            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var digest = Convert.ToHexString(sha.Hash ?? Array.Empty<byte>()).ToLowerInvariant();
            var truncated = fileCount > sample.Count ? "true" : "false";
            return $"runtime-pack-payload:{directory};count={fileCount};bytes={totalBytes};newestMtime={newestMtime};payloadSha256={digest};sampleCount={sample.Count};truncated={truncated};{string.Join("|", sample)}";
        }
        catch (Exception ex)
        {
            return $"unavailable:{directory};{ex.GetType().Name}";
        }
    }

    private static RuntimePackPayloadFileIdentity CaptureRuntimePackPayloadFileIdentity(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new("<missing-path>", exists: false, bytes: 0, mtime: 0);

        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
                return new($"missing:{path}", exists: false, bytes: 0, mtime: 0);

            var mtime = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeMilliseconds();
            var text = string.Equals(
                    Path.GetExtension(path),
                    ".json",
                    StringComparison.OrdinalIgnoreCase
                )
                ? MarkerIdentity(path)
                : $"file:{path};bytes={info.Length};mtime={mtime}";
            return new(text, exists: true, bytes: info.Length, mtime);
        }
        catch (Exception ex)
        {
            return new($"unavailable:{path};{ex.GetType().Name}", exists: false, bytes: 0, mtime: 0);
        }
    }

    private static bool IsRuntimePackPayloadFile(string path)
    {
        if (string.Equals(
                Path.GetFileName(path),
                "compatibility.json",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase);
    }

    private readonly struct RuntimePackPayloadFileIdentity
    {
        internal RuntimePackPayloadFileIdentity(
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
    }

    private static void AddHashText(HashAlgorithm hash, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        hash.TransformBlock(bytes, 0, bytes.Length, null, 0);
        var separator = new[] { (byte)'\n' };
        hash.TransformBlock(separator, 0, separator.Length, null, 0);
    }
}
