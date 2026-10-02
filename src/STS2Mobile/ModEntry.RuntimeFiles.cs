using System;
using System.IO;
using Godot;
using STS2Mobile.Steam;

namespace STS2Mobile;

public static partial class ModEntry
{
    private static bool IsStandaloneLauncherRequired()
        => !IsGamePckStructurallyReady(GamePckPath);

    private static string GamePckPath
        => Path.Combine(
            GameDirectoryPath,
            GamePckFileName
        );

    private static string GameDirectoryPath
    {
        get
        {
            var branch = ReadSelectedBranch();
            return string.Equals(branch, "public", StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(RuntimeDataDirectory, GameDirectoryName)
                : Path.Combine(RuntimeDataDirectory, GameVersionsDirectoryName, SteamGameBranch.StateDirectoryName(branch), GameDirectoryName);
        }
    }

    private static string ManagedTempDirectory
        => Path.Combine(RuntimeDataDirectory, TempDirectoryName);

    private static string ReadSelectedBranch()
    {
        try
        {
            var path = Path.Combine(RuntimeDataDirectory, GameBranchFileName);
            if (!File.Exists(path))
                return "public";

            var branch = File.ReadAllText(path).Trim();
            return string.IsNullOrWhiteSpace(branch) ? "public" : branch;
        }
        catch
        {
            return "public";
        }
    }

    private static string RuntimeDataDirectory
    {
        get
        {
            try
            {
                var dataDir = OS.GetDataDir();
                if (BootstrapTrace.TryNormalizeDirectory(dataDir, out var normalized))
                    return normalized;
            }
            catch
            {
            }

            return BootstrapTrace.ResolveFallbackDataDirectory();
        }
    }

    private static void ConfigureWritableTempDirectory()
    {
        Directory.CreateDirectory(ManagedTempDirectory);

        foreach (var variable in TempVariableNames)
            System.Environment.SetEnvironmentVariable(variable, ManagedTempDirectory);

        PatchHelper.Log($"Using writable temp directory: {ManagedTempDirectory}");
    }

    private static bool IsGamePckStructurallyReady(string path)
    {
        try
        {
            if (!File.Exists(path))
                return false;

            using var fs = File.OpenRead(path);
            using var reader = new BinaryReader(fs);
            if (!TryReadPckDirectoryBase(reader, fs.Length, out var dirBase))
                return false;

            fs.Position = dirBase;
            return reader.ReadUInt32() > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadPckDirectoryBase(BinaryReader reader, long fileLength, out long dirBase)
    {
        dirBase = 0;
        if (fileLength < MinimumPckHeaderLength)
            return false;

        if (reader.ReadUInt32() != GodotPckMagic)
            return false;

        reader.ReadUInt32();
        reader.ReadUInt32();
        reader.ReadUInt32();
        reader.ReadUInt32();
        reader.ReadUInt32();
        reader.ReadInt64();
        dirBase = reader.ReadInt64();
        return dirBase > 0 && dirBase + 4 <= fileLength;
    }
}
