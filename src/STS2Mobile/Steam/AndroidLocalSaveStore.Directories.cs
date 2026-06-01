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
            return Array.Empty<string>();

        return Directory.GetFiles(fullPath).Select(Path.GetFileName).ToArray();
    }

    string[] ISaveStore.GetDirectoriesInDirectory(string directoryPath)
    {
        var fullPath = FullPath(directoryPath);
        if (!Directory.Exists(fullPath))
            return Array.Empty<string>();

        return Directory.GetDirectories(fullPath).Select(Path.GetFileName).ToArray();
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
