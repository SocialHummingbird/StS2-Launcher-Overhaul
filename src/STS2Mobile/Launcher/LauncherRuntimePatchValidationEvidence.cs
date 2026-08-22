using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimePatchValidationEvidence
{
    internal const string MarkerFileName = "last_runtime_patch_validation.json";

    internal static string MarkerPath(string dataDir)
        => Path.Combine(dataDir, MarkerFileName);

    internal static bool MarkerPresent(string dataDir)
        => File.Exists(MarkerPath(dataDir));
}
