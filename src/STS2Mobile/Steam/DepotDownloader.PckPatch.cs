using System;
using System.IO;
using System.Text;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static void PatchGamePck(string pckPath)
    {
        const uint maxPckPathBytes = 4096;

        if (!File.Exists(pckPath))
            return;

        try
        {
            using var fs = new FileStream(pckPath, FileMode.Open, FileAccess.ReadWrite);
            using var reader = new BinaryReader(fs);

            uint magic = reader.ReadUInt32();
            if (magic != 0x43504447) // "GDPC"
                return;

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
                    PatchHelper.Log($"PCK patching skipped: invalid path length {pathLen}");
                    return;
                }

                byte[] pathBytes = reader.ReadBytes((int)pathLen);
                string path = Encoding.UTF8.GetString(pathBytes).TrimEnd('\0');
                long offset = reader.ReadInt64();
                long size = reader.ReadInt64();
                reader.ReadBytes(16); // MD5
                reader.ReadUInt32(); // flags

                long absOffset = relativeOffsets ? fileBase + offset : offset;

                patched |= PatchPckEntry(path, fs, absOffset, size);
            }

            if (patched)
                PatchHelper.Log("Patched game PCK: removed Android-incompatible plugin references");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"PCK patching failed (non-fatal): {ex.Message}");
        }
    }

    private static bool IsPckPath(string path, string expected)
    {
        return path == expected || path == $"res://{expected}";
    }

    private static bool PatchPckEntry(string path, FileStream fs, long offset, long size)
    {
        if (IsPckPath(path, "project.binary"))
            return PatchProjectBinary(fs, offset, size);
        if (IsPckPath(path, "project.godot"))
            return PatchProjectGodot(fs, offset, size);
        if (IsPckPath(path, ".godot/extension_list.cfg"))
            return PatchExtensionList(fs, offset, size);
        if (IsPckPath(path, "scenes/game.tscn"))
            return PatchGameScene(fs, offset, size);

        return false;
    }
}
