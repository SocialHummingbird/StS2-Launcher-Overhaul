function Add-SteamVersionSelectionStartupWarmupShaderExecutionChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.Execution.cs" `
        "isolates shader warmup collection, rendering, and marker completion flow" `
        @(
            "RunWarmupAsync",
            "CreateWarmupRun\(deadline\)",
            "warmup\.Deadline",
            "warmup\.IsOverBudget",
            "WriteWarmupStatus\(\s*""completed-deadline""",
            "hard deadline reached during resource scan",
            "CollectWarmupMaterialsAsync",
            "materials\.Count == 0",
            "MarkWarmupComplete\(\)",
            "WriteWarmupStatus\(""collecting""",
            "WriteWarmupStatus\(""waiting-post-draw""",
            "WriteWarmupStatus\(\s*""rendering""",
            "WriteWarmupStatus\(\s*""completed""",
            "WriteWarmupStatus\(\s*""completed-partial""",
            "RenderWarmupMaterialsAsync",
            "warmup\.CompleteAndReport\(rendered\)",
            "warmup\.CompletePartialAndReport\(rendered, materials\.Count\)",
            "WarmupTimeBudgetSeconds",
            "renderPlan\.CompletionClassification\(\)",
            "ShaderWarmupRenderPlan\.ForMaterialCount",
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
        "keeps frame and finish-delay waits within the shared warmup deadline" `
        @(
            "WaitPostDrawAsync\(LauncherMonotonicDeadline deadline\)",
            "LauncherAsyncYield\.FramePostDrawAsync\(deadline\)",
            "WaitFinishDelayAsync\(LauncherMonotonicDeadline deadline\)",
            "LauncherAsyncYield\.DelayAsync"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.Run.cs" `
        "records shader warmup watchdog status without changing launch flow" `
        @(
            "WatchWarmupDurationAsync",
            "WatchdogWarningSeconds",
            "LauncherMonotonicDeadline\.Start",
            "TimeSpan\.FromSeconds\(WarmupTimeBudgetSeconds\)",
            "await RunWarmupAsync\(deadline\)",
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
            "StS2 Launcher shader warmup status",
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
            "DeduplicationStoppedByBudget",
            "DeduplicatedMaterialCount",
            "HardDeadlineReached",
            "ThreadedLoadRequestCount",
            "ThreadedLoadCompletedCount",
            "ThreadedLoadTimeoutCount",
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
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.MaterialScanner.Collection.cs" `
        "deduplicates materials cooperatively within the shared warmup deadline" `
        @(
            "UniqueByShaderAsync",
            "LauncherMonotonicDeadline deadline",
            "deadline\.IsExpired",
            "LauncherAsyncYield\.ProcessFrameAsync",
            "MarkDeduplicationStoppedByBudget"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.Messages.MaterialScanner.cs" `
        "keeps shader scanner summary logging available without per-failure log floods" `
        @(
            "ScanSummary",
            "scenes=",
            "materials=",
            "loads=",
            "loadTimeouts=",
            "budgetStopped=",
            "deadlineReached=",
            "failures="
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMonotonicDeadline.cs" `
        "uses one monotonic hard deadline across every warmup phase" `
        @(
            "Environment\.TickCount64",
            "ElapsedMilliseconds",
            "IsExpired",
            "RemainingDelayMilliseconds",
            "Math\.Max\(\s*1L",
            "ForTest"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherThreadedLoadPolicy.cs" `
        "keeps threaded request, polling, failure, and deadline decisions deterministic" `
        @(
            "EvaluateRequest",
            "EvaluatePoll",
            "ContinuePolling",
            "Complete",
            "Fail",
            "DeadlineExceeded"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherAsyncYield.cs" `
        "adapts Godot process-frame, post-draw, and delay signals to the shared waiter" `
        @(
            "ProcessFrameAsync",
            "FramePostDrawAsync",
            "DelayAsync",
            "CreateWaiter",
            "GodotLauncherAsyncSignalSource",
            "ILauncherAsyncSignalSource",
            "SceneTree\.SignalName\.ProcessFrame",
            "RenderingServer\.SignalName\.FramePostDraw"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherAsyncSignalWaiter.cs" `
        "bounds injectable signals by deadline pause resume and destruction" `
        @(
            "ILauncherAsyncSignalSource",
            "LauncherOperationLifecycle",
            "WaitForProcessFrameAsync",
            "WaitForFramePostDrawAsync",
            "WaitUntilActiveAsync",
            "Task\.WhenAny",
            "DeadlineExceeded",
            "Destroyed",
            "LauncherOperationLifecycleState\.Paused"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherThreadedResourceLoader.cs" `
        "adapts Godot threaded resource APIs to the production load operation" `
        @(
            "ILauncherThreadedResourceApi<Resource>",
            "LauncherThreadedResourceLoadOperation\.RunAsync",
            "ResourceLoader\.LoadThreadedRequest",
            "ResourceLoader\.LoadThreadedGetStatus",
            "ThreadLoadStatus\.InProgress",
            "ThreadLoadStatus\.Loaded",
            "ResourceLoader\.LoadThreadedGet",
            "LauncherThreadedLoadOutcome\.Cancelled"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherThreadedResourceLoadOperation.cs" `
        "uses one testable production loop for successful failed hung and cancelled loads" `
        @(
            "ILauncherThreadedResourceApi<TResource>",
            "LauncherThreadedResourceOperationOutcome",
            "resources\.Request",
            "resources\.Poll",
            "resources\.Retrieve",
            "LauncherThreadedLoadPolicy\.EvaluateRequest",
            "LauncherThreadedLoadPolicy\.EvaluatePoll",
            "waiter\.WaitForProcessFrameAsync",
            "LauncherAsyncWaitOutcome\.Destroyed",
            "DeadlineExceeded",
            "Cancelled"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.MaterialScanner.MaterialResources.cs" `
        "does not synchronously load loose shader resources on the main thread" `
        @(
            "ResourceLoader\.Load(?:<[^>]+>)?\("
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\ShaderWarmupScreen.MaterialScanner.SceneExtraction.cs" `
        "does not synchronously load packed scenes on the main thread" `
        @(
            "ResourceLoader\.Load(?:<[^>]+>)?\("
        )
}
