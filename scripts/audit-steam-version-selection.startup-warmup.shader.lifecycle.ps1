function Add-SteamVersionSelectionStartupWarmupShaderLifecycleChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.cs" `
        "keeps shader warmup screen root limited to state and entry point" `
        @(
            "internal sealed partial class ShaderWarmupScreen : Control",
            "WarmupVersion = 9",
            "WarmupTimeBudgetSeconds = 90",
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
        "src\STS2Mobile\Launcher\StartupPresentationLayerPolicy.cs" `
        "keeps startup cover, shader warmup, and recovery presentation ordering explicit" `
        @(
            "StartupStatusCanvasLayer = 0",
            "StartupStatusZIndex = 4096",
            "ShaderWarmupCanvasLayer = 127",
            "RecoveryCanvasLayer = 128",
            "HasValidOrdering"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupPresentationLifecycle.cs" `
        "keeps warmup visibility and retained startup-cover cleanup policy testable" `
        @(
            "PresentationState",
            "StartupCoverRetained => true",
            "WarmupVisible",
            "InputBlocked",
            "MarkVisible",
            "TryBeginCleanup",
            "CleanupRequested"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupPresentationHost.cs" `
        "places Android shader warmup above startup status with lifecycle-safe cleanup" `
        @(
            "OperatingSystem\.IsAndroid\(\)",
            "new CanvasLayer",
            "Layer = StartupPresentationLayerPolicy\.ShaderWarmupCanvasLayer",
            "layer\.AddChild\(screen\)",
            "parent\.AddChild\(root\)",
            "_lifecycle\.MarkVisible\(\)",
            "await _screen\.RunAsync\(\)",
            "_lifecycle\.TryBeginCleanup\(\)",
            "HideRoot\(\)",
            "ProcessMode = Node\.ProcessModeEnum\.Disabled",
            "canvasLayer\.Visible = false",
            "_root\.QueueFree\(\)",
            "startup cover remains active",
            "ObjectDisposedException"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupFlow.ShaderWarmup.cs" `
        "runs shader warmup through the ordered presentation host and always cleans it up" `
        @(
            "startup\.ShowShaderWarmup\(\)",
            "ShaderWarmupPresentationRun\.ExecuteAsync",
            "presentation\.RunAsync",
            "presentation\.QueueFree"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupPresentationRun.cs" `
        "keeps warmup execution and cleanup ordering shared with production-path tests" `
        @(
            "ExecuteAsync",
            "await runPresentation\(\)",
            "finally",
            "cleanupPresentation\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.cs" `
        "keeps recovery controls above the shader-warmup presentation" `
        @(
            "CanvasLayerIndex = StartupPresentationLayerPolicy\.RecoveryCanvasLayer"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.WarmupRun.cs" `
        "isolates shader warmup run context and completion reporting" `
        @(
            "WarmupCompletion",
            "WarmupPartialCompletion",
            "MaterialCount",
            "RenderedMaterialCount",
            "ElapsedMilliseconds",
            "WarmupRun",
            "SceneTree Tree",
            "ShaderWarmupProgress Progress",
            "LauncherMonotonicDeadline Deadline",
            "IsOverBudget",
            "CompleteAndReport",
            "CompletePartialAndReport",
            "Progress\.Complete\(completion\)",
            "PatchHelper\.Log\(Message\.Completed\(completion\)\)",
            "PatchHelper\.Log\(Message\.CompletedPartial\(completion\)\)",
            "CreateWarmupRun",
            "Deadline\.ElapsedMilliseconds",
            "CreateProgress",
            "ShaderWarmupProgress\.ForLabels"
        )
}
