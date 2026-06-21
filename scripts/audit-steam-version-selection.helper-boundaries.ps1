function Add-SteamVersionSelectionHelperBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.ps1" `
        "keeps shared helper-boundary audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionHelperBoundaryChecks",
            "static-audit-utils.ps1",
            "evidence-marker-utils.ps1",
            "LauncherMarkerFile.Read.cs",
            "LauncherGameFiles.Markers.cs",
            "audit-steam-version-selection.launcher-shell.ps1",
            "audit-steam-version-selection.branch-selector.ps1",
            "audit-steam-version-selection.branch-runtime.ps1",
            "audit-steam-version-selection.branch-availability.ps1",
            "audit-steam-version-selection.download-workflows.ps1",
            "audit-steam-version-selection.session-auth.ps1",
            "audit-steam-version-selection.automation.ps1",
            "audit-steam-version-selection.local-login.ps1",
            "audit-steam-version-selection.confirmations.ps1",
            "audit-steam-version-selection.cloud-safety.ps1",
            "audit-steam-version-selection.login-panel.ps1",
            "audit-steam-version-selection.compact-labels.ps1",
            "audit-steam-version-selection.section-setup.ps1",
            "audit-steam-version-selection.safe-flow-guide.ps1",
            "audit-steam-version-selection.diagnostics-drawer.ps1",
            "audit-steam-version-selection.portal-chrome.ps1",
            "audit-steam-version-selection.status-capsule.ps1",
            "audit-steam-version-selection.compact-workflow.ps1",
            "audit-steam-version-selection.code-section.ps1",
            "audit-steam-version-selection.compact-section-flow.ps1",
            "audit-steam-version-selection.compact-install.ps1",
            "audit-steam-version-selection.startup-warmup.ps1"
        )

    Add-Check `
        "scripts\static-audit-utils.ps1" `
        "keeps shared static audit harness isolated from version-selection contracts" `
        @(
            "Initialize-StaticAudit",
            "Resolve-RepoPath",
            "Read-RepoFile",
            "Add-Check",
            "Add-ForbiddenCheck",
            "Complete-StaticAudit",
            "StaticAuditFailures",
            "StaticAuditPasses",
            "StaticAuditQuiet",
            "ThrowOnFailure"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.launcher-shell.ps1" `
        "keeps launcher shell audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionLauncherShellChecks",
            "LauncherUI.cs",
            "LauncherUI.Lifecycle.cs",
            "LauncherUI.MainThread.cs",
            "LauncherUI.Viewport.cs",
            "LauncherUI.AutoLaunch.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.ps1" `
        "keeps Steam branch selector audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorChecks",
            "SteamGameBranch.cs",
            "LauncherBranchCatalog.Option.cs",
            "LauncherBranchDropdown.cs",
            "DownloadSection.Branches.cs",
            "ActionSection.Branches.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-runtime.ps1" `
        "keeps Steam branch runtime and cache audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionBranchRuntimeChecks",
            "DepotDownloader.DepotManifestReference.cs",
            "LauncherBranchMarkerFields.cs",
            "LauncherAndroidAppPrivatePath.cs",
            "LauncherGameFiles.Redownload.cs",
            "LauncherGameFiles.CacheCleanup.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.download-workflows.ps1" `
        "keeps download, update, and branch-refresh workflow audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionDownloadWorkflowChecks",
            "LauncherModel.Downloads.cs",
            "LauncherModel.Downloads.Action.cs",
            "LauncherController.Downloads.Actions.cs",
            "LauncherController.UpdateChecks.cs",
            "LauncherController.UpdateChecks.ViewUpdate.cs",
            "LauncherController.UpdateChecks.Run.cs",
            "LauncherController.UpdateChecks.Workflow.cs",
            "LauncherController.UpdateChecks.Results.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.session-auth.ps1" `
        "keeps Steam session authentication audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionSessionAuthChecks",
            "LauncherModel.SessionAuth.cs",
            "LauncherModel.SessionAuth.Attempt.cs",
            "LauncherSteamSession.Connection.SavedCredentials.cs",
            "LauncherSteamSession.Connection.Ensure.cs",
            "LauncherSteamSession.Connection.Adoption.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.automation.ps1" `
        "keeps launcher automation audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionAutomationChecks",
            "LauncherController.Automation.cs",
            "LauncherController.Automation.Request.cs",
            "LauncherController.Automation.Run.cs",
            "LauncherController.Automation.Marker.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.local-login.ps1" `
        "keeps local Steam credential handoff audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionLocalLoginChecks",
            "LauncherController.LocalLogin.cs",
            "LauncherController.LocalLogin.Start.cs",
            "LauncherController.LocalLogin.Handoff.cs",
            "LauncherController.LocalLogin.Watch.cs",
            "LauncherController.LocalLogin.Run.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.confirmations.ps1" `
        "keeps confirmation dialog and contextual-action audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionConfirmationChecks",
            "LauncherView.Dialog.Buttons.cs",
            "LauncherView.Dialog.Message.cs",
            "LauncherView.Behavior.Confirmation.cs",
            "LauncherController.Startup.BranchSwitch.cs",
            "LauncherController.CloudSync.Request.Factory.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.ps1" `
        "keeps branch-switch and manual cloud Push safety audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyChecks",
            "LauncherBranchSwitchSafety.cs",
            "LauncherController.CloudSync.PushSafety.cs",
            "LauncherCloudSyncEvidence.Fields.cs",
            "LauncherLocalSaveEvidence.cs",
            "LauncherBackupEvidence.cs",
            "CloudSyncCoordinator.SaveBackups.Manual.cs",
            "LauncherController.Startup.RuntimeEvidence.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-panel.ps1" `
        "keeps native credential panel and login-section audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionLoginPanelChecks",
            "LauncherCredentialEntrySupport.cs",
            "LoginSection.Submission.cs",
            "LoginSection.NativePanel.cs",
            "LoginSection.Help.cs",
            "LoginSection.State.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-labels.ps1" `
        "keeps reusable compact two-line label audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCompactLabelChecks",
            "CompactButtonDetailLabelSpec.cs",
            "CompactButtonDetailLabels.cs",
            "CompactButtonDetailLabels.Text.cs",
            "CompactButtonDetailLabels.Controls.cs",
            "LoginSection.CompactNativeButton.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.section-setup.ps1" `
        "keeps compact section setup and cue-text audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionSectionSetupChecks",
            "LauncherSectionSetup.cs",
            "LauncherSectionSetup.Header.cs",
            "LauncherSectionSetup.Header.Compact.cs",
            "LoginSection.cs",
            "CodeSection.cs",
            "DownloadSection.cs",
            "ActionSection.Construction.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.safe-flow-guide.ps1" `
        "keeps quick-start safe-flow guide audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionSafeFlowGuideChecks",
            "LauncherView.Layout.FirstRunGuide.cs",
            "LauncherView.Layout.FirstRunGuide.Steps.cs",
            "LauncherView.Layout.FirstRunGuide.StepCard.cs",
            "LauncherView.Layout.FirstRunGuide.Toggle.cs",
            "CompactSafeFlowStepSpec"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-drawer.ps1" `
        "keeps Help & Reports diagnostics drawer audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsDrawerChecks",
            "LauncherView.Layout.LogColumn.cs",
            "LauncherView.Layout.LogColumn.Toggle.cs",
            "LauncherView.Layout.LogColumn.Sizing.cs",
            "LauncherView.Diagnostics.cs",
            "LauncherController.Diagnostics.Export.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-chrome.ps1" `
        "keeps launcher shell, brand, and compact panel chrome audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionPortalChromeChecks",
            "LauncherView.Layout.cs",
            "LauncherView.Layout.BrandHeader.cs",
            "LauncherView.Layout.BrandMark.cs",
            "LauncherLayoutProfile.cs",
            "StyledPanel.cs",
            "LauncherView.Layout.PrimaryColumn.Support.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.status-capsule.ps1" `
        "keeps launcher status capsule audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionStatusCapsuleChecks",
            "LauncherView.Layout.StatusCapsule.cs",
            "LauncherView.Layout.StatusCapsule.Compact.cs",
            "LauncherView.Layout.StatusCapsule.Detail.cs",
            "LauncherView.Layout.StatusCapsule.Styles.cs",
            "LauncherView.Status.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.ps1" `
        "keeps compact workflow and current-task audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowChecks",
            "LauncherView.Layout.CompactWorkflow.cs",
            "LauncherView.Layout.CompactTaskHeader.cs",
            "LauncherView.Layout.PrimaryColumn.Body.cs",
            "LauncherView.CompactWorkflow.Data.cs",
            "LauncherView.CompactWorkflow.State.cs",
            "LauncherView.CompactWorkflow.Navigation.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.code-section.ps1" `
        "keeps compact Steam Guard code-section audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCodeSectionChecks",
            "CodeSection.cs",
            "CodeSection.Labels.cs",
            "CodeSection.Input.cs",
            "CodeSection.SubmitButton.cs",
            "CodeSection.Layout.cs",
            "CodeSection.Prompt.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-section-flow.ps1" `
        "keeps compact section visibility and scroll-flow audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCompactSectionFlowChecks",
            "SetCompactReadyInstallSectionVisible",
            "HideCompactCompletedAuthSections",
            "ScrollCompactPrimaryTo",
            "CompactScrollAnchorTopPadding",
            "ReanchorCompactScrollTargetAfterViewportChange",
            "ReadyScrollTarget"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-install.ps1" `
        "keeps compact install/version/download audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCompactInstallChecks",
            "DownloadSection.cs",
            "DownloadSection.Construction.Version.cs",
            "DownloadSection.CompactVersion.cs",
            "DownloadSection.CompactDownload.cs",
            "DownloadSection.Progress.cs",
            "LauncherComponentTheme.cs",
            "new StyledProgressBar"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.startup-warmup.ps1" `
        "keeps startup safe-mode, shader warmup, and startup-status audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionStartupWarmupChecks",
            "LauncherStartupFlow.StartupMode.cs",
            "ShaderWarmupScreen.cs",
            "StyledProgressBar.cs",
            "LauncherStartupStatus.cs",
            "LauncherStartupStatus.Android.cs",
            "LauncherGameStartupRecovery.State.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-availability.ps1" `
        "keeps Steam branch availability audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionBranchAvailabilityChecks",
            "SteamBranchAvailabilityMarkerFields",
            "SteamBranchAvailabilityMarkerFile",
            "SteamBranchAvailabilityMarkerRow",
            "LauncherBranchAvailabilityStatus.Read.cs",
            "DepotDownloader.BranchAvailability.Marker.cs"
        )

    Add-Check `
        "scripts\evidence-marker-utils.ps1" `
        "centralizes marker-text parsing for release evidence scripts" `
        @(
            "Read-MarkerValueFromText",
            "Read-MarkerIntFromText",
            "Read-MarkerRowsFromText",
            "Read-BranchFromMarkerText",
            "MissingValue",
            "OrdinalIgnoreCase",
            "\[int\]::TryParse",
            'return @\(\$MissingValue\)'
        )

    Add-Check `
        "scripts\android-shell-utils.ps1" `
        "centralizes Android shell quoting for run-as evidence capture" `
        @(
            "ConvertTo-AndroidShellSingleQuoted",
            "ConvertTo-AndroidShellPathSingleQuoted",
            "Unsupported single quote in device path",
            "-split",
            "-join",
            "return ConvertTo-AndroidShellSingleQuoted"
        )

    Add-Check `
        "scripts\evidence-path-utils.ps1" `
        "centralizes repo-relative and evidence-relative path helpers" `
        @(
            "Resolve-EvidenceRepoPath",
            "Get-EvidenceRelativePath",
            "ConvertTo-EvidenceSafeFileName",
            "IsPathRooted",
            "MakeRelativeUri",
            "UnescapeDataString",
            "DirectorySeparatorChar",
            "empty",
            "\[\^A-Za-z0-9\._-\]"
        )

    Add-Check `
        "scripts\evidence-redaction-utils.ps1" `
        "centralizes public evidence and focused log redaction patterns" `
        @(
            "ConvertTo-RedactedEvidenceText",
            "ConvertTo-RedactedLogLine",
            "Get-EvidenceTextFileExtensions",
            "Get-EvidenceImageFileExtensions",
            "Get-EvidenceLocalOnlyPathPatterns",
            "Test-EvidenceLocalOnlyPath",
            "Get-EvidenceSensitiveTextChecks",
            "Get-PublicEvidenceRedactionReviewFields",
            "Format-PublicEvidenceRedactionReviewFields",
            "Screenshots manually reviewed",
            "Credential suggestions absent",
            "Only sanitized diagnostics selected for public sharing",
            "redacted-local-path",
            "android-app-private",
            "redacted-device-serial",
            "redacted-email",
            "Bearer <redacted>",
            "logs\\\\logcat-full",
            "logs\\\\logcat-steam-version-focused",
            "logcat-\(\?!steam-version-focused-redacted\)",
            "startup-routing-focused",
            "credential/token assignment",
            "Android package-private data path",
            "known connected device serial",
            "saveData",
            "profileContent"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.cs" `
        "declares shared marker-file sentinel values" `
        @(
            "internal static partial class LauncherMarkerFile",
            "MissingFileValue = ""<none>""",
            "MissingLineValue = ""<missing>""",
            "ReadFailedValue = ""<read failed>"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.Read.cs" `
        "centralizes scalar marker-file value parsing" `
        @(
            "ReadValue",
            "ReadOptionalValue",
            "File\.ReadLines",
            "StringComparison\.OrdinalIgnoreCase"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.Typed.cs" `
        "centralizes typed marker-file parsing" `
        @(
            "ReadInt",
            "NumberStyles\.Integer",
            "CultureInfo\.InvariantCulture",
            "ReadUtc",
            "UtcParseable",
            "DateTimeStyles\.AdjustToUniversal",
            "ReadBoolFlag"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.Values.cs" `
        "centralizes marker-file repeated-value reads" `
        @(
            "ReadJoinedValues",
            "ReadValues",
            "File\.ReadLines",
            "valueFormatter",
            "maxValues"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherMarkerFile.Predicates.cs" `
        "centralizes marker-file predicates and counts" `
        @(
            "CountLines",
            "HasLine",
            "HasConcreteValue"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Markers.cs" `
        "keeps game-file marker evidence readers as thin shared-helper wrappers" `
        @(
            "ReadMarkerValue",
            "LauncherMarkerFile\.ReadValue",
            "ReadMarkerInt",
            "LauncherMarkerFile\.ReadInt",
            "MarkerUtcParseable",
            "LauncherMarkerFile\.UtcParseable",
            "MarkerHasLine",
            "LauncherMarkerFile\.HasLine"
        )
}
