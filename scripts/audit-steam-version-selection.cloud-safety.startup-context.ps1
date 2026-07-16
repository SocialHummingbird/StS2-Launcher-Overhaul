function Add-SteamVersionSelectionCloudSafetyStartupContextChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchCoordinator.cs" `
        "uses centralized selector guidance and lightweight downloaded-state readiness in branch-switch flow" `
        @(
            "BranchSwitchConfirmationMessage",
            "SteamGameBranch\.SelectorInstallSlotHelpText",
            "LauncherBranchCatalog\.ReadVisibleBranches",
            "var visibleBranches = LauncherBranchCatalog\.ReadVisibleBranches\(_model\.DataDir\)",
            "LauncherBranchCatalog\.ReadSelectableBranches",
            "visibleBranches",
            "SelectedOptionStatus",
            "SelectedOptionDownloadProblem",
            "AppendLog\(STS2Mobile\.Steam\.SteamGameBranch\.SelectorInstallSlotHelpText",
            "Steam Cloud Push will require backup storage permission",
            "LauncherLaunchReadinessCache\.Clear",
            "_versions\.ReadGameBranchOptions",
            "SetActionPreferences\(LauncherPreferences\.ReadActionPreferences\(branch\), branches\)",
            "RefreshSelectedDownloadedStateEvidence",
            "branch switch downloaded-state readiness",
            "SelectedVersionReadyStatus\(readiness\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.cs" `
        "keeps launch coordinator shell limited to wiring public launch entry points" `
        @(
            "internal sealed partial class LauncherLaunchCoordinator",
            "_launchInProgress",
            "_activeLaunchAttempt",
            "internal void LaunchPressed\(\)",
            "internal void SafeLaunchPressed\(\)",
            "internal void AutoLaunchRequested\(bool safeLaunch\)",
            "LauncherStartGamePlan\.Normal",
            "LauncherStartGamePlan\.Safe",
            "LauncherStartGamePlan\.AutoNormal",
            "LauncherStartGamePlan\.AutoSafe",
            "StartGame\("
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.StartGame.cs" `
        "keeps Start Game orchestration as an explicit phase list" `
        @(
            "StartGame\(LauncherStartGamePlan",
            "TryBeginLaunchAttempt",
            "TryEvaluateSelectedLaunchReadiness",
            "TryEvaluateModLaunchReadiness",
            "CompleteLaunchHandoff"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.Attempt.cs" `
        "keeps launch-attempt request acceptance and terminal failure handling isolated" `
        @(
            "TryBeginLaunchAttempt",
            "FinishFailedLaunchAttempt",
            "LaunchAttemptContext",
            "_activeLaunchAttempt",
            "ActiveLaunchAttemptBranch",
            "SafeSelectedBranch",
            "SteamGameBranch\.Normalize\(LauncherPreferences\.ReadGameBranch\(\)\)",
            "source=\{plan\.Source\}",
            "launch request ignored",
            "Start Game is already running",
            "SetLaunchInProgress\(true\)",
            "SetLaunchInProgress\(false\)",
            "_activeLaunchAttempt = null",
            "SetLaunchControlsDisabled\(inProgress\)",
            "LauncherLaunchAttemptTiming\.Failed",
            "WriteLaunchAttempt",
            "LauncherLaunchAttemptPhases\.Checking",
            "LauncherLaunchReadiness\.Pending",
            "selected-version readiness has not completed",
            "launch setup failed",
            "LauncherLaunchAttemptPhases\.SetupFailed"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LaunchAttemptContext.cs" `
        "keeps launch-attempt timing and phase state out of coordinator orchestration partials" `
        @(
            "internal sealed class LaunchAttemptContext",
            "Stopwatch attemptTimer",
            "internal Stopwatch AttemptTimer",
            "internal LauncherLaunchReadiness PendingReadiness",
            "SteamGameBranch\.Normalize\(branch\)",
            "StartReadinessTiming",
            "StopReadinessTiming",
            "StartModReadinessTiming",
            "StopModReadinessTiming",
            "StopAttemptTiming",
            "BlockedTiming",
            "FailedTiming",
            "ReadyTiming",
            "LauncherLaunchAttemptTiming\.Blocked",
            "LauncherLaunchAttemptTiming\.Failed",
            "LauncherLaunchAttemptTiming\.Ready"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.Readiness.cs" `
        "keeps selected-runtime readiness phase isolated from request acceptance and handoff" `
        @(
            "TryEvaluateSelectedLaunchReadiness",
            "StartReadinessTiming",
            "StopReadinessTiming",
            "EvaluateSelectedLaunchReadiness",
            "LauncherLaunchReadiness\.Evaluate",
            "readiness\.Ready",
            "BlockedTiming",
            "FailedTiming",
            "selected-version readiness check failed",
            "launch readiness failed",
            "LauncherLaunchAttemptPhases\.ReadinessFailed"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.ModReadiness.cs" `
        "keeps launcher-side mod readiness phase isolated from selected-runtime readiness" `
        @(
            "TryEvaluateModLaunchReadiness",
            "StartModReadinessTiming",
            "StopModReadinessTiming",
            "LauncherModLaunchReadiness\.Evaluate",
            "\$""\{plan\.ModReadinessPhase\}: passed""",
            "modReadiness\.Summary",
            "FailedTiming",
            "launch mod readiness failed",
            "LauncherLaunchAttemptPhases\.ModReadinessFailed"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.Handoff.cs" `
        "keeps final launch handoff and no-op handoff diagnostics isolated" `
        @(
            "CompleteLaunchHandoff",
            "ReadyTiming",
            "WriteLaunchAttempt",
            "LauncherLaunchAttemptPhases\.Ready",
            "launch handoff failed",
            "LauncherLaunchAttemptPhases\.LaunchHandoffFailed",
            "plan\.Launch\(_model, readiness, modReadiness, attempt\.AttemptId, attempt\.ReadyTiming\)",
            "handoff\.Requested",
            "handoff\.RequestedPhase",
            "LauncherLaunchAttemptPhases\.IsSuccessfulHandoffPhase\(handoff\.RequestedPhase\)",
            "Launch handoff was requested without an accepted evidence phase",
            "launch handoff missing accepted requested phase",
            "attempt\.StopAttemptTiming\(\)",
            "timing = attempt\.ReadyTiming\(\)",
            "handoff\.FailurePhase",
            "handoff\.FailureProblem",
            "handoff\.WritePatchLog",
            "string\.IsNullOrWhiteSpace\(handoff\.FailurePhase\)",
            "LauncherLaunchAttemptPhases\.LaunchHandoffNotRequested",
            "Launch handoff was not requested after prepared readiness"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchAttemptPhases.cs" `
        "centralizes launch-attempt marker phase values used by C# writers and evidence reviewers" `
        @(
            "internal static class LauncherLaunchAttemptPhases",
            "SetupFailed = ""setup failed""",
            "Checking = ""checking""",
            "Ready = ""ready""",
            "Blocked = ""blocked""",
            "BlockedInModel = ""blocked in model""",
            "ReadinessFailed = ""readiness failed""",
            "ModReadinessFailed = ""mod readiness failed""",
            "InProcessSignalFailed = ""in-process signal failed""",
            "InProcessSignalled = ""in-process signalled""",
            "LaunchHandoffFailed = ""launch handoff failed""",
            "LaunchHandoffNotRequested = ""launch handoff not requested""",
            "RestartRequested = ""restart requested""",
            "RestartRequestedWithoutReadyFiles = ""restart requested without ready files""",
            "SafeAndroidRestartRequested = ""safe android restart requested""",
            "IsSuccessfulHandoffPhase",
            "RestartRequested",
            "SafeAndroidRestartRequested",
            "InProcessSignalled"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.Status.cs" `
        "keeps selected-version ready status and lightweight display readiness separate from Start Game handoff" `
        @(
            "ShowLoggedIn",
            "SelectedVersionReadyStatus",
            "SelectedVersionDownloadRequiredStatus",
            "SteamGameInstallPaths\.VersionSlotKind",
            "Active install slot",
            "RefreshSelectedRuntimeSlotEvidence",
            "RefreshSelectedDownloadedStateEvidence",
            "string branch,",
            "string phase",
            "LauncherLaunchReadiness\.EvaluateDownloadedState",
            "final runtime pairing check runs when Start Game is pressed",
            "SelectedVersionReadyStatus\(readiness\)",
            "SelectedVersionReadyStatus\(string baseStatus, LauncherLaunchReadiness readiness\)",
            "SelectedVersionDownloadRequiredStatus\(readiness\)",
            "SelectedVersionDownloadRequiredStatus\(LauncherLaunchReadiness readiness\)",
            "readiness\?\.Branch \?\? LauncherPreferences\.ReadGameBranch\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "exposes launch-control disabling and combined startup branch/preference application without leaking action-section internals into launch orchestration" `
        @(
            "SetActionPreferences\(",
            "LauncherPreferences\.ActionPreferences preferences",
            "IReadOnlyList<LauncherBranchCatalog\.BranchOption> branches",
            "Download\.SetGameBranchOptions\(preferences\.GameBranch, branches\)",
            "Actions\.SetGameBranchOptions\(preferences\.GameBranch, branches\)",
            "internal void SetLaunchControlsDisabled\(bool disabled\)",
            "Actions\.SetLaunchControlsDisabled\(disabled\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
        "keeps Start Game and Safe Start disabled while launch handoff is in progress" `
        @(
            "_launchControlsDisabled",
            "internal void SetLaunchControlsDisabled\(bool disabled\)",
            "ApplyLaunchControlsDisabled",
            "_launchButton\.Disabled = _launchControlsDisabled",
            "_safeLaunchButton\.Disabled = _launchControlsDisabled"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.Secondary.cs" `
        "reapplies launch-control disabled state when ready-action visibility changes" `
        @(
            "SetSecondaryButtonsVisible",
            "ApplyLaunchControlsDisabled"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartGamePlan.cs" `
        "defines explicit Start Game phases for normal and safe launch without duplicating coordinator flow" `
        @(
            "internal sealed class LauncherStartGamePlan",
            "internal string Source",
            "LauncherLaunchSource\.Button",
            "LauncherLaunchSource\.AutoLaunch",
            "LauncherLaunchSource\.Automation",
            "ButtonPressedPhase",
            "ReadinessPhase",
            "ReadinessPassedPhase",
            "BlockedPhase",
            "ModReadinessPhase",
            "CheckingStatus",
            "StartingStatus",
            "Func<LauncherLaunchAttemptTiming> timingSnapshot",
            "Start Game readiness passed",
            "Safe launch readiness passed",
            "Auto-launch readiness passed",
            "Auto-safe-launch readiness passed",
            "Automation launch readiness passed",
            "Automation safe launch readiness passed",
            "AutoNormal",
            "AutoSafe",
            "AutomationNormal",
            "AutomationSafe",
            "internal LauncherLaunchHandoffResult Launch\(",
            "model\.LaunchSafe\(readiness, modReadiness, Source, attemptId, timingSnapshot\)",
            "model\.Launch\(readiness, modReadiness, Source, attemptId, timingSnapshot\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchAttemptTiming.cs" `
        "captures Start Game timing evidence without doing extra launch work" `
        @(
            "internal sealed class LauncherLaunchAttemptTiming",
            "Stopwatch",
            "AttemptElapsedText",
            "ReadinessElapsedText",
            "ModReadinessElapsedText",
            "Blocked",
            "Ready",
            "Failed",
            "<not measured>"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchReadinessCacheStatus.cs" `
        "centralizes launch-readiness cache status strings used by marker and evidence tooling" `
        @(
            "internal static class LauncherLaunchReadinessCacheStatus",
            "Fresh = ""fresh""",
            "FreshCached = ""fresh-cached""",
            "MemoryCacheHit = ""memory-cache-hit""",
            "Pending = ""pending""",
            "DownloadedStateOnly = ""downloaded-state-only""",
            "DownloadedStateCheckFailed = ""downloaded-state-check-failed"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchReadiness.cs" `
        "keeps selected-version launch readiness as a reusable evidence model" `
        @(
            "internal sealed partial class LauncherLaunchReadiness",
            "internal string Branch",
            "internal bool Ready",
            "internal string ReadinessProblem",
            "internal GameRuntimeSlot RuntimeSlot",
            "internal string CacheStatus",
            "RuntimePairingStatus",
            "PatchCompatibilityStatus",
            "RuntimePackUsable",
            "RuntimeCacheMarkerPresent",
            "RuntimePatchValidationMarkerPresent",
            "WithCacheStatus"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchReadiness.Pending.cs" `
        "keeps accepted-launch pending readiness marker construction isolated" `
        @(
            "Pending",
            "LauncherLaunchReadinessCacheStatus\.Pending",
            "runtimeSlot: null"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchReadiness.FullRuntime.cs" `
        "keeps full selected-runtime launch readiness behind the Start Game path" `
        @(
            "Evaluate",
            "EvaluateFresh",
            "DownloadedForValidation\(dataDir, branch, out var downloadProblem\)",
            "PatchCompatibilityValidator\.ValidateSelectedVersionSlot",
            "runtime pairing inspected",
            "LauncherRuntimeSlotEvidence\.Write",
            "LauncherLaunchReadinessCacheStatus\.Fresh",
            "LauncherLaunchReadinessCache\.TryGet",
            "LauncherLaunchReadinessCache\.Store"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchReadiness.DownloadedState.cs" `
        "keeps lightweight downloaded-state readiness separate from full runtime validation" `
        @(
            "EvaluateDownloadedState",
            "selected downloaded files present",
            "selected downloaded files check failed",
            "LauncherLaunchReadinessCacheStatus\.DownloadedStateOnly",
            "LauncherLaunchReadinessCacheStatus\.DownloadedStateCheckFailed",
            "Selected game version check failed before launch",
            "final runtime validation deferred to Start Game"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchReadinessCache.cs" `
        "keeps selected-version launch readiness cache storage and miss reporting isolated" `
        @(
            "internal static class LauncherLaunchReadinessCache",
            "TryGet",
            "Store",
            "Clear",
            "LauncherLaunchReadinessCacheKey\.Create",
            "LauncherLaunchReadinessCacheKey Key",
            "TargetMatches",
            "MatchesCurrent",
            "reason=empty",
            "reason=target changed",
            "reason=cache changed",
            "reason=cache validation failed",
            "launch readiness cache store skipped",
            "IsCacheable",
            "ClearMatchingTarget",
            "ClearSnapshot",
            "not inspected full-runtime readiness",
            "clearedMatchingCache",
            "clearedStaleCache",
            "LauncherLaunchReadinessCacheStatus\.Fresh",
            "StringComparison\.Ordinal",
            "readiness\.HasRuntimeSlot",
            "ExceptionDetail",
            "exception=",
            "LauncherLaunchReadinessCacheStatus\.MemoryCacheHit",
            "LauncherLaunchReadinessCacheStatus\.FreshCached",
            "mismatchReason"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchReadinessCacheKey.cs" `
        "matches cached selected-runtime readiness only while launch-relevant file identities are unchanged" `
        @(
            "internal sealed class LauncherLaunchReadinessCacheKey",
            "Create",
            "LauncherLaunchReadinessCacheIdentities\.Capture",
            "LauncherLaunchReadinessCacheIdentities Identities",
            "TargetMatches",
            "MatchesCurrent",
            "mismatchReason",
            "Identities\.MatchesCurrent"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchReadinessCacheIdentities.cs" `
        "captures launch-readiness cache file and marker identities away from cache storage and matching" `
        @(
            "internal readonly struct LauncherLaunchReadinessCacheIdentities",
            "Capture",
            "MatchesCurrent",
            "MatchesIdentity",
            "string actual",
            "pck identity changed",
            "runtime slot marker identity changed",
            "source assembly identity changed",
            "active Android assembly identity changed",
            "runtime pack manifest identity changed",
            "runtime pack payload identity changed",
            "runtime cache marker identity changed",
            "runtime patch validation marker identity changed",
            "patch compatibility marker identity changed",
            "MaxMarkerIdentityBytes",
            "oversized-marker",
            "LauncherGameFiles\.PckPath",
            "FileIdentity\(LauncherGameFiles\.PckPath",
            "SteamGameInstallPaths\.BranchMarkerPath",
            "MarkerIdentity\(SteamGameInstallPaths\.BranchMarkerPath",
            "MarkerIdentity\(Path\.Combine\(gameDirectory, ""release_info\.json""\)",
            "GameRuntimeSlot\.FindSourceAssemblyPath",
            "GameRuntimeSlot\.FindActiveAndroidAssemblyPath",
            "ActiveAndroidAssemblyIdentity",
            "FileIdentity\(GameRuntimeSlot\.FindActiveAndroidAssemblyPath",
            "LauncherRuntimeSlotEvidence\.MarkerPath",
            "RuntimeSlotMarkerIdentity",
            "MarkerIdentity\(LauncherRuntimeSlotEvidence\.MarkerPath",
            "SHA256\.HashData",
            "File\.OpenRead\(path\)",
            "LauncherRuntimeCacheEvidence\.MarkerPath",
            "MarkerIdentity\(LauncherRuntimeCacheEvidence\.MarkerPath",
            "LauncherRuntimePatchValidationEvidence\.MarkerPath",
            "RuntimePatchValidationMarkerIdentity",
            "PatchCompatibilityMarkerIdentity",
            "PatchCompatibilityEvidence\.GameDirectoryMarkerFileName",
            "GameRuntimeSlot\.RuntimePackDirectoryPath",
            "var runtimePackDirectory = GameRuntimeSlot\.RuntimePackDirectoryPath\(dataDir, branch\)",
            "RuntimePackPayloadIdentity",
            "RuntimePackPayloadIdentityFor",
            "CaptureRuntimePackPayloadFileIdentity",
            "RuntimePackPayloadFileIdentity",
            "MaxRuntimePackPayloadSampleFiles",
            "payloadSha256",
            "AddHashText",
            "sha\.TransformFinalBlock",
            "truncated",
            "IsRuntimePackPayloadFile",
            "Path\.GetFileName\(path\)",
            """compatibility\.json""",
            "SearchOption\.TopDirectoryOnly",
            "OrderBy\(path => Path\.GetFileName\(path\), StringComparer\.OrdinalIgnoreCase\)",
            "Path\.GetExtension\(path\)",
            "file:\{path\};bytes="
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchHandoffResult.cs" `
        "carries explicit launch handoff success or terminal failure details back to the coordinator" `
        @(
            "internal sealed class LauncherLaunchHandoffResult",
            "internal bool Requested",
            "internal string RequestedPhase",
            "internal string FailurePhase",
            "internal string FailureProblem",
            "internal bool WritePatchLog",
            "string requestedPhase",
            "internal static LauncherLaunchHandoffResult Success",
            "internal static LauncherLaunchHandoffResult Failed"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Launch.cs" `
        "keeps prepared launch handoff entry points separate from readiness and bridge implementation details" `
        @(
            "internal LauncherLaunchHandoffResult Launch\(",
            "internal LauncherLaunchHandoffResult LaunchSafe\(",
            "LaunchPrepared\(",
            "private LauncherLaunchHandoffResult LaunchPrepared",
            "var action = safe \? ""safe"" : ""normal""",
            "string launchSource",
            "Func<LauncherLaunchAttemptTiming> timingSnapshot",
            "timingSnapshot \?\?= LauncherLaunchAttemptTiming\.NotMeasured",
            "SelectedGameVersionReadyForLaunch\(readiness, out var readinessProblem\)",
            "LauncherLaunchHandoffResult\.Failed",
            "LauncherLaunchHandoffResult\.Success",
            "SetSafeLaunchMarker\(safe\)",
            "TrySignalInProcessLaunch\(readiness, modReadiness, safe, launchSource, attemptId, timingSnapshot\)",
            "safe && TrySafeAndroidRestart\(readiness, modReadiness, launchSource, attemptId, timingSnapshot\)",
            "RestartForLaunch\(safe, readiness, modReadiness, launchSource, attemptId, timingSnapshot\)",
            "private static void SetSafeLaunchMarker\(bool safe\)",
            "SaveManualSafeLaunchMarker",
            "ClearManualSafeLaunchMarker"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Launch.Readiness.cs" `
        "keeps model-level prepared-readiness fallback blockers isolated from launch handoff orchestration" `
        @(
            "SelectedGameVersionReadyForLaunch",
            "LauncherLaunchReadiness readiness",
            "out string problem",
            "selected game version readiness was not prepared",
            "LauncherLaunchMarkers\.RecordPhase",
            "Launch blocked"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Launch.InProcess.cs" `
        "keeps current-process launch signalling and signal-failure evidence isolated from restart fallback" `
        @(
            "private bool TrySignalInProcessLaunch\(",
            "LauncherLaunchReadiness readiness",
            "LauncherModLaunchReadiness modReadiness",
            "string launchSource",
            "var selectedBranch = readiness\.Branch",
            "in-process launch signal failed",
            "LauncherLaunchAttemptPhases\.InProcessSignalFailed",
            "LauncherLaunchAttemptPhases\.InProcessSignalled",
            "Func<LauncherLaunchAttemptTiming> timingSnapshot",
            "timingSnapshot\(\)",
            "_launchTcs\.TrySetResult\(true\)",
            "launch requires restart",
            "processBranch=",
            "RequiresProcessRestartForPreparedRuntime",
            "activeAndroidAssemblySha256=",
            "preparedAndroidAssemblySha256=",
            "Prepared Android game-code runtime does not match"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\GameRuntimeSlot.Readiness.cs" `
        "requires a restart when the active Android assembly differs from the prepared runtime pack" `
        @(
            "PreparedAndroidAssemblySha256",
            "RuntimePack\.ActualAndroidAssemblySha256",
            "ActiveAndroidAssemblyMatchesPreparedRuntime",
            "ActiveAndroidAssemblySha256",
            "RequiresProcessRestartForPreparedRuntime",
            "RuntimePackUsable"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Launch.Restart.cs" `
        "keeps restart bridge launch-attempt marker writes centralized on prepared readiness" `
        @(
            "TrySafeAndroidRestart",
            "private LauncherLaunchHandoffResult RestartForLaunch",
            "string launchSource",
            "Func<LauncherLaunchAttemptTiming> timingSnapshot",
            "timingSnapshot\(\)",
            "WriteBridgeLaunchAttempt",
            "preparedReadinessUsed: true",
            "LauncherLaunchAttemptPhases\.SafeAndroidRestartRequested",
            "LauncherLaunchAttemptPhases\.RestartRequestedWithoutReadyFiles",
            "LauncherLaunchAttemptPhases\.RestartRequested",
            "Launch blocked because selected files were not ready in final bridge check",
            "LauncherLaunchHandoffResult\.Failed",
            "LauncherLaunchHandoffResult\.Success",
            "Restarting app to launch selected game version",
            "LaunchGameOnRestart"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.cs" `
        "carries prepared fast-path readiness through the launcher session result" `
        @(
            "internal readonly struct FastPathResult",
            "internal LauncherLaunchReadiness Readiness",
            "internal bool ReadyToLaunch",
            "FastPathResult Ready\(LauncherLaunchReadiness readiness\)",
            "FastPathOutcome\.ReadyToLaunch",
            "ResetGameFilesForRedownload",
            "LauncherLaunchReadinessCache\.Clear\(""selected version redownload reset""\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.SessionFastPath.cs" `
        "uses lightweight downloaded-state readiness for saved-session display and leaves full runtime validation to Start Game" `
        @(
            "if \(hasCredentials\)",
            "if \(!hasOwnershipMarker\)",
            "FastPathResult\.AutoConnect\(\)",
            "selected downloaded-state readiness",
            "LauncherLaunchReadiness\.EvaluateDownloadedState",
            "session fast path downloaded-state readiness",
            "FastPathResult\.Ready\(readiness\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSessionCoordinator.FastPath.cs" `
        "uses saved-session prepared readiness when showing ready-to-launch status" `
        @(
            "FastPathOutcome\.ReadyToLaunch",
            "SelectedVersionReadyStatus\(_model\.WelcomeBackStatus\(\), result\.Readiness\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherUI.AutoLaunch.cs" `
        "delegates restart auto-launch into the launch coordinator instead of duplicating readiness orchestration" `
        @(
            "AutoLaunchIfRequested",
            "_controller\.AutoLaunchRequested\(safeLaunch\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.AutoLaunch.cs" `
        "routes restart auto-launch requests through the shared launch coordinator path" `
        @(
            "internal void AutoLaunchRequested\(bool safeLaunch\)",
            "_launch\.AutoLaunchRequested\(safeLaunch\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSessionCoordinator.Timeout.cs" `
        "routes offline launch timeout through lightweight downloaded-state readiness and preserves it in ready-to-launch status" `
        @(
            "LauncherLaunchReadiness\.EvaluateDownloadedState",
            "connection timeout offline downloaded-state readiness",
            "EvaluateOfflineLaunchReadiness",
            "ConnectionTimeoutOutcome\.OfflineLaunch\(readiness\)",
            "LauncherLaunchReadiness _readiness",
            "launch\.SelectedVersionReadyStatus\(_status, _readiness\)",
            "launch\.ShowReadyToLaunch"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchMarkers.cs" `
        "keeps launcher marker paths best-effort so diagnostics cannot become a startup failure source" `
        @(
            "MarkerPath\(string fileName\)",
            "OS\.GetDataDir\(\)",
            "Failed to resolve launcher marker path",
            "return fileName",
            "TryWriteMarker",
            "TryAppendMarker",
            "TryDeleteMarker",
            "MarkerPath\(LauncherStorageNames\.StartupMarker\)",
            "MarkerPath\(LauncherStorageNames\.StartupContext\)",
            "MarkerPath\(LauncherStorageNames\.StartupTimeline\)",
            "MarkerPath\(LauncherStorageNames\.ManualSafeLaunch\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchMarkers.Attempt.cs" `
        "records a unified launch-attempt marker for diagnostics and recovery" `
        @(
            "LaunchAttemptPath",
            "MarkerPath\(LauncherStorageNames\.LaunchAttempt\)",
            "LauncherStorageNames\.LaunchAttempt",
            "WriteLaunchAttempt",
            "StS2 Mobile launch attempt",
            "AttemptUtcPrefix",
            "AttemptPhasePrefix",
            "AttemptActionPrefix",
            "AttemptSourcePrefix",
            "AttemptPreparedReadinessUsedPrefix",
            "ReadinessCacheStatusPrefix",
            "RuntimeSlotIdPrefix",
            "ModEnabledCountPrefix",
            "SelectedModsPrefix",
            "SteamGameBranch\.Normalize\(LauncherPreferences\.ReadGameBranch\(\)\)",
            "MarkerLine",
            "Prepared readiness used:",
            "Launch attempt elapsed ms:",
            "Launch readiness elapsed ms:",
            "Mod readiness elapsed ms:",
            "Readiness evaluation phase:",
            "Readiness cache status:"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchMarkers.Attempt.Read.cs" `
        "parses the unified launch-attempt marker once for startup recovery display" `
        @(
            "LaunchAttemptSummaryPrefixes",
            "ReadLastLaunchAttempt",
            "LauncherMarkerFile\.ReadOptionalValues",
            "ReadAttemptValue\(values, AttemptUtcPrefix\)",
            "ReadAttemptValue\(values, AttemptPhasePrefix\)",
            "ReadAttemptValue\(values, AttemptActionPrefix\)",
            "ReadAttemptValue\(values, AttemptSourcePrefix\)",
            "ReadAttemptValue\(values, GameDirectoryPrefix\)",
            "ReadAttemptValue\(values, SourceAssemblyPathPrefix\)",
            "ReadAttemptValue\(values, ActiveAndroidAssemblyPathPrefix\)",
            "ReadAttemptValue\(values, RuntimePackManifestPathPrefix\)",
            "ReadAttemptValue\(values, ModReadinessPhasePrefix\)",
            "ReadAttemptValue\(values, ModInstalledCountPrefix\)",
            "ReadAttemptValue\(values, ModUnsupportedCountPrefix\)",
            "LaunchAttemptSummary\.Missing",
            "new LaunchAttemptSummary",
            "IReadOnlyDictionary<string, string>"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LaunchAttemptSummary.cs" `
        "keeps parsed launch-attempt recovery display separate from marker writing" `
        @(
            "internal readonly struct LaunchAttemptSummary",
            "internal string Utc",
            "internal string Source",
            "\$"" from \{Source\}""",
            "var when = HasConcrete\(Utc\)",
            "\$"" at \{Utc\}""",
            "RuntimeSlotId",
            "RuntimePairingStatus",
            "PatchCompatibilityStatus",
            "GameDirectory",
            "PckPath",
            "PckSha256",
            "SourceAssemblyPath",
            "SourceAssemblySha256",
            "ActiveAndroidAssemblyPath",
            "ActiveAndroidAssemblySha256",
            "IdentityLine",
            "Runtime identity:",
            "ShortHash",
            "RuntimePackDirectory",
            "RuntimePackManifestPath",
            "RuntimePackUsable",
            "RuntimeCacheMarkerPath",
            "RuntimeCacheMarkerPresent",
            "RuntimePatchValidationMarkerPath",
            "RuntimePatchValidationMarkerPresent",
            "PatchCompatibilityMarkerPath",
            "ModReadinessPhase",
            "ModReadinessCacheStatus",
            "ModPlayMode",
            "ModInstalledCount",
            "ModEnabledCount",
            "ModUnsupportedCount",
            "SelectedMods",
            "RecoveryHint",
            "LauncherLaunchAttemptPhases\.SetupFailed",
            "launch setup fails again",
            "LauncherLaunchAttemptPhases\.ReadinessFailed",
            "redownload the selected version to rebuild runtime evidence",
            "LauncherLaunchAttemptPhases\.ModReadinessFailed",
            "switch to Play Vanilla once",
            "LauncherLaunchAttemptPhases\.RestartRequestedWithoutReadyFiles",
            "final bridge check did not have ready files",
            "LauncherLaunchAttemptPhases\.LaunchHandoffNotRequested",
            "did not request Android to start the game",
            "LauncherLaunchAttemptPhases\.LaunchHandoffFailed",
            "LauncherLaunchAttemptPhases\.InProcessSignalFailed",
            "Android handoff failed after readiness passed",
            "LauncherLaunchAttemptPhases\.SafeAndroidRestartRequested",
            "Safe Start was already requested"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchMarkers.Auth.cs" `
        "records Steam auth failure diagnostics through the safe launcher marker path" `
        @(
            "SteamAuthFailurePath",
            "MarkerPath\(LauncherStorageNames\.SteamAuthFailure\)",
            "RecordSteamAuthFailure",
            "ReadSteamAuthFailure"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherLaunchMarkers.Attempt.cs" `
        "does not resolve OS data dir directly while writing launch-attempt diagnostics" `
        @(
            "OS\.GetDataDir\(\)",
            "Path\.Combine\(OS\.GetDataDir"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherLaunchMarkers.Attempt.Read.cs" `
        "does not reread the launch-attempt marker once per summary field" `
        @(
            "LauncherMarkerFile\.ReadOptionalValue\("
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherLaunchMarkers.Auth.cs" `
        "does not resolve OS data dir directly while writing auth diagnostics" `
        @(
            "OS\.GetDataDir\(\)",
            "Path\.Combine\(OS\.GetDataDir"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModLaunchReadinessCacheStatus.cs" `
        "centralizes mod-readiness cache status strings used by launch-attempt marker evidence" `
        @(
            "internal static class LauncherModLaunchReadinessCacheStatus",
            "Fresh = ""fresh""",
            "FreshCached = ""fresh-cached""",
            "MemoryCacheHit = ""memory-cache-hit""",
            "NotNeededVanilla = ""not-needed-vanilla"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModLaunchReadiness.cs" `
        "captures launcher-side mod selection readiness before Start Game from one safe source snapshot without replacing runtime mod-load evidence" `
        @(
            "internal sealed class LauncherModLaunchReadiness",
            "LauncherModLaunchReadinessCacheStatus\.Fresh",
            "LauncherModLaunchReadinessCacheStatus\.NotNeededVanilla",
            "var document = LauncherModSelectionState\.Load\(\)",
            "LauncherModSelectionState\.IsModdedModeFor\(document\)",
            "var identity = LauncherModSourceIdentity\.Create\(\)",
            "LauncherModLaunchReadinessCache\.TryGet\(identity, phase, out var cached\)",
            "var snapshot = LauncherModSelectionState\.KnownModsSnapshot\(document, identity\)",
            "EvaluateFresh\(phase, snapshot\.Mods\)",
            "LauncherModLaunchReadinessCache\.Store\(snapshot\.Identity, readiness\)",
            "LauncherModLaunchReadinessCache\.TryGet",
            "LauncherWorkshopModSafety\.HasActiveSelectedMods",
            "runtime mod scan skipped",
            "SelectedMods",
            "CloudPushLocked"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModSelectionState.cs" `
        "supports one loaded selector document for Start Game mod readiness without rereading selection JSON" `
        @(
            "PlayModeFor\(Load\(\)\)",
            "IsModdedModeFor\(LauncherModSelectionDocument document\)",
            "PushShouldBeLocked\(LauncherModSelectionDocument document\)",
            "EnabledModCount\(LauncherModSelectionDocument document\)",
            "KnownMods\(LauncherModSelectionDocument document\)",
            "IsPathEnabled\(string path, LauncherModSelectionDocument document\)",
            "KnownModsSnapshot\(Load\(\)\)",
            "KnownModsSnapshot\(LauncherModSelectionDocument document\)",
            "LauncherModSourceIdentity identity",
            "identity \?\?= LauncherModSourceIdentity\.Create\(\)",
            "document \?\?= DefaultDocument\(\)",
            "document\.EnabledMods \?\?= new Dictionary<string, bool>\(StringComparer\.OrdinalIgnoreCase\)",
            "private static LauncherModPlayMode PlayModeFor\(LauncherModSelectionDocument document\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModSourceIdentity.cs" `
        "tracks mod selector, Workshop, staged, and manual source identities for safe mod readiness caching" `
        @(
            "internal sealed class LauncherModSourceIdentity",
            "AppPaths\.AppPrivateModSelectionPath",
            "MetadataFileIdentity\(AppPaths\.AppPrivateModSelectionPath",
            "AppPaths\.AppPrivateWorkshopManifestPath",
            "MetadataFileIdentity\(AppPaths\.AppPrivateWorkshopManifestPath",
            "MaxMetadataIdentityBytes",
            "oversized-metadata",
            "AndroidJavaCrypto\.Sha256FileHashData",
            "AndroidJavaCrypto\.Sha256HashData",
            "SHA256\.HashData",
            "File\.OpenRead\(path\)",
            "AppPaths\.AppPrivateWorkshopStagedModsDir",
            "AppPaths\.ExternalModsDir",
            "LaunchRelevantPatterns",
            "MaxDirectoryIdentitySampleFiles",
            "\*\.json",
            "\*\.pck",
            "\*\.dll",
            "LaunchRelevantDirectoryIdentity",
            "EnumerateFiles\(path, ""\*"", SearchOption\.AllDirectories\)",
            "Where\(IsLaunchRelevantFile\)",
            "IsLaunchRelevantFile",
            "Path\.GetExtension\(path\)",
            "metadataSha256",
            "metadataBytes",
            "metadataTruncated",
            "sampleCount",
            "truncated",
            "var fileCount = 0",
            "new List<string>\(MaxDirectoryIdentitySampleFiles\)",
            "foreach \(var file in files\)",
            "fileCount\+\+",
            "AddHashText",
            "MemoryStream",
            "LaunchRelevantFileIdentity",
            "LaunchRelevantFileIdentity\.Capture",
            "identity\.Text",
            "identity\.Exists",
            "Matches"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherModSourceIdentity.cs" `
        "does not allocate entire mod metadata files, use Android-unsafe incremental managed SHA-256, or duplicate mod file stats while computing Start Game mod readiness identity" `
        @(
            "File\.ReadAllBytes",
            "SHA256\.Create",
            "TryReadFileStats",
            "Select\(LaunchRelevantFileIdentity\.Capture\)\s*\.ToArray",
            "\.Take\(MaxDirectoryIdentitySampleFiles\)\s*\.Select\(identity => identity\.Text\)\s*\.ToArray",
            "SelectMany\(pattern => Directory\.EnumerateFiles\(path, pattern, SearchOption\.AllDirectories\)\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\AndroidJavaCrypto.Sha256.cs" `
        "routes in-memory and file SHA-256 through the Android Java crypto bridge while retaining managed desktop fallbacks" `
        @(
            'Sha256Base64BridgeMethod = "sha256Base64"',
            'Sha256FileBase64BridgeMethod = "sha256FileBase64"',
            "internal static byte\[\] Sha256HashData\(ReadOnlySpan<byte> data\)",
            "internal static byte\[\] Sha256FileHashData\(string path\)",
            "OperatingSystem\.IsAndroid",
            "CallBase64Bridge",
            "Convert\.ToBase64String\(data\)"
        )

    Add-Check `
        "android\src\com\game\sts2launcher\GodotApp.java" `
        "provides Java SHA-256 bridge methods for in-memory and file launch-readiness identities" `
        @(
            "public String sha256Base64\(String dataBase64\)",
            "public String sha256FileBase64\(String path\)",
            'MessageDigest\.getInstance\("SHA-256"\)',
            "Base64\.encodeToString\(digest\.digest",
            "FileInputStream"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModLaunchReadinessCache.cs" `
        "caches mod launch readiness only while selector and mod source identities are unchanged" `
        @(
            "internal static class LauncherModLaunchReadinessCache",
            "LauncherModSourceIdentity\.Create",
            "Identity\.Matches\(key\)",
            "LauncherModLaunchReadinessCacheStatus\.MemoryCacheHit",
            "LauncherModLaunchReadinessCacheStatus\.FreshCached"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.AttachmentLists.cs" `
        "keeps launch-attempt diagnostics large enough for selected paths, hashes, mod state, and timing evidence" `
        @(
            "SummarySmallFiles",
            "new DiagnosticAttachment\(LaunchAttempt\(dataDir\), 8192\)",
            "RawErrorLogFiles",
            "new DiagnosticAttachment\(",
            "LaunchAttempt\(dataDir\)",
            "SmallAttachmentMaxChars"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Diagnostics.cs" `
        "keeps exported Help Report runtime evidence full while support display actions use lightweight downloaded-state readiness" `
        @(
            "private enum DiagnosticsReadinessScope",
            "FullRuntime",
            "DownloadedState",
            "WriteDiagnosticsReport",
            "CreateDiagnosticsSnapshot\(DiagnosticsReadinessScope\.FullRuntime\)",
            "BuildDiagnosticsSummaryForDisplay",
            "BuildRawErrorLogForClipboard",
            "CreateDiagnosticsSnapshot\(DiagnosticsReadinessScope\.DownloadedState\)",
            "private LauncherDiagnostics\.Snapshot CreateDiagnosticsSnapshot\(DiagnosticsReadinessScope readinessScope\)",
            "readinessScope switch",
            "LauncherLaunchReadiness\.Evaluate",
            "diagnostics snapshot readiness",
            "LauncherLaunchReadiness\.EvaluateDownloadedState",
            "diagnostics display downloaded-state readiness",
            "new LauncherDiagnostics\.Snapshot",
            "readiness"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportLauncherPreferences.cs" `
        "reports selected-version readiness from the prepared diagnostics readiness object" `
        @(
            "LauncherLaunchReadiness launchReadiness",
            "launchReadiness\?\.Ready",
            "launchReadiness\?\.ReadinessProblem",
            "launchReadiness\?\.CacheStatus",
            "launchReadiness\?\.EvaluationPhase",
            "AppendGameRuntimeSlot\(sb, dataDir, branch, launchReadiness\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportRuntimeSlot.cs" `
        "reuses the launch-readiness runtime slot in diagnostics before falling back to fresh inspection" `
        @(
            "LauncherLaunchReadiness launchReadiness",
            "launchReadiness\?\.RuntimeSlot \?\? GameRuntimeSlot\.Inspect\(dataDir, branch\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnosticsCoordinator.PreviousLaunch.cs" `
        "uses launch-attempt marker details once per launcher session for targeted previous-startup recovery guidance" `
        @(
            "_previousLaunchWarningChecked",
            "if \(_previousLaunchWarningChecked\)",
            "ReadLastLaunchAttempt",
            "LaunchAttemptSummary",
            "ShortLine",
            "RuntimeLine",
            "PathLine",
            "IdentityLine",
            "MarkerLine",
            "ModLine",
            "TimingLine",
            "RecoveryHint"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.cs" `
        "does not reintroduce readiness or status work into the launch coordinator shell" `
        @(
            "LauncherGameFiles\.Ready",
            "LauncherGameFiles\.ReadinessProblem",
            "LauncherLaunchReadiness\.Evaluate",
            "LauncherLaunchReadiness\.EvaluateDownloadedState",
            "WriteLaunchAttempt",
            "ShowLoggedIn"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.StartGame.cs" `
        "does not reintroduce hidden full runtime readiness helpers into Start Game orchestration" `
        @(
            "LauncherGameFiles\.Ready",
            "LauncherGameFiles\.ReadinessProblem",
            "LauncherLaunchReadiness\.Evaluate",
            "LauncherModLaunchReadiness\.Evaluate",
            "WriteLaunchAttempt",
            "LauncherLaunchAttemptTiming\.",
            "Stopwatch\.StartNew",
            "LaunchExceptionProblem",
            "SetLaunchInProgress"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherLaunchReadinessCache.cs" `
        "does not reintroduce launch-relevant file identity hashing into cache storage" `
        @(
            "SHA256\.HashData",
            "FileIdentity\(",
            "MarkerIdentity\(",
            "MaxMarkerIdentityBytes",
            "Path\.Combine\(gameDirectory"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherLaunchReadinessCacheKey.cs" `
        "does not reintroduce cache storage locking into cache-key identity checks" `
        @(
            "static readonly object Gate",
            "_cached",
            "lock \(Gate\)",
            "CacheEntry"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherLaunchReadinessCacheKey.cs" `
        "does not reintroduce launch-relevant file identity hashing into cache-key matching" `
        @(
            "SHA256\.HashData",
            "FileIdentity\(",
            "MarkerIdentity\(",
            "MaxMarkerIdentityBytes",
            "Path\.Combine\(gameDirectory"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.Status.cs" `
        "does not reintroduce hidden full runtime readiness checks into selected-version display status" `
        @(
            "LauncherGameFiles\.Ready",
            "LauncherGameFiles\.ReadinessProblem"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchCoordinator.cs" `
        "keeps branch-switch ready status on lightweight downloaded-state readiness" `
        @(
            "RefreshSelectedRuntimeSlotEvidence",
            "LauncherGameFiles\.Ready",
            "LauncherGameFiles\.ReadinessProblem"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherModel.SessionFastPath.cs" `
        "keeps saved-session startup on lightweight downloaded-state readiness" `
        @(
            "LauncherGameFiles\.Ready",
            "LauncherGameFiles\.ReadinessProblem",
            "LauncherLaunchReadiness\.Evaluate\("
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherSessionCoordinator.Timeout.cs" `
        "keeps offline timeout startup on lightweight downloaded-state readiness" `
        @(
            "LauncherGameFiles\.Ready",
            "LauncherGameFiles\.ReadinessProblem",
            "LauncherLaunchReadiness\.Evaluate\("
        )

    Add-LaunchAttemptMarkerReadCoverageCheck
}

function Add-LaunchAttemptMarkerReadCoverageCheck {
    $writerPath = "src\STS2Mobile\Launcher\LauncherLaunchMarkers.Attempt.cs"
    $readerPath = "src\STS2Mobile\Launcher\LauncherLaunchMarkers.Attempt.Read.cs"
    $description = "keeps launch-attempt marker writer prefixes covered by the recovery parser"
    $writer = Read-RepoFile $writerPath
    $reader = Read-RepoFile $readerPath
    if ($null -eq $writer -or $null -eq $reader) {
        return
    }

    $writtenPrefixes = [regex]::Matches($writer, "MarkerLine\((?<prefix>[A-Za-z0-9]+Prefix),") |
        ForEach-Object { $_.Groups["prefix"].Value } |
        Sort-Object -Unique
    $listedPrefixes = [regex]::Matches($reader, "(?m)^\s*(?<prefix>[A-Za-z0-9]+Prefix),\s*$") |
        ForEach-Object { $_.Groups["prefix"].Value } |
        Sort-Object -Unique
    $constructorPrefixes = [regex]::Matches($reader, "ReadAttemptValue\(values, (?<prefix>[A-Za-z0-9]+Prefix)\)") |
        ForEach-Object { $_.Groups["prefix"].Value } |
        Sort-Object -Unique
    $constructorPrefixes = @($constructorPrefixes + "AttemptPhasePrefix") | Sort-Object -Unique

    $missingFromReadList = @($writtenPrefixes | Where-Object { $listedPrefixes -notcontains $_ })
    if ($missingFromReadList.Count -gt 0) {
        $script:StaticAuditFailures.Add("$readerPath - $description - writer prefix missing from LaunchAttemptSummaryPrefixes: $($missingFromReadList -join ', ')")
        return
    }

    $missingFromSummary = @($listedPrefixes | Where-Object { $constructorPrefixes -notcontains $_ })
    if ($missingFromSummary.Count -gt 0) {
        $script:StaticAuditFailures.Add("$readerPath - $description - listed prefix is not read into LaunchAttemptSummary: $($missingFromSummary -join ', ')")
        return
    }

    $script:StaticAuditPasses += 1
    if (-not $script:StaticAuditQuiet) {
        Write-Host "PASS $readerPath - $description"
    }
}
