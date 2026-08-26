using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using STS2Mobile.Launcher;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private void PatchGamePck(string pckPath)
    {
        RequireUpdatingBeforeInstalledMutation();
        PatchGamePck(
            pckPath,
            targetArm64: RuntimeInformation.ProcessArchitecture == Architecture.Arm64,
            enableArm64V2FmodManagers: false
        );
    }

    internal static void RepairGamePckForArm64V2(
        BranchInstallUpdate update,
        string pckPath
    )
    {
        if (update == null)
            throw new ArgumentNullException(nameof(update));

        update.RequireUpdatingBeforeInstalledMutation();
        PatchGamePck(
            pckPath,
            targetArm64: true,
            enableArm64V2FmodManagers: true
        );
    }

    private static void PatchGamePck(
        string pckPath,
        bool targetArm64,
        bool enableArm64V2FmodManagers
    )
    {
        const uint maxPckPathBytes = 4096;

        if (!File.Exists(pckPath))
            throw new FileNotFoundException("Cannot prepare the Android PCK because the downloaded PCK is missing.", pckPath);

        try
        {
            using var fs = new FileStream(pckPath, FileMode.Open, FileAccess.ReadWrite);
            using var reader = new BinaryReader(fs);

            uint magic = reader.ReadUInt32();
            if (magic != 0x43504447) // "GDPC"
                throw new InvalidDataException($"Downloaded PCK has invalid magic: {pckPath}");

            reader.ReadUInt32(); // format version
            reader.ReadUInt32(); // major
            reader.ReadUInt32(); // minor
            reader.ReadUInt32(); // patch
            uint flags = reader.ReadUInt32();
            long fileBase = reader.ReadInt64();
            long dirBase = reader.ReadInt64();
            fs.Seek(16 * 4, SeekOrigin.Current); // 16 reserved uint32s

            bool relativeOffsets = (flags & 0x02) != 0;

            fs.Position = dirBase;
            uint fileCount = reader.ReadUInt32();
            bool patched = false;

            for (uint i = 0; i < fileCount; i++)
            {
                uint pathLen = reader.ReadUInt32();
                if (pathLen == 0 || pathLen > maxPckPathBytes)
                {
                    throw new InvalidDataException(
                        $"Downloaded PCK contains invalid path length {pathLen}: {pckPath}"
                    );
                }

                byte[] pathBytes = reader.ReadBytes((int)pathLen);
                string path = Encoding.UTF8.GetString(pathBytes).TrimEnd('\0');
                long offset = reader.ReadInt64();
                long size = reader.ReadInt64();
                reader.ReadBytes(16); // MD5
                reader.ReadUInt32(); // flags

                long absOffset = relativeOffsets ? fileBase + offset : offset;

                patched |= PatchPckEntry(
                    path,
                    fs,
                    absOffset,
                    size,
                    targetArm64,
                    enableArm64V2FmodManagers
                );
            }

            fs.Flush(flushToDisk: true);
            if (patched)
                PatchHelper.Log("Patched game PCK: removed Android-incompatible plugin references");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"PCK patching failed: {ex.Message}");
            throw new IOException(
                $"Failed to prepare downloaded PCK for Android: {ex.Message}",
                ex
            );
        }
    }

    private static bool IsPckPath(string path, string expected)
    {
        return path == expected || path == $"res://{expected}";
    }

    private static bool PatchPckEntry(
        string path,
        FileStream fs,
        long offset,
        long size,
        bool targetArm64,
        bool enableArm64V2FmodManagers
    )
    {
        if (IsPckPath(path, "project.binary"))
        {
            return PatchProjectBinary(
                fs,
                offset,
                size,
                enableArm64V2FmodManagers
            );
        }
        if (IsPckPath(path, "project.godot"))
        {
            return PatchProjectGodot(
                fs,
                offset,
                size,
                enableArm64V2FmodManagers
            );
        }
        if (IsPckPath(path, ".godot/extension_list.cfg"))
            return PatchExtensionList(fs, offset, size);
        if (IsPckPath(path, "scenes/game.tscn"))
            return PatchGameScene(fs, offset, size, targetArm64);

        return false;
    }
}
