using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchSwitchSafety
{
    internal const string MarkerFileName = "last_game_branch_switch.txt";

    internal static string MarkerPath(string dataDir)
        => Path.Combine(dataDir, MarkerFileName);

    internal static bool HasMarker(string dataDir)
        => File.Exists(MarkerPath(dataDir));
}
