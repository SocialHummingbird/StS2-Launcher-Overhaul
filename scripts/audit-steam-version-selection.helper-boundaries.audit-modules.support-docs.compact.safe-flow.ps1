function Add-SteamVersionSelectionSupportDocsCompactSafeFlowBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.safe-flow-guide.ps1" `
        "keeps quick-start safe-flow guide audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionSafeFlowGuideChecks",
            "audit-steam-version-selection.safe-flow-guide.panel.ps1",
            "audit-steam-version-selection.safe-flow-guide.steps.ps1",
            "audit-steam-version-selection.safe-flow-guide.toggle.ps1",
            "Add-SteamVersionSelectionSafeFlowGuidePanelChecks",
            "Add-SteamVersionSelectionSafeFlowGuideStepChecks",
            "Add-SteamVersionSelectionSafeFlowGuideToggleChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.safe-flow-guide.panel.ps1" `
        "keeps quick-start safe-flow guide panel audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionSafeFlowGuidePanelChecks",
            "LauncherView.Layout.FirstRunGuide.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.safe-flow-guide.steps.ps1" `
        "keeps quick-start safe-flow guide step audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionSafeFlowGuideStepChecks",
            "LauncherView.Layout.FirstRunGuide.Steps.cs",
            "LauncherView.Layout.FirstRunGuide.StepCard.cs",
            "LauncherView.Layout.FirstRunGuide.StepCard.Decor.cs",
            "LauncherView.Layout.FirstRunGuide.StepCard.Labels.cs",
            "LauncherView.Layout.FirstRunGuide.StepStyle.cs",
            "CompactSafeFlowStepSpec"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.safe-flow-guide.toggle.ps1" `
        "keeps quick-start safe-flow guide toggle audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionSafeFlowGuideToggleChecks",
            "LauncherView.Layout.FirstRunGuide.Toggle.cs",
            "LauncherView.Layout.FirstRunGuide.Toggle.Text.cs"
        )
}
