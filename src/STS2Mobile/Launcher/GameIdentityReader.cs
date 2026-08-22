using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal interface IGameIdentityFileHasher
{
    string Sha256(string path);
}

internal sealed class GameIdentityFileHasher : IGameIdentityFileHasher
{
    internal static readonly GameIdentityFileHasher Instance = new();

    private GameIdentityFileHasher()
    {
    }

    public string Sha256(string path)
    {
        byte[] hash;
        if (OperatingSystem.IsAndroid())
        {
            hash = AndroidJavaCrypto.Sha256FileHashData(path);
        }
        else
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                1024 * 1024,
                FileOptions.SequentialScan
            );
            hash = SHA256.HashData(stream);
        }

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

internal readonly record struct GameIdentityFileSnapshot(
    string NormalizedPath,
    long Length,
    long LastWriteTimeUtcTicks
)
{
    internal static GameIdentityFileSnapshot Capture(
        string path,
        string description
    )
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new GameIdentityException(
                GameIdentityFailureKind.MissingFile,
                $"Cannot calculate game identity because {description} is missing: {path ?? "<empty>"}.",
                path
            );
        }

        try
        {
            var info = new FileInfo(path);
            info.Refresh();
            if (!info.Exists)
            {
                throw new GameIdentityException(
                    GameIdentityFailureKind.MissingFile,
                    $"Cannot calculate game identity because {description} disappeared: {path}.",
                    path
                );
            }

            return new GameIdentityFileSnapshot(
                GameIdentityPckCache.NormalizePath(info.FullName),
                info.Length,
                info.LastWriteTimeUtc.Ticks
            );
        }
        catch (GameIdentityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new GameIdentityException(
                GameIdentityFailureKind.UnreadableFile,
                $"Cannot inspect {description} while calculating game identity: {path} ({ex.GetType().Name}: {ex.Message}).",
                path,
                ex
            );
        }
    }
}

internal sealed class GameIdentityReader
{
    private const uint PckMagic = 0x43504447;
    private const long MinimumPckHeaderBytes = 96;
    private readonly IGameIdentityFileHasher _fileHasher;

    internal GameIdentityReader(IGameIdentityFileHasher fileHasher)
    {
        _fileHasher = fileHasher ?? throw new ArgumentNullException(nameof(fileHasher));
    }

    internal static GameIdentity ReadInstalled(string dataDir, string branch)
        => new GameIdentityReader(GameIdentityFileHasher.Instance).Read(dataDir, branch);

    internal GameIdentity Read(string dataDir, string branch)
    {
        if (string.IsNullOrWhiteSpace(dataDir))
            throw new ArgumentException("A launcher data directory is required.", nameof(dataDir));

        var normalizedBranch = SteamGameBranch.StorageIdentity(branch);
        var gameDirectory = SteamGameInstallPaths.GameDirectory(dataDir, normalizedBranch);
        var pckPath = Path.Combine(gameDirectory, LauncherStorageNames.GamePck);
        var sourceAssemblyPath = ResolveInstalledSourceAssemblyPath(gameDirectory);
        var markerPath = SteamGameInstallPaths.BranchMarkerPath(dataDir, normalizedBranch);

        var generationBefore = ReadInstallGeneration(markerPath, normalizedBranch);
        var pckBefore = GameIdentityFileSnapshot.Capture(pckPath, "selected PCK");
        var sourceBefore = GameIdentityFileSnapshot.Capture(sourceAssemblyPath, "selected source sts2.dll");
        RequireCompletedInstall(generationBefore.Snapshot, pckBefore, sourceBefore, markerPath);
        ValidatePckStructure(pckPath);

        var pckCacheHit = GameIdentityPckCache.TryRead(
            dataDir,
            normalizedBranch,
            generationBefore.Generation,
            pckBefore,
            out var pckSha256
        );
        if (!pckCacheHit)
            pckSha256 = HashCurrentFile(pckPath, "selected PCK");
        RequireUnchanged(pckBefore, pckPath, "selected PCK");

        // sts2.dll is deliberately always hashed. It is small enough that a
        // persistent digest cache only creates another invalidation problem.
        var sourceAssemblySha256 = HashCurrentFile(
            sourceAssemblyPath,
            "selected source sts2.dll"
        );
        RequireUnchanged(sourceBefore, sourceAssemblyPath, "selected source sts2.dll");

        var generationAfter = ReadInstallGeneration(markerPath, normalizedBranch);
        RequireUnchanged(pckBefore, pckPath, "selected PCK");
        RequireUnchanged(sourceBefore, sourceAssemblyPath, "selected source sts2.dll");
        if (generationBefore != generationAfter)
        {
            throw new GameIdentityException(
                GameIdentityFailureKind.FileChanged,
                $"Cannot calculate game identity because the completed install generation changed while files were being hashed: {markerPath}.",
                markerPath
            );
        }
        RequireCompletedInstall(generationAfter.Snapshot, pckBefore, sourceBefore, markerPath);

        var identity = new GameIdentity(
            normalizedBranch,
            generationAfter.Generation,
            pckSha256,
            sourceAssemblySha256
        );
        if (!pckCacheHit)
            GameIdentityPckCache.TryWrite(dataDir, identity, pckBefore);

        return identity;
    }

    private string HashCurrentFile(string path, string description)
    {
        try
        {
            var hash = _fileHasher.Sha256(path);
            if (!GameIdentity.IsSha256(hash))
            {
                throw new GameIdentityException(
                    GameIdentityFailureKind.UnreadableFile,
                    $"Cannot calculate game identity because hashing {description} returned an invalid SHA-256 value: {path}.",
                    path
                );
            }

            return hash.ToLowerInvariant();
        }
        catch (GameIdentityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new GameIdentityException(
                GameIdentityFailureKind.UnreadableFile,
                $"Cannot read {description} while calculating game identity: {path} ({ex.GetType().Name}: {ex.Message}).",
                path,
                ex
            );
        }
    }

    internal static string ResolveInstalledSourceAssemblyPath(string gameDirectory)
    {
        if (!Directory.Exists(gameDirectory))
        {
            throw new GameIdentityException(
                GameIdentityFailureKind.MissingFile,
                $"Cannot calculate game identity because the selected game directory is missing: {gameDirectory}.",
                gameDirectory
            );
        }

        List<string> candidates;
        try
        {
            candidates = Directory
                .EnumerateDirectories(gameDirectory, "data_*", SearchOption.TopDirectoryOnly)
                .Select(directory => Path.Combine(directory, "sts2.dll"))
                .Where(File.Exists)
                .Select(Path.GetFullPath)
                .Distinct(PathComparer())
                .OrderBy(path => path, PathComparer())
                .ToList();
        }
        catch (Exception ex)
        {
            throw new GameIdentityException(
                GameIdentityFailureKind.UnreadableFile,
                $"Cannot enumerate the selected game assembly directory while calculating game identity: {gameDirectory} ({ex.GetType().Name}: {ex.Message}).",
                gameDirectory,
                ex
            );
        }

        if (candidates.Count == 0)
        {
            var expected = Path.Combine(
                gameDirectory,
                "data_sts2_windows_x86_64",
                "sts2.dll"
            );
            throw new GameIdentityException(
                GameIdentityFailureKind.MissingFile,
                $"Cannot calculate game identity because selected source sts2.dll is missing: {expected}.",
                expected
            );
        }

        if (candidates.Count > 1)
        {
            throw new GameIdentityException(
                GameIdentityFailureKind.AmbiguousSourceAssembly,
                "Cannot calculate game identity because multiple selected source sts2.dll files exist: "
                    + string.Join("; ", candidates),
                gameDirectory
            );
        }

        return candidates[0];
    }

    private static InstallGenerationRead ReadInstallGeneration(
        string markerPath,
        string normalizedBranch
    )
    {
        var before = GameIdentityFileSnapshot.Capture(
            markerPath,
            "completed Steam branch marker"
        );

        byte[] bytes;
        try
        {
            using var stream = new FileStream(
                markerPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024,
                FileOptions.SequentialScan
            );
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            bytes = memory.ToArray();
        }
        catch (Exception ex)
        {
            throw new GameIdentityException(
                GameIdentityFailureKind.UnreadableFile,
                $"Cannot read the completed Steam branch marker while calculating game identity: {markerPath} ({ex.GetType().Name}: {ex.Message}).",
                markerPath,
                ex
            );
        }

        RequireUnchanged(before, markerPath, "completed Steam branch marker");
        // File.WriteAllText and older launcher builds may emit a UTF-8 BOM.  It
        // is transport metadata, not part of the first marker field.
        var text = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var markerBranch = lines
            .FirstOrDefault(line => line.StartsWith(LauncherBranchMarkerFields.Branch, StringComparison.OrdinalIgnoreCase))?
            .Substring(LauncherBranchMarkerFields.Branch.Length)
            .Trim();
        var depotRows = lines.Count(line => line.StartsWith(
            LauncherBranchMarkerFields.DepotManifestRow,
            StringComparison.OrdinalIgnoreCase
        ));
        var declaredDepotCountText = lines
            .FirstOrDefault(line => line.StartsWith(
                LauncherBranchMarkerFields.DepotManifestCount,
                StringComparison.OrdinalIgnoreCase
            ))?
            .Substring(LauncherBranchMarkerFields.DepotManifestCount.Length)
            .Trim();
        var hasCompleteDepotList = int.TryParse(
            declaredDepotCountText,
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture,
            out var declaredDepotCount
        ) && declaredDepotCount > 0 && declaredDepotCount == depotRows;
        if (string.IsNullOrWhiteSpace(markerBranch)
            || !string.Equals(
                SteamGameBranch.StorageIdentity(markerBranch),
                normalizedBranch,
                StringComparison.Ordinal
            )
            || !hasCompleteDepotList)
        {
            throw new GameIdentityException(
                GameIdentityFailureKind.InvalidInstallGeneration,
                $"Cannot calculate game identity because the completed Steam branch marker is missing matching branch/depot provenance for '{normalizedBranch}': {markerPath}.",
                markerPath
            );
        }

        return new InstallGenerationRead(
            Convert.ToHexString(AndroidJavaCrypto.Sha256HashData(bytes)).ToLowerInvariant(),
            before
        );
    }

    private static void RequireCompletedInstall(
        GameIdentityFileSnapshot marker,
        GameIdentityFileSnapshot pck,
        GameIdentityFileSnapshot sourceAssembly,
        string markerPath
    )
    {
        if (marker.LastWriteTimeUtcTicks >= pck.LastWriteTimeUtcTicks
            && marker.LastWriteTimeUtcTicks >= sourceAssembly.LastWriteTimeUtcTicks)
        {
            return;
        }

        throw new GameIdentityException(
            GameIdentityFailureKind.IncompleteInstall,
            "Cannot calculate game identity because selected game files are newer than the completed install marker. "
                + $"The branch may be partially replaced: marker={markerPath}, pck={pck.NormalizedPath}, sourceAssembly={sourceAssembly.NormalizedPath}.",
            markerPath
        );
    }

    private static void RequireUnchanged(
        GameIdentityFileSnapshot before,
        string path,
        string description
    )
    {
        var after = GameIdentityFileSnapshot.Capture(path, description);
        if (before == after)
            return;

        throw new GameIdentityException(
            GameIdentityFailureKind.FileChanged,
            $"Cannot calculate game identity because {description} changed while it was being inspected: {path}.",
            path
        );
    }

    private static void ValidatePckStructure(string path)
    {
        try
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024,
                FileOptions.SequentialScan
            );
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            if (stream.Length < MinimumPckHeaderBytes || reader.ReadUInt32() != PckMagic)
                throw InvalidPck(path, "header or magic is invalid");

            reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadInt64();
            var directoryOffset = reader.ReadInt64();
            if (directoryOffset <= 0 || directoryOffset + 4 > stream.Length)
                throw InvalidPck(path, $"directory offset {directoryOffset} is outside the file");

            stream.Position = directoryOffset;
            if (reader.ReadUInt32() == 0)
                throw InvalidPck(path, "directory contains no files");
        }
        catch (GameIdentityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new GameIdentityException(
                GameIdentityFailureKind.UnreadableFile,
                $"Cannot read selected PCK structure while calculating game identity: {path} ({ex.GetType().Name}: {ex.Message}).",
                path,
                ex
            );
        }
    }

    private static GameIdentityException InvalidPck(string path, string detail)
        => new(
            GameIdentityFailureKind.InvalidPck,
            $"Cannot calculate game identity because the selected PCK {detail}: {path}.",
            path
        );

    private static StringComparer PathComparer()
        => OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private readonly record struct InstallGenerationRead(
        string Generation,
        GameIdentityFileSnapshot Snapshot
    );
}
