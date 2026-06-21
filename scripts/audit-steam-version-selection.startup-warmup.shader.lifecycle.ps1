function Add-SteamVersionSelectionStartupWarmupShaderLifecycleChecks {
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
}
