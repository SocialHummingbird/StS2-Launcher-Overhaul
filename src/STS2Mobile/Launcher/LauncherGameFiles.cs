using System.IO;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class LauncherGameFiles
{
    private const uint PckMagic = 0x43504447;
    private const long MinimumPckHeaderBytes = 96;

    internal static string PckPath(string dataDir) =>
        Path.Combine(dataDir, LauncherStorageNames.GameDirectory, LauncherStorageNames.GamePck);

    internal static bool Ready() => Ready(OS.GetDataDir());

    internal static bool Ready(string dataDir)
        => IsValidPck(PckPath(dataDir));

    internal static void DeleteDownloadedState(string dataDir)
    {
        DeleteDirectory(Path.Combine(dataDir, LauncherStorageNames.GameDirectory));
        DeleteDirectory(Path.Combine(dataDir, LauncherStorageNames.DownloadStateDirectory));
        DeleteFile(LauncherLaunchMarkers.StartupMarkerPath);
        PatchHelper.Log("[Launcher] Deleted downloaded game files and download state");
    }

    internal static string FormatSize(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        if (bytes >= 1024L * 1024)
            return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / 1024.0:F0} KB";
    }

    private static void DeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to delete directory {path}: {ex.Message}");
        }
    }

    private static void DeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to delete file {path}: {ex.Message}");
        }
    }

    private static bool IsValidPck(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            using var reader = new BinaryReader(fs);

            if (!TryReadPckDirectoryOffset(reader, fs.Length, out var directoryOffset))
                return false;

            if (directoryOffset <= 0 || directoryOffset + 4 > fs.Length)
                return false;

            fs.Position = directoryOffset;
            return reader.ReadUInt32() > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadPckDirectoryOffset(
        BinaryReader reader,
        long fileLength,
        out long directoryOffset
    )
    {
        directoryOffset = 0;

        if (fileLength < MinimumPckHeaderBytes)
            return false;

        if (reader.ReadUInt32() != PckMagic)
            return false;

        reader.ReadUInt32(); // format version
        reader.ReadUInt32(); // major
        reader.ReadUInt32(); // minor
        reader.ReadUInt32(); // patch
        reader.ReadUInt32(); // flags
        reader.ReadInt64(); // file base

        directoryOffset = reader.ReadInt64();
        return true;
    }
}
