namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    private static string ReadMarkerValue(string path, string prefix)
        => LauncherMarkerFile.ReadValue(path, prefix);

    private static int? ReadMarkerInt(string path, string prefix)
        => LauncherMarkerFile.ReadInt(path, prefix);
}
