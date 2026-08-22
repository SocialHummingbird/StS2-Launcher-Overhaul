using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class RuntimePackWriter
{
    private static readonly string[] RuntimeSupportAssemblyFileNames =
    {
    };

    private static string CreateStagingDirectory(
        string finalPackDirectory,
        Guid installTransactionId,
        Guid attemptId
    )
    {
        var parent = Path.GetDirectoryName(finalPackDirectory);
        if (string.IsNullOrWhiteSpace(parent))
            throw new IOException("Cannot resolve the runtime-pack parent directory.");

        Directory.CreateDirectory(parent);
        var stagingDirectory = Path.GetFullPath(
            $"{finalPackDirectory}.staging.{installTransactionId:N}.{attemptId:N}"
        );
        var parentPrefix = Path.GetFullPath(parent)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!stagingDirectory.StartsWith(parentPrefix, comparison))
            throw new IOException($"Refusing to stage a runtime pack outside its parent: {stagingDirectory}.");
        if (Directory.Exists(stagingDirectory) || File.Exists(stagingDirectory))
            throw new IOException($"Runtime-pack staging attempt already exists: {stagingDirectory}.");

        Directory.CreateDirectory(stagingDirectory);
        return stagingDirectory;
    }

    private static RuntimeAssemblyCopyResult CopyRuntimeAssembly(
        string sourceAssemblyPath,
        string activeAndroidAssemblyPath,
        string stagingDirectory,
        GameIdentity gameIdentity,
        RuntimePackGenerationHooks hooks
    )
    {
        var destinationPath = Path.Combine(stagingDirectory, RuntimeAssemblyFileName);
        File.Copy(sourceAssemblyPath, destinationPath, overwrite: false);

        var copiedSourceSha256 = Sha256Hex(destinationPath);
        if (!string.Equals(
                copiedSourceSha256,
                gameIdentity.SourceAssemblySha256,
                StringComparison.Ordinal
            ))
        {
            throw new InvalidDataException(
                $"Copied source sts2.dll does not match the authoritative GameIdentity: expected={gameIdentity.SourceAssemblySha256}; actual={copiedSourceSha256}."
            );
        }

        hooks?.AfterSourceCopied?.Invoke(stagingDirectory);
        var publicizerResult = AndroidAssemblyPublicizer.Publicize(
            destinationPath,
            sourceAssemblyPath,
            activeAndroidAssemblyPath
        );
        return new RuntimeAssemblyCopyResult(
            destinationPath,
            Sha256Hex(destinationPath),
            publicizerResult
        );
    }

    private static string[] CopyRuntimeSupportAssemblies(
        string sourceAssemblyPath,
        string stagingDirectory,
        IDictionary<string, string> supportAssemblySha256
    )
    {
        var sourceDirectory = Path.GetDirectoryName(sourceAssemblyPath);
        if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
            return Array.Empty<string>();

        var copied = new List<string>();
        foreach (var fileName in RuntimeSupportAssemblyFileNames)
        {
            var sourcePath = Path.Combine(sourceDirectory, fileName);
            if (!File.Exists(sourcePath))
                continue;

            var destinationPath = Path.Combine(stagingDirectory, fileName);
            File.Copy(sourcePath, destinationPath, overwrite: false);
            copied.Add(fileName);
            supportAssemblySha256[fileName] = Sha256Hex(destinationPath);
        }

        return copied.ToArray();
    }

    internal static string Sha256Hex(string path)
    {
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

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private readonly record struct RuntimeAssemblyCopyResult(
        string Path,
        string Sha256,
        AndroidAssemblyPublicizer.Result PublicizerResult
    );

    private readonly struct RuntimePackGenerationPaths
    {
        internal RuntimePackGenerationPaths(string dataDir, GameIdentity gameIdentity)
        {
            var gameDirectory = Steam.SteamGameInstallPaths.GameDirectory(
                dataDir,
                gameIdentity.Branch
            );
            SourceAssemblyPath = GameIdentityReader.ResolveInstalledSourceAssemblyPath(
                gameDirectory
            );
            ActiveAndroidAssemblyPath = GameRuntimeSlot.FindActiveAndroidAssemblyPath(dataDir);
            ReleaseInfoPath = Path.Combine(gameDirectory, "release_info.json");
            BranchMarkerPath = Steam.SteamGameInstallPaths.BranchMarkerPath(
                dataDir,
                gameIdentity.Branch
            );
            FinalPackDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(
                dataDir,
                gameIdentity.Branch
            );
        }

        internal string SourceAssemblyPath { get; }
        internal string ActiveAndroidAssemblyPath { get; }
        internal string ReleaseInfoPath { get; }
        internal string BranchMarkerPath { get; }
        internal string FinalPackDirectory { get; }
    }
}
