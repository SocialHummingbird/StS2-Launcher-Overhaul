function Add-SteamVersionSelectionSupportDocsAuditModuleBoundaryChecks {
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
        "scripts\audit-steam-version-selection.diagnostics-reporting.ps1" `
        "keeps launcher diagnostics report and branch-switch evidence audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsReportingChecks",
            "LauncherController.Diagnostics.cs",
            "LauncherDiagnostics.Reports.cs",
            "LauncherDiagnostics.ReportLauncherPreferences.cs",
            "LauncherDiagnostics.ReportBranchAvailability.cs",
            "LauncherDiagnostics.ReportCachedGameVersions.cs",
            "LauncherDiagnostics.ReportBranchSwitchSafety.cs",
            "LauncherDiagnostics.ReportBranchSwitchSafety.Push.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.evidence-tooling.ps1" `
        "keeps Steam version-selection evidence tooling audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionEvidenceToolingChecks",
            "android-adb-utils.ps1",
            "capture-steam-version-selection-evidence.ps1",
            "new-steam-version-selection-evidence.ps1",
            "export-public-evidence-redaction.ps1",
            "review-public-evidence-redaction.ps1",
            "audit-steam-branch-guidance-parity.ps1",
            "steam-version-selection-tooling.md"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.release-docs.ps1" `
        "keeps Steam version-selection release/readiness documentation audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionReleaseDocsChecks",
            "steam-version-selection-validation.md",
            "steam-version-selection-release-readiness.md",
            "steam-version-selection-architecture.md",
            "steam-version-selection-completion-audit.md",
            "steam-version-selection-runbook.md",
            "steam-version-selection-user-guide.md",
            "android-release-validation.md"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.beta-integrity.ps1" `
        "keeps beta branch integrity evidence audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionBetaIntegrityChecks",
            "capture-steam-beta-integrity-evidence.ps1",
            "review-beta-integrity-summary.ps1",
            "steam-beta-integrity-runtime-checklist.md",
            "Public-vs-beta branch integrity",
            "Public-vs-beta depot manifest integrity",
            "Public-vs-beta file inventory"
        )
}
