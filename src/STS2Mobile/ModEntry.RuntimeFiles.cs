using System;
using System.IO;
using System.Text;
using Godot;

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
                : Path.Combine(RuntimeDataDirectory, GameVersionsDirectoryName, StateDirectoryName(branch), GameDirectoryName);
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

    private static string StateDirectoryName(string branch)
    {
        branch = StorageIdentity(branch);

        if (string.Equals(branch, "public", StringComparison.OrdinalIgnoreCase))
            return "public";

        if (string.Equals(branch, "beta", StringComparison.OrdinalIgnoreCase))
            return "beta";

        var sb = new StringBuilder(branch.Length);
        foreach (var ch in branch)
        {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.')
                sb.Append(ch);
            else
                sb.Append('_');
        }

        var safePrefix = sb.Length == 0 ? "branch" : sb.ToString();
        if (safePrefix.Length > 48)
            safePrefix = safePrefix[..48].TrimEnd('.', '-', '_');

        if (safePrefix.Length == 0)
            safePrefix = "branch";

        return $"{safePrefix}-{StableBranchHash(branch)}";
    }

    private static string StorageIdentity(string branch)
        => string.IsNullOrWhiteSpace(branch) ? "public" : branch.Trim().ToLowerInvariant();

    private static string StableBranchHash(string branch)
    {
        unchecked
        {
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;

            var hash = offsetBasis;
            foreach (var ch in branch)
            {
                hash ^= ch;
                hash *= prime;
            }

            return hash.ToString("x8");
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
