using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherLaunchReadinessCacheIdentities
{
    private const long MaxManifestBytes = 1024 * 1024;

    private LauncherLaunchReadinessCacheIdentities(
        GameIdentity gameIdentity,
        string runtimePackManifestIdentity,
        string runtimePackPayloadIdentity
    )
    {
        GameIdentity = gameIdentity;
        RuntimePackManifestIdentity = runtimePackManifestIdentity;
        RuntimePackPayloadIdentity = runtimePackPayloadIdentity;
    }

    internal GameIdentity GameIdentity { get; }
    internal string RuntimePackManifestIdentity { get; }
    internal string RuntimePackPayloadIdentity { get; }

    internal static LauncherLaunchReadinessCacheIdentities Capture(
        string dataDir,
        string branch,
        GameIdentity gameIdentity = null
    )
    {
        branch = SteamGameBranch.StorageIdentity(branch);
        gameIdentity ??= GameIdentityReader.ReadInstalled(dataDir, branch);
        if (gameIdentity.Branch != branch)
            throw new ArgumentException("Cached game identity belongs to a different branch.", nameof(gameIdentity));

        var runtimePackDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(dataDir, branch);
        return new LauncherLaunchReadinessCacheIdentities(
            gameIdentity,
            ManifestIdentity(Path.Combine(runtimePackDirectory, "compatibility.json")),
            RuntimePackPayloadIdentityFor(runtimePackDirectory)
        );
    }

    internal bool MatchesCurrent(string dataDir, string branch, out string mismatchReason)
    {
        branch = SteamGameBranch.StorageIdentity(branch);
        GameIdentity currentIdentity;
        try
        {
            currentIdentity = GameIdentityReader.ReadInstalled(dataDir, branch);
        }
        catch (GameIdentityException ex)
        {
            mismatchReason = $"current game identity unavailable ({ex.Kind}): {ex.Message}";
            return false;
        }

        if (GameIdentity != currentIdentity)
        {
            mismatchReason = "authoritative game identity changed";
            return false;
        }

        if (!BranchInstallStateStore.Current.TryReadReady(
                dataDir,
                branch,
                currentIdentity,
                out _,
                out var installStateProblem
            ))
        {
            mismatchReason = $"installation state is not ready: {installStateProblem}";
            return false;
        }

        var runtimePackDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(dataDir, branch);
        if (!Matches(
                RuntimePackManifestIdentity,
                ManifestIdentity(Path.Combine(runtimePackDirectory, "compatibility.json"))))
        {
            mismatchReason = "runtime pack manifest changed";
            return false;
        }

        if (!Matches(RuntimePackPayloadIdentity, RuntimePackPayloadIdentityFor(runtimePackDirectory)))
        {
            mismatchReason = "runtime pack payload changed";
            return false;
        }

        mismatchReason = string.Empty;
        return true;
    }

    private static bool Matches(string expected, string actual)
        => string.Equals(expected, actual, StringComparison.Ordinal);

    private static string ManifestIdentity(string path)
    {
        try
        {
            if (!File.Exists(path))
                return $"missing:{path}";

            var info = new FileInfo(path);
            if (info.Length > MaxManifestBytes)
                return $"oversized:{path};bytes={info.Length}";

            var hash = GameIdentityFileHasher.Instance.Sha256(path);
            return $"manifest:{path};bytes={info.Length};sha256={hash}";
        }
        catch (Exception ex)
        {
            return $"unavailable:{path};{ex.GetType().Name}";
        }
    }

    private static string RuntimePackPayloadIdentityFor(string directory)
    {
        try
        {
            if (!Directory.Exists(directory))
                return $"missing:{directory}";

            var files = Directory
                .EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                .Where(path => !string.Equals(
                    Path.GetFileName(path),
                    "compatibility.json",
                    StringComparison.OrdinalIgnoreCase
                ))
                .Where(path =>
                    string.Equals(Path.GetExtension(path), ".dll", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);

            var canonical = new StringBuilder();
            var count = 0;
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                var text = string.Equals(Path.GetExtension(file), ".json", StringComparison.OrdinalIgnoreCase)
                    ? ManifestIdentity(file)
                    : $"file:{file};bytes={info.Length};mtime={info.LastWriteTimeUtc.Ticks}";
                canonical.Append(text).Append('\n');
                count++;
            }

            var digest = Convert.ToHexString(
                AndroidJavaCrypto.Sha256HashData(
                    Encoding.UTF8.GetBytes(canonical.ToString())
                )
            ).ToLowerInvariant();
            return $"runtime-pack-payload:{directory};count={count};sha256={digest}";
        }
        catch (Exception ex)
        {
            return $"unavailable:{directory};{ex.GetType().Name}";
        }
    }

}
