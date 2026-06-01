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

            var directoryOffset = ReadPckDirectoryOffset(reader, fs.Length);
            if (!directoryOffset.HasValue)
                return false;

            if (directoryOffset.Value <= 0 || directoryOffset.Value + 4 > fs.Length)
                return false;

            fs.Position = directoryOffset.Value;
            return reader.ReadUInt32() > 0;
        }
        catch
        {
            return false;
        }
    }

    private static long? ReadPckDirectoryOffset(
        BinaryReader reader,
        long fileLength
    )
    {
        if (fileLength < MinimumPckHeaderBytes)
            return null;

        if (reader.ReadUInt32() != PckMagic)
            return null;

        reader.ReadUInt32(); // format version
        reader.ReadUInt32(); // major
        reader.ReadUInt32(); // minor
        reader.ReadUInt32(); // patch
        reader.ReadUInt32(); // flags
        reader.ReadInt64(); // file base

        return reader.ReadInt64();
    }
}
