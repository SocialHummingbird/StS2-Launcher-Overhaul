using System;

namespace STS2Mobile;

public static partial class ModEntry
{
    private const int ApplyComplete = 2;
    private const int ApplyInProgress = 1;
    private const int ApplyNotStarted = 0;
    private const int BootstrapProbeCode = 1729;
    private const string HarmonyId = "com.sts2mobile";
    private const int HarmonyConstructorProbeCode = 1730;
    private const int ProbeFailure = -1;
    private const int ProbeSuccess = 0;
    private const int ProbeSuccessWithValue = 1;
    private const uint GodotPckMagic = 0x43504447;
    private const string GameBranchFileName = "game_branch";
    private const string GameDirectoryName = "game";
    private const string GameVersionsDirectoryName = "game_versions";
    private const string GamePckFileName = "SlayTheSpire2.pck";
    private const string LauncherBootstrapVariable = "STS2_LAUNCHER_BOOTSTRAP";
    private const string BootstrapUiModeVariable = "STS2_BOOTSTRAP_UI_MODE";
    private const string MinimalBootstrapUiVariable = "STS2_MINIMAL_BOOTSTRAP_UI";
    private const int StartupFallbackShieldZIndex = 4090;
    private const int StartupFallbackLauncherZIndex = 4092;
    private const int MinimumPckHeaderLength = 96;
    private const string TempDirectoryName = "tmp";
    private static readonly string[] TempVariableNames =
    {
        "TMPDIR",
        "TMP",
        "TEMP",
    };
    private static int _applyState = ApplyNotStarted;
    private static int _exceptionHandlersInstalled;
    private static string _startupFallbackReason;

    internal static bool HasStartupFallbackReason
        => !string.IsNullOrWhiteSpace(_startupFallbackReason);
}
