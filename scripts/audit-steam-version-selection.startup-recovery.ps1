function Add-SteamVersionSelectionStartupRecoveryChecks {

    Add-Check `
        "src\STS2Mobile\Launcher\PostStartupDiagnosticsPolicy.cs" `
        "keeps full-diagnostic and duplicate-scheduling decisions testable" `
        @(
            "DetailedTraceEnabled",
            "ShouldWriteFullDiagnostics",
            "detailedTraceEnabled \|\| failureOrRecovery",
            "PostStartupDiagnosticsScheduleGate",
            "Interlocked\.CompareExchange",
            "Interlocked\.Exchange",
            "IsStopped",
            "Stop"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PostStartupDiagnosticsSettings.cs" `
        "enables detailed traces only through the explicit marker or environment setting" `
        @(
            "LauncherStorageNames\.DetailedPostStartupTrace",
            "PostStartupDiagnosticsPolicy\.EnvironmentVariable",
            "PostStartupDiagnosticsConfiguration\.DetailedTraceEnabled",
            "RuntimeConfigurationSource"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PostStartupDiagnosticsConfiguration.cs" `
        "keeps production diagnostic opt-in inputs injectable for focused tests" `
        @(
            "IPostStartupDiagnosticsConfigurationSource",
            "EnvironmentValue",
            "MarkerExists",
            "PostStartupDiagnosticsPolicy\.DetailedTraceEnabled"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupFlow.GameStartupAttempt.cs" `
        "gates the pre-start scene snapshot and defaults to a lightweight heartbeat" `
        @(
            "WriteStartupEntryEvidence",
            "ShouldWriteFullDiagnostics",
            "failureOrRecovery: false",
            "if \(writeFullDiagnostics\)",
            "WriteStartupSceneSnapshot",
            "WritePostStartupHeartbeat",
            "Scene-tree traversal skipped on ordinary startup path"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupFlow.GameStartupAttempt.Runner.cs" `
        "uses gated startup-entry evidence before invoking game startup" `
        @(
            'WriteStartupEntryEvidence\("before NGame\.GameStartup"\)',
            "Invoking NGame\.GameStartup"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherStartupFlow.GameStartupAttempt.Runner.cs" `
        "does not directly traverse or snapshot the scene tree" `
        @(
            "WriteStartupSceneSnapshot",
            "WriteSceneSnapshot",
            "WritePostStartupTrace"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameStartupRecovery.PostStartupTrace.cs" `
        "writes successful evidence as a heartbeat unless detailed traces are enabled" `
        @(
            "WriteSuccessfulPostStartupEvidence",
            "ShouldWriteFullDiagnostics",
            "failureOrRecovery: false",
            "WritePostStartupHeartbeat\(phase, mergedDetails\)",
            "if \(writeFullTrace\)",
            "LauncherDiagnostics\.WritePostStartupTrace",
            "fullTrace=\{writeFullTrace\}"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameStartupRecovery.PostStartupTrace.cs" `
        "deduplicates delayed diagnostics and cancels them on scene teardown" `
        @(
            "PostStartupScheduleGate\.TrySchedule",
            "duplicate request ignored",
            "CancellationTokenSource",
            "gameNode\.TreeExiting \+= CancelForTreeExit",
            "PostStartupScheduleGate\.Stop",
            "Task\.Delay\(",
            "cancellationToken",
            "gameNode\.TreeExiting -= CancelForTreeExit"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameStartupRecovery.cs" `
        "records successful GameStartup completion through the gated evidence path" `
        @(
            "MarkGameStartupCompleted",
            "WriteSuccessfulPostStartupEvidence",
            "NGame\.GameStartup completed before main-menu guard"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameStartupRecovery.MainMenu.cs" `
        "uses lightweight success evidence while preserving full recovery and failure traces" `
        @(
            "if \(scene\.IsMainMenu\)",
            "WriteSuccessfulPostStartupEvidence",
            "main menu guard passed",
            "WritePostStartupTrace",
            "main menu guard missing",
            "LauncherDiagnostics\.WritePostStartupTrace",
            "main menu guard failed"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameStartupRecovery.StateUpdate.cs" `
        "skips successful scene snapshots unless detailed diagnostics are enabled" `
        @(
            "Reason != StartupObservationReason",
            "Reason != WatchdogRecoveredReason",
            "PostStartupDiagnosticsSettings\.DetailedTraceEnabled",
            "WriteStartupSceneSnapshot"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\AndroidMainMenuPreparation.Evidence.cs" `
        "writes lightweight successful preparation evidence and full failure or opt-in evidence" `
        @(
            "ShouldWriteFullDiagnostics",
            "failureOrRecovery: !result\.CanExposeMainMenu",
            "if \(writeFullDiagnostics\)",
            "WriteDetailedEvidence",
            "WriteLightweightHeartbeat",
            "Evidence level: lightweight heartbeat"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameStartupRecovery.Watchdog.cs" `
        "retains a full scene trace when the startup watchdog fires" `
        @(
            "RecoveryStateUpdate\.WatchdogStalled",
            "WritePostStartupTrace",
            "startup watchdog fired"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\AndroidMainMenuWorkingSet.cs" `
        "defines the bounded main-menu working set with logical source paths" `
        @(
            "LogicalPath",
            "res://images/atlases/intent_atlas\.png",
            "res://images/ui/reward_screen/reward_panel\.png",
            "res://images/ui/run_history/monster\.png",
            "res://images/packed/timeline/epoch_slot_locked\.png",
            "res://shaders/button_pulse\.gdshader",
            "MaximumResourceCount = 32"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\AndroidMainMenuWorkingSet.cs" `
        "does not pin version-specific Godot import outputs" `
        @(
            "\.godot/imported",
            "[a-f0-9]{32}",
            "\.ctex"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\AndroidMainMenuWorkingSetPlan.cs" `
        "resolves and deduplicates logical paths through the active virtual filesystem" `
        @(
            "activeVirtualResourceExists",
            "HashSet<string>\(StringComparer\.Ordinal\)",
            "IsLogicalResourcePath",
            '!path\.StartsWith\("res://\.godot/"',
            "MissingLogicalPaths",
            "DuplicateCount",
            "RejectedCount"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\AndroidMainMenuWorkingSetIdentity.cs" `
        "sanitizes branch, PCK, and mod identity for preparation evidence" `
        @(
            "Branch",
            "PckIdentity",
            "ModMode",
            "trimmed\.Length != 64",
            "Uri\.IsHexDigit",
            "Substring\(0, 12\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\AndroidMainMenuResourceResolution.cs" `
        "records only virtual logical and effective resource paths" `
        @(
            "LogicalPath",
            "EffectivePath",
            'StartsWith\("res://"',
            "<non-virtual>"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\AndroidMainMenuPreparationResult.cs" `
        "admits main-menu handoff only after rendered-frame stability" `
        @(
            "StableRenderedFrames",
            "TimedOut",
            "Aborted",
            "Failed",
            "CanExposeMainMenu",
            "Outcome is AndroidMainMenuPreparationOutcome\.NotRequired",
            "or AndroidMainMenuPreparationOutcome\.StableRenderedFrames"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\AndroidMainMenuPreparation.Frames.cs" `
        "measures consecutive FramePostDraw timing within the hard deadline" `
        @(
            "CreateChild",
            "MaximumFrameObservationMs",
            "MainMenuRenderedFrameHandoff\.WaitAsync",
            "LauncherAsyncYield\.CreateWaiter",
            "IsPreparationTargetAlive",
            "LauncherOperationLifecycle"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\AndroidMainMenuPreparation.Frames.cs" `
        "does not substitute ProcessFrame for rendered-frame evidence" `
        @(
            "ProcessFrame",
            "Task\.Delay"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\MainMenuRenderedFrameHandoff.cs" `
        "uses production FramePostDraw signals for stable timeout and destroyed handoffs" `
        @(
            "WaitForFramePostDrawAsync",
            "LauncherAsyncWaitOutcome\.Destroyed",
            "LauncherAsyncWaitOutcome\.DeadlineExceeded",
            "ObserveRenderedFrame",
            "AndroidMainMenuPreparationResult\.Stable",
            "AndroidMainMenuPreparationResult\.Aborted",
            "TryTimeout",
            "AndroidMainMenuPreparationResult\.TimedOut"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\MainMenuRenderedFrameHandoff.cs" `
        "does not substitute ProcessFrame for rendered-frame evidence" `
        @(
            "ProcessFrame"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\AndroidMainMenuPreparation.Resources.cs" `
        "uses bounded threaded loading and rendered material exercise" `
        @(
            "AndroidMainMenuWorkingSetPlan\.Create",
            "path => ResourceLoader\.Exists\(path\)",
            "entry\.LogicalPath",
            "LauncherThreadedResourceLoader\.LoadAsync",
            "LauncherThreadedLoadOutcome\.DeadlineExceeded",
            "LauncherThreadedLoadOutcome\.Cancelled",
            "Optional working-set budget exhausted at",
            "continuing to rendered-frame stability",
            "LauncherAsyncYield\.FramePostDrawAsync",
            "LauncherOperationLifecycle"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\AndroidMainMenuPreparation.Resources.cs" `
        "does not translate logical requests to imported cache paths" `
        @(
            "\.godot/imported",
            "entry\.Path"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\AndroidMainMenuPreparation.Resources.cs" `
        "does not synchronously load main-menu resources" `
        @(
            "ResourceLoader\.Load<",
            "ResourceLoader\.Load\("
        )

    Add-Check `
        "src\STS2Mobile\Launcher\AndroidMainMenuPreparation.cs" `
        "treats optional preload exhaustion as advisory but fails closed without rendered-frame stability" `
        @(
            "OverallPreparationDeadlineMs",
            "LauncherMonotonicDeadline\.Start",
            "LauncherOperationLifecycleMonitor",
            "lifecycle\.Destroy\(\)",
            "state\.ResourceBudgetReached",
            "Resource preload budget reached\. Checking rendered frames",
            "CaptureWorkingSetIdentity",
            "attempt\.SelectedBranch",
            "attempt\.PckSha256",
            "attempt\.ModPlayMode",
            "IsPreparationTargetAlive",
            "stability\.TryAbort",
            "WaitForStableRenderedFramesAsync"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\AndroidMainMenuPreparation.cs" `
        "does not block NMainMenu solely because the advisory working-set budget expired" `
        @(
            "state\.ThreadedLoadTimedOut",
            "threaded working-set load exceeded the resource deadline"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherOperationLifecycleMonitor.cs" `
        "maps Android Home resume and scene teardown into startup lifecycle state" `
        @(
            "NotificationApplicationPaused",
            "NotificationApplicationResumed",
            "_lifecycle\.Pause\(\)",
            "_lifecycle\.Resume\(\)",
            "_ExitTree",
            "_lifecycle\.Destroy\(\)"
        )

    Add-Check `
        "tools\StartupProductionPathProbe\Program.cs" `
        "covers production startup loading signals presentation lifecycle diagnostics and invalidation" `
        @(
            "LauncherThreadedResourceLoadOperation\.RunAsync",
            "res://slow\.tres",
            "res://hung\.tres",
            "res://stalled\.tres",
            "LauncherThreadedResourceOperationOutcome\.DeadlineExceeded",
            "Home/pause",
            "scene-tree destruction",
            "MainMenuRenderedFrameHandoff\.WaitAsync",
            "ProcessFrame must not substitute",
            "ShaderWarmupPresentationRun\.ExecuteAsync",
            "standard mod pack",
            "BaseLib",
            "late-loaded packs",
            "branch, PCK, and mod-mode",
            "stale positive and negative",
            "diagnostics configuration should honor"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameStartupRecovery.cs" `
        "blocks normal startup cleanup until the rendered-frame gate admits handoff" `
        @(
            "AndroidMainMenuPreparation\.RunAsync",
            "if \(!preparation\.CanExposeMainMenu\)",
            "HandleMainMenuPreparationFailure",
            "return true"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameStartupRecovery.Watchdog.cs" `
        "blocks watchdog cleanup when rendered-frame stability fails" `
        @(
            "AndroidMainMenuPreparation\.RunAsync",
            "if \(!preparation\.CanExposeMainMenu\)",
            "MainMenuRenderingUnstable",
            "return",
            "ui\.MarkRecoveredStartup"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupFlow.GameStartupAttempt.Runner.cs" `
        "marks startup observed only after the readiness result succeeds" `
        @(
            "if \(!await CompleteAsync\(\)\)",
            "return",
            "MarkObserved\(\)",
            "return await Startup\.EnsureMainMenuReadyAsync\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\AndroidMainMenuPreparation.Evidence.cs" `
        "records rendered-frame timing and handoff classification" `
        @(
            "Handoff allowed",
            "lifecycle teardown blocked handoff",
            "Selected branch",
            "Selected PCK identity",
            "Mod play mode",
            "active Godot virtual filesystem after mod-pack mounting",
            "Resolved resource",
            "Missing logical resource",
            "FramePostDraw stability outcome",
            "FramePostDraw samples",
            "Slow rendered frames",
            "Average rendered-frame interval ms",
            "Maximum rendered-frame interval ms",
            "Consecutive stable rendered frames"
        )

    Add-Check `
        "src\STS2Mobile\Patches\AndroidAtlasFallbackResolutionCache.cs" `
        "uses generation-checked atlas fallback publication across invalidation" `
        @(
            "CaptureGeneration",
            "expectedGeneration",
            "_generation != expectedGeneration",
            "TryStoreHit",
            "TryStoreMiss",
            "TryRemove",
            "IsCurrent",
            "AndroidAtlasFallbackCacheInvalidation",
            "_generation = checked\(_generation \+ 1\)"
        )

    Add-Check `
        "src\STS2Mobile\Patches\AndroidAtlasResourceChangeTracker.cs" `
        "invalidates successful resource changes while preserving failed and no-op generations" `
        @(
            "RecordResourcePackLoad",
            "!loadSucceeded \|\| !resourcesMayHaveChanged",
            "ObserveMountedResourceSet",
            "BaselineRecorded",
            "Unchanged",
            "_cache\.Invalidate\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Patches\AndroidAtlasCompatibilityPatches.cs" `
        "patches the global Godot resource-pack boundary and retries stale atlas resolutions" `
        @(
            "PatchResourcePackLoad",
            "nameof\(ProjectSettings\.LoadResourcePack\)",
            "types: new\[\] \{ typeof\(string\), typeof\(bool\), typeof\(int\) \}",
            "LoadResourcePackPostfix",
            "RecordResourcePackLoad",
            "loadSucceeded: __result",
            "resourcesMayHaveChanged: true",
            "TryStoreHit",
            "TryStoreMiss",
            "FallbackResolutionCache\.IsCurrent",
            "MaximumResolutionAttempts"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\AndroidMainMenuWorkingSetIdentity.cs" `
        "keys mounted resource identity by branch PCK and mod mode" `
        @(
            "ResourceSetKey",
            '\$"\{Branch\}\|\{PckIdentity\}\|\{ModMode\}"'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\AndroidMainMenuPreparation.cs" `
        "observes branch and runtime resource identity before logical resource preparation" `
        @(
            "CaptureWorkingSetIdentity",
            "ObserveCurrentMountedResourceSetIdentity",
            "ObserveMountedResourceSetIdentity",
            "identity\.ResourceSetKey",
            "PreloadWorkingSetAsync"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupFlow.cs" `
        "observes branch and runtime resource identity before shader warmup" `
        @(
            "CreateStartupContext",
            "ObserveCurrentMountedResourceSetIdentity",
            "RunShaderWarmupIfNeededAsync"
        )

    Add-Check `
        "src\STS2Mobile\Patches\ModLoaderPatches.cs" `
        "mounts BaseLib packs through the globally tracked Godot resource-pack boundary" `
        @(
            "ProjectSettings\.LoadResourcePack\(pckPath, true, 0\)"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Patches\ModLoaderPatches.cs" `
        "does not retain BaseLib-only atlas cache invalidation" `
        @(
            "InvalidateFallbackResolutionCache"
        )

    Add-Check `
        "tools\AtlasResourcePackPolicyProbe\Program.cs" `
        "covers pack sources branch changes and concurrent stale publication" `
        @(
            "standard-mod pack",
            "BaseLib pack",
            "late-loaded pack",
            "failed pack load",
            "declared no-op",
            "branch/PCK/mod-mode resource-set change",
            "concurrent-miss",
            "old-generation positive fallback"
        )

    Add-Check `
        "tools\AndroidAtlasCompatibilityProbe\Program.cs" `
        "attaches the compiled global resource-pack postfix to the reporter Godot runtime" `
        @(
            "AssertStandardModPackBoundary",
            "MegaCrit\.Sts2\.Core\.Modding\.ModManager",
            "Godot\.ProjectSettings",
            "AttachAndAssertPostfix",
            '"LoadResourcePackPostfix"',
            '"Godot\.ProjectSettings"',
            '"LoadResourcePack"',
            "patchInfo\?\.Postfixes",
            "Harmony postfix was not attached"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.StartupRecoveryReports.cs" `
        "adds public-sharing warning before startup recovery diagnostics content" `
        @(
            "AppendStartupRecoveryDiagnostics",
            "AppendPublicSharingWarning",
            "Data dir:"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Text.cs" `
        "labels startup recovery launcher-log copy as review-before-sharing" `
        @(
            "If startup stalls, restart the app, try Safe Start, or create a help report",
            "Review logs before sharing"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Buttons.cs" `
        "labels startup recovery launcher-log button as review-before-sharing" `
        @(
            "Copy Launcher Log \(Review First\)",
            "Review first"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.CompactButton.cs" `
        "uses structured compact startup recovery button labels instead of raw debug labels" `
        @(
            "CompactRecoveryButtonBody",
            "CompactRecoveryButtonTitle",
            "CompactRecoveryButtonDetail",
            "AddCompactRecoveryButtonLabels",
            "CompactButtonDetailLabels\.Apply",
            "CompactButtonDetailLabelSpec",
            "CompactRecoveryButtonLabels",
            '\$"\{titleText\}\\n\{detailText\}"'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Buttons.cs" `
        "uses compact startup recovery action copy instead of raw debug labels" `
        @(
            "Restart App",
            "Open launcher",
            "Safe Start",
            "Cloud off",
            "Help Report",
            "Share details",
            "Copy Log",
            "Review first",
            "Hide Help",
            "Keep waiting"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Layout.cs" `
        "keeps startup recovery controls reachable with a scroll-safe Android layout" `
        @(
            "CreateScrollContainer",
            "ScrollContainer",
            "FollowFocus = true",
            "SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
            "CreateFrame",
            "MarginContainer",
            "RecoveryMargin",
            "RecoveryTopMargin",
            "UseCompactRecoveryCopy",
            "OperatingSystem\.IsAndroid\(\)",
            "SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Construction.cs" `
        "wires startup recovery scroll hierarchy in order" `
        @(
            "CreateScrollContainer",
            "CreateFrame",
            "CreateContainer",
            "scroll\.AddChild\(frame\)",
            "frame\.AddChild\(box\)"
        )
}
