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
            "WriteWarmupStatus\(\s*""completed""",
            "WriteWarmupStatus\(\s*""completed-partial""",
            "RenderWarmupMaterialsAsync",
            "warmup\.CompleteAndReport\(rendered\)",
            "warmup\.CompletePartialAndReport\(rendered, materials\.Count\)",
            "WarmupTimeBudgetSeconds",
            "precompile time budget reached; startup continued",
            "WaitFinishDelayAsync",
            "progress\.ShowScanning\(\)",
            "ShaderWarmupMaterialScanner\.CollectAsync",
            "materials\.Diagnostics\.ToEvidenceLines\(\)",
            "WriteWarmupStatus\(\s*""collected""",
            "MergeEvidence",
            "PatchHelper\.Log\(Message\.Collected\(materials\.Materials\.Count\)\)",
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
            "WriteWarmupStatus\(\s*""watchdog-warning""",
            "PatchHelper\.Log\(Message\.WatchdogWarning\(WatchdogWarningSeconds\)\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.Marker.cs" `
        "writes shader warmup phase marker for crash and stall reports" `
        @(
            "StatusMarkerPath",
            "LauncherStorageNames\.ShaderWarmupStatus",
            "WriteWarmupStatus",
            "MergeEvidence",
            "StS2 Mobile shader warmup status",
            "Warmup version:",
            "Warmup time budget seconds:"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.MaterialScanner.Diagnostics.cs" `
        "keeps shader scanner failures summarized and rate-limited for support markers" `
        @(
            "ShaderWarmupMaterialScanDiagnostics",
            "MaxLoggedFailuresPerCategory = 5",
            "DirectoryEnumerationFailureCount",
            "ResourceLoadFailureCount",
            "SceneExtractionFailureCount",
            "PropertyReadFailureCount",
            "SceneScanStoppedByBudget",
            "ToEvidenceLines",
            "Scan failures:",
            "Scan classification:",
            "RecordDirectoryEnumerationFailure",
            "RecordResourceLoadFailure",
            "RecordSceneExtractionFailure",
            "RecordPropertyReadFailure",
            "ShouldLogFailure",
            "ShaderWarmupMaterialScanResult"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.Messages.MaterialScanner.cs" `
        "keeps shader scanner summary logging available without per-failure log floods" `
        @(
            "ScanSummary",
            "scenes=",
            "materials=",
            "budgetStopped=",
            "failures="
        )
}
