function Add-SteamVersionSelectionStartupWarmupChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupFlow.StartupMode.cs" `
        "keeps startup safe-mode decisions in a small marker-driven shell" `
        @(
            "private sealed partial class StartupMode",
            "CreateFromMarkers",
            "PreviousStartupPhase\.FromMarkers",
            "ConsumeManualSafeLaunchMarker",
            "SafeLaunchRequested",
            "ShouldForceLocalSaves",
            "PhaseSettingsAndSaves",
            "PhaseGameStartup",
            "ShouldSkipShaderWarmup",
            "PhaseShaderWarmup",
            "SafeLaunchMessage"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupFlow.StartupMode.PreviousPhase.cs" `
        "isolates previous startup phase marker reads and comparisons" `
        @(
            "PreviousStartupPhase",
            "LauncherLaunchMarkers\.ReadStartupPhase",
            "StringComparison\.OrdinalIgnoreCase",
            "Matches\(string phase\)",
            "DescribePreviousStall\(string message\)",
            "\$""\{message\} \{Phase\}"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupFlow.StartupMode.SaveModePlan.cs" `
        "isolates startup local-save safe-mode application" `
        @(
            "StartupSaveModePlan",
            "Loading settings and saves in local-only safe mode",
            "Loading settings and saves",
            "LauncherPreferences\.LoadAndApplyCloudSyncEnabled",
            "LauncherCloudSaveState\.DisableCloudSyncForLaunch",
            "PatchHelper\.Log\(ReasonLog\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.cs" `
        "keeps shader warmup screen root limited to state and entry point" `
        @(
            "internal sealed partial class ShaderWarmupScreen : Control",
            "WarmupVersion = 5",
            "TaskCompletionSource<bool> _tcs",
            "Label _statusLabel",
            "Label _detailLabel",
            "ProgressBar _progressBar",
            "RunAsync",
            "Initialize\(\)",
            "await _tcs\.Task"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.Run.cs" `
        "isolates shader warmup screen initialization and deferred execution" `
        @(
            "private void Initialize\(\)",
            "ZIndex = 100",
            "GetViewport\(\)\?\.GetVisibleRect\(\)\.Size",
            "BuildUI\(vpSize\)",
            "PatchHelper\.Log\(Message\.ScreenInitialized\)",
            "PatchHelper\.Log\(Message\.ScreenBuildFailed\(ex\)\)",
            "_tcs\?\.TrySetResult\(false\)",
            "Callable\.From\(RunWarmup\)\.CallDeferred\(\)",
            "RunWarmupTaskAsync",
            "PatchHelper\.Log\(Message\.RunFailed\(ex\)\)",
            "_tcs\?\.TrySetResult\(true\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.WarmupRun.cs" `
        "isolates shader warmup run context and completion reporting" `
        @(
            "WarmupCompletion",
            "MaterialCount",
            "ElapsedMilliseconds",
            "WarmupRun",
            "SceneTree Tree",
            "ShaderWarmupProgress Progress",
            "Stopwatch Stopwatch",
            "CompleteAndReport",
            "Progress\.Complete\(completion\)",
            "PatchHelper\.Log\(Message\.Completed\(completion\)\)",
            "CreateWarmupRun",
            "Stopwatch\.StartNew\(\)",
            "CreateProgress",
            "ShaderWarmupProgress\.ForLabels"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.Execution.cs" `
        "isolates shader warmup collection, rendering, and marker completion flow" `
        @(
            "RunWarmupAsync",
            "CreateWarmupRun",
            "CollectWarmupMaterialsAsync",
            "materials\.Count == 0",
            "MarkWarmupComplete\(\)",
            "RenderWarmupMaterialsAsync",
            "warmup\.CompleteAndReport\(materials\.Count\)",
            "WaitFinishDelayAsync",
            "progress\.ShowScanning\(\)",
            "ShaderWarmupMaterialScanner\.CollectAsync",
            "PatchHelper\.Log\(Message\.Collected\(materials\.Count\)\)",
            "progress\.ShowCompiling\(\)",
            "ShaderWarmupRenderer\.ForScreen",
            "WriteWarmupVersion\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.Timing.cs" `
        "isolates shader warmup frame and finish-delay waits" `
        @(
            "WaitPostDrawAsync",
            "RenderingServer\.SignalName\.FramePostDraw",
            "WaitFinishDelayAsync",
            "GetTree\(\)\.CreateTimer\(0\.5\)",
            "SceneTreeTimer\.SignalName\.Timeout"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Components\StyledProgressBar.cs" `
        "uses a taller styled compact percentage progress bar" `
        @(
            "internal StyledProgressBar\(float scale, bool compact = false\)",
            "compact\s*\?\s*LauncherComponentTheme\.CompactProgressBarHeight",
            "ShowPercentage = true",
            "CompactProgressBarFontSize",
            "BackgroundStyle",
            "FillStyle",
            "ProgressFillCompact",
            "BuildProgressStyle"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.UI.cs" `
        "uses Android-readable compact sizing and styled progress for shader warmup" `
        @(
            "OperatingSystem\.IsAndroid\(\)",
            "AndroidMinimumScale = 1\.06f",
            "AndroidMinimumPanelWidth = 320f",
            "AndroidPanelWidthRatio = 0\.94f",
            "CalculateAdaptiveScale\(vpSize, androidCompact\)",
            "widthRatio: CalculatePanelWidthRatio\(vpSize, androidCompact\)",
            "compact: androidCompact",
            "CalculateWarmupPanelSize\(vpSize, androidCompact\)",
            "new StyledProgressBar\(scale, androidCompact\)",
            "androidCompact \? AndroidMinimumScale : MinimumScale",
            "return AndroidPanelWidthRatio"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.cs" `
        "routes startup status creation between Android card and legacy label" `
        @(
            "internal static partial class LauncherStartupStatus",
            "OperatingSystem\.IsAndroid\(\)",
            "CreateAndroidStatusCard\(parent, viewportSize\)",
            "CreateLegacyLabel\(parent, viewportSize\)",
            "Startup status label creation failed",
            "CalculateSafeMargin"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.Android.cs" `
        "composes an Android-readable framed startup status card after launcher close" `
        @(
            "internal static partial class LauncherStartupStatus",
            "AndroidMinimumScale = 1\.06f",
            "AndroidWidthRatio = 0\.94f",
            "AndroidMessageFontSize = 18",
            "AndroidPanelHeight = 98",
            "CreateAndroidStatusCard",
            "PanelContainer",
            "BuildAndroidPanelStyle",
            "CreateAndroidTitleLabel",
            "CreateAndroidMessageLabel",
            "CalculateAndroidPanelWidth",
            "MouseFilterEnum\.Ignore"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.Android.Metrics.cs" `
        "isolates Android startup status scale and width math" `
        @(
            "CalculateAndroidScale",
            "ReferenceShortEdge",
            "AndroidMinimumScale",
            "AndroidMaximumScale",
            "Math\.Clamp",
            "CalculateAndroidPanelWidth",
            "AndroidWidthRatio"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.Android.Labels.cs" `
        "isolates Android startup status title and message labels" `
        @(
            "CreateAndroidTitleLabel",
            "Starting Game",
            "AndroidTitleFontSize",
            "LauncherComponentTheme\.OrangeHot",
            "CreateAndroidMessageLabel",
            "MessageNodeName",
            "AndroidMessageFontSize",
            "AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
            "LauncherComponentTheme\.TextPrimary"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.Android.Style.cs" `
        "isolates Android startup status card frame styling" `
        @(
            "BuildAndroidPanelStyle",
            "LauncherStyleBoxes\.MakeFilled",
            "PanelBackground",
            "0\.92f",
            "AndroidPanelRadius",
            "LauncherComponentTheme\.CyanDim",
            "AndroidPanelHorizontalMargin",
            "AndroidPanelVerticalMargin"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.Legacy.cs" `
        "keeps the desktop startup status label fallback isolated" `
        @(
            "CreateLegacyLabel",
            "CalculateFontSize",
            "AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
            "Control\.LayoutPreset\.TopWide",
            "font_size",
            "new Color\(0\.55f, 0\.85f, 1f\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupStatus.cs" `
        "cleans up the full Android startup status card after observed game startup" `
        @(
            "internal static bool QueueFree\(Label label\)",
            "FindStatusRoot\(label\)",
            "for \(Node current = label; current != null; current = current\.GetParent\(\)\)",
            "current\.Name == NodeName",
            "target\.QueueFree\(\)",
            "Startup status cleanup failed"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameStartupRecovery.State.cs" `
        "uses startup-status root cleanup instead of freeing only the message label" `
        @(
            "LauncherStartupStatus\.QueueFree\(StartupStatus\)",
            "Post-startup recovery UI cleanup finished after game startup was observed",
            "statusCleared"
        )
}
