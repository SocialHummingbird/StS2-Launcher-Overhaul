using System;
using System.IO;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private void EnsureEnoughFreeSpaceForFile(string directory, long fileSize, string fileName)
    {
        if (!OperatingSystem.IsAndroid())
            return;

        long availableFreeSpace;
        try
        {
            Directory.CreateDirectory(directory);
            availableFreeSpace = GetAndroidAvailableFreeSpace(directory);
        }
        catch (Exception ex)
        {
            Log($"Could not check free space for {fileName}: {ex.Message}");
            return;
        }

        var required = fileSize + AndroidMinimumFreeSpaceBytes;
        if (availableFreeSpace < required)
        {
            throw new IOException(
                $"Not enough storage for {fileName}: need {FormatBytes(required)}, "
                    + $"available {FormatBytes(availableFreeSpace)}"
            );
        }
    }

    private static long GetAndroidAvailableFreeSpace(string directory)
    {
        var usableBytes = AndroidGodotAppBridge.GetUsableSpaceBytes(directory);
        if (usableBytes > 0)
            return usableBytes;

        var root = Path.GetPathRoot(Path.GetFullPath(directory));
        if (string.IsNullOrWhiteSpace(root))
            return long.MaxValue;

        var drive = new DriveInfo(root);
        return drive.AvailableFreeSpace;
    }
}
