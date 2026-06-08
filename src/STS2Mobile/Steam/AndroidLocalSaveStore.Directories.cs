using System;
using System.IO;
using System.Linq;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidLocalSaveStore
{
    bool ISaveStore.DirectoryExists(string path) => Directory.Exists(FullPath(path));

    string[] ISaveStore.GetFilesInDirectory(string directoryPath)
    {
        var fullPath = FullPath(directoryPath);
        if (!Directory.Exists(fullPath))
        {
            PatchHelper.Log($"[Cloud] Android local save files: {directoryPath} -> {fullPath} missing");
            return Array.Empty<string>();
        }

        var files = Directory.GetFiles(fullPath).Select(Path.GetFileName).ToArray();
        PatchHelper.Log($"[Cloud] Android local save files: {directoryPath} -> {fullPath} count={files.Length}");
        return files;
    }

    string[] ISaveStore.GetDirectoriesInDirectory(string directoryPath)
    {
        var fullPath = FullPath(directoryPath);
        if (!Directory.Exists(fullPath))
        {
            PatchHelper.Log($"[Cloud] Android local save directories: {directoryPath} -> {fullPath} missing");
            return Array.Empty<string>();
        }

        var directories = Directory.GetDirectories(fullPath).Select(Path.GetFileName).ToArray();
        PatchHelper.Log($"[Cloud] Android local save directories: {directoryPath} -> {fullPath} count={directories.Length}");
        return directories;
    }

    void ISaveStore.CreateDirectory(string directoryPath) => Directory.CreateDirectory(FullPath(directoryPath));

    void ISaveStore.DeleteDirectory(string directoryPath)
    {
        var fullPath = FullPath(directoryPath);
        if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, recursive: true);
    }

    void ISaveStore.DeleteTemporaryFiles(string directoryPath)
    {
        var fullPath = FullPath(directoryPath);
        if (!Directory.Exists(fullPath))
            return;

        foreach (var file in Directory.GetFiles(fullPath, "*.tmp"))
            File.Delete(file);
    }
}
