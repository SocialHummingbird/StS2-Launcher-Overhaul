using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    private const uint PckMagic = 0x43504447;
    private const long MinimumPckHeaderBytes = 96;

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
