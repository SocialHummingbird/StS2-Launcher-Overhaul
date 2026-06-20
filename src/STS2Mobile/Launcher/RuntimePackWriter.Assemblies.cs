using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class RuntimePackWriter
{
    private static readonly string[] RuntimeSupportAssemblyFileNames =
    {
        "Steamworks.NET.dll",
        "Sentry.dll"
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

    private static string[] CopyRuntimeSupportAssemblies(GameRuntimeSlot slot, string packDirectory)
    {
        // Android packages the support assemblies with the app. Runtime packs only swap the branch-specific game assembly.
        return Array.Empty<string>();
    }
}
