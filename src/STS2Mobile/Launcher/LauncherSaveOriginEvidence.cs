using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherSaveOriginEvidence
{
    internal const string MarkerFileName = "current_android_save_origin.txt";

    internal static string MarkerPath(string dataDir)
        => Path.Combine(dataDir, MarkerFileName);

    internal static bool MarkerPresent(string dataDir)
        => File.Exists(MarkerPath(dataDir));
}
