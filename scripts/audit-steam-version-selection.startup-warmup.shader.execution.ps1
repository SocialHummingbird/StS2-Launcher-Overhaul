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
}
