function Add-SteamVersionSelectionStartupWarmupShaderExecutionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.Execution.cs" `
        "isolates shader warmup collection, rendering, and marker completion flow" `
        @(
            "RunWarmupAsync",
            "CreateWarmupRun",
            "CollectWarmupMaterialsAsync",
            "materials\.Count == 0",
            "MarkWarmupComplete\(\)",
            "WriteWarmupStatus\(""collecting""",
            "WriteWarmupStatus\(""waiting-post-draw""",
            "WriteWarmupStatus\(""rendering""",
            "WriteWarmupStatus\(""completed""",
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
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.Run.cs" `
        "records shader warmup watchdog status without changing launch flow" `
        @(
            "WatchWarmupDurationAsync",
            "WatchdogWarningSeconds",
            "Task\.Delay\(TimeSpan\.FromSeconds\(WatchdogWarningSeconds\)\)",
            "WriteWarmupStatus\(""watchdog-warning""",
            "PatchHelper\.Log\(Message\.WatchdogWarning\(WatchdogWarningSeconds\)\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.Marker.cs" `
        "writes shader warmup phase marker for crash and stall reports" `
        @(
            "StatusMarkerPath",
            "LauncherStorageNames\.ShaderWarmupStatus",
            "WriteWarmupStatus",
            "StS2 Mobile shader warmup status",
            "Warmup version:"
        )
}
