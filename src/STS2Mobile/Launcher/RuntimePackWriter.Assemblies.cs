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

    private static string PreparePackDirectory(GameRuntimeSlot slot)
    {
        var packDirectory = Path.GetDirectoryName(slot.RuntimePackManifestPath);
        if (string.IsNullOrWhiteSpace(packDirectory))
            return null;

        if (Directory.Exists(packDirectory))
            Directory.Delete(packDirectory, recursive: true);
        Directory.CreateDirectory(packDirectory);
        return packDirectory;
    }

    private static void CopyRuntimeAssembly(GameRuntimeSlot slot, string packDirectory)
        => File.Copy(
            slot.SourceAssemblyPath,
            Path.Combine(packDirectory, RuntimeAssemblyFileName),
            overwrite: true
        );

    private static string[] CopyRuntimeSupportAssemblies(
        GameRuntimeSlot slot,
        string packDirectory,
        IDictionary<string, string> supportAssemblySha256
    )
    {
        var sourceDirectory = Path.GetDirectoryName(slot.SourceAssemblyPath);
        if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
            return Array.Empty<string>();

        var copied = new List<string>();
        foreach (var fileName in RuntimeSupportAssemblyFileNames)
        {
            var sourcePath = Path.Combine(sourceDirectory, fileName);
            if (!File.Exists(sourcePath))
                continue;

            var destinationPath = Path.Combine(packDirectory, fileName);
            File.Copy(sourcePath, destinationPath, overwrite: true);
            copied.Add(fileName);
            supportAssemblySha256[fileName] = Sha256Hex(destinationPath);
        }

        return copied.ToArray();
    }

    private static string Sha256Hex(string path)
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

        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }
}
