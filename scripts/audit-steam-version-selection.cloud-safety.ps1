function Add-SteamVersionSelectionCloudSafetyChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameVersionCache.cs" `
        "enumerates side-by-side cached non-public versions" `
        @(
            "CachedVersion",
            "LauncherStorageNames\.GameVersionsDirectory",
            "Selected"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.cs" `
        "keeps branch-switch safety marker identity isolated" `
        @(
            "internal static partial class LauncherBranchSwitchSafety",
            "last_game_branch_switch\.txt",
            "internal const string MarkerFileName",
            "MarkerPath",
            "HasMarker"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Fields.cs" `
        "centralizes branch-switch safety marker field prefixes" `
        @(
            "UtcPrefix = ""UTC:""",
            "PreviousBranchPrefix = ""Previous branch:""",
            "SelectedBranchPrefix = ""Selected branch:""",
            "SelectedBranchSelectionKindPrefix = ""Selected branch selection kind:""",
            "SelectorModePrefix = ""Steam branch selector mode:""",
            "SelectedVersionPrefix = ""Selected version:""",
            "SelectedVersionSlotKindPrefix = ""Selected version slot kind:""",
            "SelectedVersionSlotDirectoryPrefix = ""Selected version slot directory:""",
            "SelectedBranchNotePrefix = ""Selected branch note:""",
            "LocalBackupForcedPrefix = ""Local backup forced on:""",
            "ManualPushRequiresBackupStoragePrefix = ""Manual Push requires backup storage:""",
            "WarningAcknowledgedPrefix = ""Warning acknowledged:""",
            "NonPublicBranchWarningAcknowledgedPrefix = ""Non-public branch warning acknowledged:"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Read.cs" `
        "reads and parses branch-switch safety marker evidence" `
        @(
            "MarkerUtc",
            "MarkerUtcParseable",
            "PreviousBranch",
            "SelectedBranch",
            "SelectedBranchSelectionKind",
            "SelectorMode",
            "SelectedVersion",
            "SelectedVersionSlotKind",
            "SelectedVersionSlotDirectory",
            "SelectedBranchNote",
            "LocalBackupForced",
            "ManualPushRequiresBackupStorage",
            "WarningAcknowledged",
            "NonPublicBranchWarningAcknowledged",
            "ReadMarkerValue",
            "LauncherMarkerFile\.ReadValue",
            "LauncherMarkerFile\.HasConcreteValue",
            "ReadMarkerBool",
            "TryReadMarkerUtc",
            "DateTimeStyles\.AdjustToUniversal"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Gates.cs" `
        "enforces branch-switch safety gates before manual Push" `
        @(
            "HasRequiredEvidence",
            "SelectedBranchMatches",
            "ManualPushPrerequisitesSatisfied",
            "SteamGameBranch\.Normalize",
            "LauncherCloudSyncEvidence\.HasManualPullAfterBranchSwitch",
            "LauncherLocalSaveEvidence\.HasImportantSaveEvidence",
            "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
            "AppPaths\.HasStoragePermission"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Write.cs" `
        "writes branch-switch safety marker and marks save-origin pending" `
        @(
            "WriteMarker",
            "LocalBackupForcedPrefix",
            "ManualPushRequiresBackupStoragePrefix",
            "WarningAcknowledgedPrefix",
            "NonPublicBranchWarningAcknowledgedPrefix",
            "SelectedBranchSelectionKindPrefix",
            "SelectorModePrefix",
            "SelectedBranchNotePrefix",
            "SelectedVersionPrefix",
            "SelectedVersionSlotKindPrefix",
            "SelectedVersionSlotDirectoryPrefix",
            "SelectorHelpText",
            "WriteBranchSwitchPendingOrigin",
            "beta password entry is not implemented",
            "Failed to write branch switch safety marker"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.cs" `
        "keeps manual Push entry points routed through shared safety gates" `
        @(
            "CloudPushPressed",
            "CanArmCloudPush",
            "CloudPushSafetyContext\.Create",
            "CanPushWithBaselineEvidence\(pushContext\)",
            "CanPushAfterBranchSwitch\(pushContext\)",
            "ManualCloudSyncRequest\.Push"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.Context.cs" `
        "captures selected branch context for Push safety markers" `
        @(
            "CloudPushSafetyContext",
            "LauncherPreferences\.ReadGameBranch\(\)",
            "SteamGameBranch\.DisplayName",
            "SelectedBranch",
            "SelectedVersion",
            "WriteBlockedMarker",
            "WriteManualPushBlockedMarker"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.Baseline.cs" `
        "guards baseline manual Push until Pull, local save, and save-origin evidence match" `
        @(
            "CanPushWithBaselineEvidence",
            "Pull from Cloud must complete for the selected game version before Push",
            "selected game version \{pushContext\.SelectedVersion\}",
            "no Android local save evidence exists before Push",
            "LastManualPullCompletionRecorded",
            "LastManualPullMatchesSelectedBranch",
            "WriteBlockedMarker",
            "LauncherLocalSaveEvidence\.HasImportantSaveEvidence",
            "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
            "Manual Push blocked: Android local save origin evidence does not match the selected runtime"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.BranchSwitch.cs" `
        "guards manual Push after branch switches until backup storage is available" `
        @(
            "CanPushAfterBranchSwitch",
            "BranchSwitchSafety",
            "LauncherBranchSwitchSafety\.HasRequiredEvidence",
            "branch switch marker is missing required safety evidence",
            "does not match the selected game version",
            "no current Pull-after-switch evidence exists",
            "no Android local save evidence exists",
            "backup storage permission is unavailable",
            "LauncherCloudSyncEvidence\.HasManualPullAfterBranchSwitch",
            "WriteBlockedMarker",
            "Pull from Cloud must complete after this game-version switch before Push",
            "HasManualPullAfterBranchSwitch",
            "LauncherLocalSaveEvidence\.HasImportantSaveEvidence",
            "no Android local save files were found",
            "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
            "Manual Push blocked: save-origin evidence is missing or belongs to a different selected runtime after branch switch",
            "backup storage",
            "Push"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.cs" `
        "keeps manual cloud sync request state and confirmation properties typed" `
        @(
            "CloudSyncTimeoutMs = 180_000",
            "private readonly partial struct ManualCloudSyncRequest",
            "ConfirmationMessage",
            "ConfirmText",
            "CancelText",
            "BypassConfirmation",
            "Func<Task<string>> run",
            "Action<Exception>\? onFailed = null"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.Factory.cs" `
        "routes manual Pull and Push requests through explicit markers and request callbacks" `
        @(
            "ManualCloudSyncRequest Push",
            "ManualCloudSyncRequest Pull",
            "PushConfirmationMessage\(dataDir, selectedBranch\)",
            "LauncherCloudSaveState\.ManualPushAllAsync",
            "LauncherCloudSaveState\.ManualPullAllAsync",
            "WriteManualPushMarker",
            "WriteManualPushBlockedMarker",
            "WriteManualPullMarker",
            "Pull Steam Cloud saves to Android local storage"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.PushConfirmation.cs" `
        "warns final Push confirmation about selected version, branch switches, and save overwrite risk" `
        @(
            "Selected version slot:",
            "Pull-after-switch for",
            "Android local save evidence",
            "local pre-Push backup",
            "cloud pre-Push backup",
            "A game version switch was recorded",
            "cross-version/destructive",
            "LauncherBranchSwitchSafety\.HasMarker",
            "SteamGameInstallPaths\.VersionSlotKind",
            "Pull from Cloud first and verify the Android saves exist before pushing"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.Lifecycle.cs" `
        "keeps manual cloud sync lifecycle UI updates and timeout execution isolated" `
        @(
            "ShowStarted",
            "ShowComplete",
            "ShowFailed",
            "ShowFinished",
            "RunWithTimeoutAsync",
            "SetPushPullDisabled\(true\)",
            "SetPushPullDisabled\(false\)",
            "Open Help & Reports for details",
            "PatchHelper\.Log",
            "LauncherTimeout\.RunOrThrowAsync",
            "CloudSyncTimeoutMs"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.Execution.cs" `
        "executes Pull/Push cloud sync requests without bypassing Push confirmation" `
        @(
            "ManualCloudSyncRequest\.Pull\(",
            "RequestCloudSync",
            "ExecuteCloudSyncAsync",
            "request\.BypassConfirmation",
            "ShowConfirmation",
            "RunOnMainThread"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.cs" `
        "declares manual cloud evidence marker filenames and paths" `
        @(
            "last_manual_cloud_pull\.txt",
            "LastManualPushMarkerFileName",
            "last_manual_cloud_push_blocked\.txt"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Fields.cs" `
        "centralizes manual cloud evidence marker prefixes" `
        @(
            "SelectedBranchPrefix = ""Selected branch:""",
            "SelectedBranchSelectionKindPrefix = ""Selected branch selection kind:""",
            "SelectorModePrefix = ""Steam branch selector mode:""",
            "SelectedVersionPrefix = ""Selected version:""",
            "SelectedVersionSlotKindPrefix = ""Selected version slot kind:""",
            "SelectedVersionSlotDirectoryPrefix = ""Selected version slot directory:""",
            "SelectedBranchNotePrefix = ""Selected branch note:""",
            "ManualPullCompletedBeforePushPrefix = ""Manual Pull completed before Push:""",
            "ManualPullCompletedBeforeBranchSwitchPushPrefix = ""Manual Pull completed before branch-switch Push:""",
            "PrePushLocalBackupEvidenceCountPrefix = ""Pre-Push local backup evidence count:""",
            "PrePushCloudBackupEvidenceCountPrefix = ""Pre-Push cloud backup evidence count:""",
            "LatestPrePushLocalBackupUtcPrefix = ""Latest pre-Push local backup UTC:""",
            "LatestPrePushCloudBackupUtcPrefix = ""Latest pre-Push cloud backup UTC:""",
            "ImportantLocalSaveEvidenceCountPrefix = ""Important Android local save evidence count:""",
            "BaselineManualPushPrerequisitesSatisfiedPrefix = ""Baseline manual Push prerequisites satisfied:""",
            "BranchSwitchPrePushBackupEvidenceSatisfiedPrefix = ""Branch-switch pre-Push backup evidence satisfied:""",
            "BranchSwitchManualPushPrerequisitesSatisfiedPrefix = ""Branch-switch manual Push prerequisites satisfied:""",
            "ManualPushCompletedAfterBranchSwitchSafetyGatesPrefix = ""Manual Push completed after branch-switch safety gates:""",
            "BlockedReasonPrefix = ""Blocked reason:""",
            "ManualPushBlockedBeforeUploadPrefix = ""Manual Push blocked before upload:""",
            "ManualPushBlockedReasonPrefix = ""Manual Push blocked:"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Pull.cs" `
        "records successful manual Pull evidence for branch-switch Push safety" `
        @(
            "HasManualPullAfterBranchSwitch",
            "LastManualPullUtc",
            "LastManualPullUtcParseable",
            "LastManualPullSelectedBranch",
            "LastManualPullSelectedBranchSelectionKind",
            "LastManualPullSelectorMode",
            "LastManualPullSelectedVersion",
            "LastManualPullSelectedVersionSlotKind",
            "LastManualPullSelectedVersionSlotDirectory",
            "LastManualPullCompletionRecorded",
            "LastManualPullBeforePushCompletionRecorded",
            "BaselineManualPushPrerequisitesSatisfied",
            "ManualPullCompletedBeforePushPrefix",
            "LastManualPullIsAfterBranchSwitch",
            "LastManualPullMatchesSelectedBranch",
            "WriteManualPullMarker",
            "ManualPullCompletedBeforeBranchSwitchPushPrefix",
            "SelectedVersionPrefix",
            "SelectedBranchNotePrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.cs" `
        "reads successful manual Push timestamps from the Push marker" `
        @(
            "LastManualPushUtc",
            "LastManualPushUtcParseable",
            "LastManualPushMarkerPath",
            "CultureInfo\.InvariantCulture"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Latest.cs" `
        "selects latest manual Push evidence between completed and blocked outcomes" `
        @(
            "LatestManualPushEvidenceOutcome",
            "LatestManualPushEvidenceUtc",
            "LatestManualPushEvidenceSelectedBranch",
            "LatestManualPushEvidenceSelectedBranchSelectionKind",
            "LatestManualPushEvidenceSelectorMode",
            "LatestManualPushEvidenceSelectedVersion",
            "LatestManualPushEvidenceSelectedVersionSlotKind",
            "LatestManualPushEvidenceSelectedVersionSlotDirectory",
            "LatestManualPushEvidenceReason",
            "blocked-before-upload",
            "Manual Push completed",
            "LatestManualPushEvidenceBlocked"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Read.cs" `
        "reads completed manual Push marker metadata and backup evidence" `
        @(
            "LastManualPushSelectedBranch",
            "LastManualPushSelectedBranchSelectionKind",
            "LastManualPushSelectorMode",
            "LastManualPushSelectedVersion",
            "LastManualPushSelectedVersionSlotKind",
            "LastManualPushSelectedVersionSlotDirectory",
            "LastManualPushRecordedLocalBackupCount",
            "LastManualPushRecordedCloudBackupCount",
            "LastManualPushRecordedLatestLocalBackupUtc",
            "LastManualPushRecordedLatestCloudBackupUtc",
            "LastManualPushRecordedImportantLocalSaveEvidenceCount",
            "LastManualPushRecordedBaselinePrerequisitesSatisfied",
            "LastManualPushCompletionRecorded",
            "LastManualPushPrePushBackupEvidenceSatisfied",
            "PrePushLocalBackupEvidenceCountPrefix",
            "PrePushCloudBackupEvidenceCountPrefix",
            "LatestPrePushLocalBackupUtcPrefix",
            "LatestPrePushCloudBackupUtcPrefix",
            "ImportantLocalSaveEvidenceCountPrefix",
            "BaselineManualPushPrerequisitesSatisfiedPrefix",
            "ManualPushCompletedAfterBranchSwitchSafetyGatesPrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Safety.cs" `
        "requires completed selected-branch Push evidence after branch switches" `
        @(
            "LastManualPushIsAfterBranchSwitch",
            "LastManualPushMatchesSelectedBranch",
            "HasManualPushAfterBranchSwitch",
            'LatestManualPushEvidenceOutcome\(dataDir\), "completed"',
            "LastManualPushCompletionRecorded",
            "LastManualPushPrePushBackupEvidenceSatisfied",
            "SteamGameBranch\.Normalize"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Write.cs" `
        "records successful manual Push evidence after save-origin and backup gates" `
        @(
            "WriteManualPushMarker",
            "LauncherSaveOriginEvidence\.WriteManualPushOrigin",
            "PrePushLocalBackupEvidenceCountPrefix",
            "PrePushCloudBackupEvidenceCountPrefix",
            "LatestPrePushLocalBackupUtcPrefix",
            "LatestPrePushCloudBackupUtcPrefix",
            "ImportantLocalSaveEvidenceCountPrefix",
            "BaselineManualPushPrerequisitesSatisfiedPrefix",
            "BranchSwitchPrePushBackupEvidenceSatisfiedPrefix",
            "ManualPushCompletedAfterBranchSwitchSafetyGatesPrefix",
            "SelectedVersionPrefix",
            "SelectedBranchNotePrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.BlockedPush.cs" `
        "records blocked-before-upload manual Push evidence" `
        @(
            "LastManualPushBlockedUtc",
            "LastManualPushBlockedUtcParseable",
            "LastManualPushBlockedSelectedBranch",
            "LastManualPushBlockedSelectedBranchSelectionKind",
            "LastManualPushBlockedSelectorMode",
            "LastManualPushBlockedSelectedVersion",
            "LastManualPushBlockedSelectedVersionSlotKind",
            "LastManualPushBlockedSelectedVersionSlotDirectory",
            "LastManualPushBlockedRecordedPrerequisitesSatisfied",
            "LastManualPushBlockedRecordedLocalBackupCount",
            "LastManualPushBlockedRecordedCloudBackupCount",
            "LastManualPushBlockedRecordedLatestLocalBackupUtc",
            "LastManualPushBlockedRecordedLatestCloudBackupUtc",
            "LastManualPushBlockedRecordedImportantLocalSaveEvidenceCount",
            "LastManualPushBlockedRecordedBaselinePrerequisitesSatisfied",
            "LastManualPushBlockedRecordedPrePushBackupEvidenceSatisfied",
            "LastManualPushBlockedReason",
            "LastManualPushBlockedBeforeUpload",
            "LastManualPushBlockedMatchesSelectedBranch",
            "WriteManualPushBlockedMarker",
            "WriteManualPushBlockedMarker\(string dataDir, string selectedBranch, string reason\)",
            "ManualPushBlockedBeforeUploadPrefix",
            "ManualPushBlockedReasonPrefix",
            "PrePushLocalBackupEvidenceCountPrefix",
            "PrePushCloudBackupEvidenceCountPrefix",
            "LatestPrePushLocalBackupUtcPrefix",
            "LatestPrePushCloudBackupUtcPrefix",
            "ImportantLocalSaveEvidenceCountPrefix",
            "BaselineManualPushPrerequisitesSatisfiedPrefix",
            "SelectedVersionPrefix",
            "SelectedBranchNotePrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Markers.cs" `
        "keeps manual cloud evidence marker parsing isolated" `
        @(
            "HasCompletionFlag",
            'string\? ReadSelectedBranch',
            "ReadMarkerValue",
            "LauncherMarkerFile\.ReadOptionalValue",
            "ReadUtc",
            "LauncherMarkerFile\.ReadUtc",
            "LauncherMarkerFile\.ReadBoolFlag",
            "SanitizeSingleLine"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.cs" `
        "exposes bounded local save evidence counts for branch-switch Push safety" `
        @(
            "internal static partial class LauncherLocalSaveEvidence",
            "MaxFilesToInspect = 1000",
            "MaxDirectoriesToInspect = 250",
            "IgnoredDirectoryNames",
            "HasImportantSaveEvidence",
            "CountImportantSaveEvidence"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.Classify.cs" `
        "classifies non-empty Android save artifacts as important local save evidence" `
        @(
            "IsImportantSaveEvidence",
            "Path\.GetRelativePath",
            "ToLowerInvariant",
            "FileHasContent",
            "\.save",
            "\.save\.backup",
            "\.run",
            "\.bak",
            "prefs",
            "prefs\.save",
            "prefs\.backup",
            "prefs\.save\.backup",
            "new FileInfo\(file\)\.Length > 0"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.Enumeration.cs" `
        "walks local save directories with runtime-directory exclusions and scan limits" `
        @(
            "EnumerateFilesSafely",
            "new Stack<string>",
            "MaxFilesToInspect",
            "MaxDirectoriesToInspect",
            "IsIgnoredRuntimeDirectory",
            "IgnoredDirectoryNames",
            "StringComparison\.OrdinalIgnoreCase",
            "SafeFiles",
            "SafeDirectories"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.FileSystem.cs" `
        "swallows filesystem enumeration failures when scanning local save evidence" `
        @(
            "SafeFiles",
            "Directory\.GetFiles",
            "SafeDirectories",
            "Directory\.GetDirectories",
            "Array\.Empty<string>\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBackupEvidence.cs" `
        "exposes local/cloud pre-Push backup evidence after branch switching" `
        @(
            "internal static partial class LauncherBackupEvidence",
            "local-pre-push",
            "cloud-pre-push",
            "MaxBackupFilesToInspect",
            "LocalPrePushBackupCount",
            "CloudPrePushBackupCount",
            "LatestLocalPrePushBackupUtc",
            "LatestCloudPrePushBackupUtc",
            "HasLocalPrePushBackupAfterBranchSwitch",
            "HasCloudPrePushBackupAfterBranchSwitch",
            "HasPrePushBackupEvidenceAfterBranchSwitch"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBackupEvidence.Counts.cs" `
        "counts pre-Push backup files through bounded backup enumeration" `
        @(
            "CountBackups",
            "EnumerateBackups\(source\)",
            "count\+\+"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBackupEvidence.BranchSwitch.cs" `
        "compares pre-Push backup timestamps against branch-switch evidence" `
        @(
            "HasBackupAfterBranchSwitch",
            "TryReadBranchSwitchUtc",
            "LauncherBranchSwitchSafety\.MarkerUtc",
            "DateTimeStyles\.AdjustToUniversal",
            "TryReadBackupUtc\(backupPath, source, out var backupUtc\)",
            "backupUtc >= branchSwitchUtc"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBackupEvidence.Enumeration.cs" `
        "enumerates pre-Push backup files without walking unbounded backup trees" `
        @(
            "EnumerateBackups",
            "Directory\.Exists\(BackupDirectory\)",
            "Array\.Empty<string>\(\)",
            "Directory\.EnumerateFiles",
            "\*\.\{source\}\.bak",
            "SearchOption\.AllDirectories",
            "inspected >= MaxBackupFilesToInspect"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBackupEvidence.Timestamp.cs" `
        "parses backup UTC evidence from backup filenames before falling back to file mtime" `
        @(
            "LatestBackupUtc",
            "TryReadBackupUtc",
            "ToString\(""O"", CultureInfo\.InvariantCulture\)",
            "<none>",
            "EndsWith\(suffix, StringComparison\.OrdinalIgnoreCase\)",
            "DateTimeOffset\.FromUnixTimeSeconds",
            "File\.GetLastWriteTimeUtc"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SaveBackups.Manual.cs" `
        "fails manual Push before upload when required backup evidence is missing" `
        @(
            "EnforceManualPushBackupEvidence",
            "Manual Push blocked: local backup is enabled but backup storage permission is unavailable",
            "Manual Push blocked: local pre-Push backup evidence is incomplete",
            "Manual Push blocked: cloud pre-Push backup evidence is incomplete",
            "CloudImportantSaveCount",
            "importantPaths.Count",
            "localBackups < importantPaths.Count",
            "cloudBackups < cloudImportantSaveCount",
            "AppPaths\.HasStoragePermission"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.Startup.BranchSwitch.cs" `
        "uses centralized selector guidance in branch-switch confirmation" `
        @(
            "BranchSwitchConfirmationMessage",
            "SteamGameBranch\.SelectorInstallSlotHelpText",
            "LauncherBranchCatalog\.ReadVisibleBranches",
            "SelectedOptionStatus",
            "SelectedOptionDownloadProblem",
            "AppendLog\(STS2Mobile\.Steam\.SteamGameBranch\.SelectorInstallSlotHelpText",
            "Steam Cloud Push will require backup storage permission"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.Startup.RuntimeEvidence.cs" `
        "uses selected-version runtime evidence in ready and download-required status copy" `
        @(
            "SelectedVersionReadyStatus",
            "SelectedVersionDownloadRequiredStatus",
            "SteamGameInstallPaths\.VersionSlotKind",
            "Active install slot",
            "RefreshSelectedRuntimeSlotEvidence",
            "LauncherRuntimeSlotEvidence\.Write"
        )

}
